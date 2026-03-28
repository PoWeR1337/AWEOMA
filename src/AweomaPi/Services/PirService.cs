using System.Device.Gpio;
using AweomaPi.Hardware;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Services;

/// <summary>
/// Ueberwacht den PIR-Bewegungsmelder HC-SR501 (GPIO 25).
/// Nur aktiv wenn PCB Extended und PIR erkannt wurde.
/// Events: MotionDetected / MotionEnded
/// </summary>
public sealed class PirService : IDisposable
{
    private readonly ILogger<PirService> _log;
    private readonly HardwareProfile _hw;
    private GpioController? _gpio;

    private bool _lastState;
    private DateTime _lastMotion = DateTime.MinValue;

    /// <summary>Wird ausgeloest wenn Bewegung erkannt wird (GPIO HIGH).</summary>
    public event EventHandler<MotionEventArgs>? MotionDetected;

    /// <summary>Wird ausgeloest wenn Bewegung aufhoert (GPIO LOW).</summary>
    public event EventHandler<MotionEventArgs>? MotionEnded;

    /// <summary>Aktuelle PIR-Status-Beschreibung fuer Display.</summary>
    public string StatusText => _lastState
        ? $"Aktiv ({(int)(DateTime.UtcNow - _lastMotion).TotalSeconds}s)"
        : "Ruhig";

    public PirService(HardwareProfile hw, ILogger<PirService> log)
    {
        _hw  = hw;
        _log = log;
    }

    public void Initialize()
    {
        if (!_hw.HasPirSensor)
        {
            _log.LogInformation("[PIR] Kein Bewegungsmelder erkannt (nur PCB Extended) – PirService deaktiviert.");
            return;
        }

        try
        {
            _gpio = new GpioController();
            _gpio.OpenPin(GpioPins.PirSensor, PinMode.InputPullDown);

            // Auf steigende Flanke (Bewegung START) und fallende Flanke (Bewegung ENDE) hoeren
            _gpio.RegisterCallbackForPinValueChangedEvent(
                GpioPins.PirSensor,
                PinEventTypes.Rising | PinEventTypes.Falling,
                OnPirChanged);

            _log.LogInformation("[PIR] HC-SR501 aktiv auf GPIO {Pin}. Hinweis: 30s Aufwaermzeit benoetigt.",
                GpioPins.PirSensor);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[PIR] Initialisierung fehlgeschlagen.");
        }
    }

    private void OnPirChanged(object sender, PinValueChangedEventArgs e)
    {
        bool motion = e.ChangeType == PinEventTypes.Rising;

        if (motion == _lastState) return;  // Doppel-Events ignorieren
        _lastState = motion;

        if (motion)
        {
            _lastMotion = DateTime.UtcNow;
            _log.LogInformation("[PIR] Bewegung erkannt! ({Time})", _lastMotion.ToLocalTime());
            MotionDetected?.Invoke(this, new MotionEventArgs(_lastMotion));
        }
        else
        {
            _log.LogInformation("[PIR] Bewegung beendet. Dauer: {Sec}s",
                (int)(DateTime.UtcNow - _lastMotion).TotalSeconds);
            MotionEnded?.Invoke(this, new MotionEventArgs(DateTime.UtcNow));
        }
    }

    public void Dispose()
    {
        if (_gpio is not null && _gpio.IsPinOpen(GpioPins.PirSensor))
            _gpio.ClosePin(GpioPins.PirSensor);
        _gpio?.Dispose();
    }
}

public sealed record MotionEventArgs(DateTime Timestamp) : EventArgs;
