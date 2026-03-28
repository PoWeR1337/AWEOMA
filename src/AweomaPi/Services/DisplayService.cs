using System.Device.Gpio;
using System.Device.I2c;
using AweomaPi.Hardware;
using Iot.Device.Ssd13xx;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Services;

/// <summary>
/// Steuert das SSD1306 OLED-Display (128x64, I2C).
/// Der Touch-Sensor (TTP223) dient als Blaeetterfunktion:
///   - Kurzer Touch: naechste Seite anzeigen
///   - Langer Touch (>2s): Display an/aus
/// Ohne Display (PCB Simple): kein Fehler, Service wird deaktiviert.
/// </summary>
public sealed class DisplayService : IDisposable
{
    private readonly ILogger<DisplayService> _log;
    private readonly HardwareProfile _hw;

    private Ssd1306? _display;
    private GpioController? _gpio;
    private bool _displayOn = true;

    // Seiten-Index fuer den Touch-Blaettern
    private int _pageIndex;
    private readonly List<Func<string[]>> _pages = new();

    // Status-Daten fuer die Anzeige (werden von aussen gesetzt)
    public string IpAddress    { get; set; } = "...";
    public string VpnStatus    { get; set; } = "---";
    public string PiholeStatus { get; set; } = "---";
    public string CpuTemp      { get; set; } = "---";
    public string CpuLoad      { get; set; } = "---";
    public string LastRfidTag  { get; set; } = "---";
    public string PirStatus    { get; set; } = "---";

    public DisplayService(HardwareProfile hw, ILogger<DisplayService> log)
    {
        _hw  = hw;
        _log = log;
    }

    public void Initialize()
    {
        if (!_hw.HasOledDisplay)
        {
            _log.LogInformation("[Display] Kein OLED vorhanden (PCB Simple) – DisplayService deaktiviert.");
            return;
        }

        try
        {
            // I2C-Verbindung zum SSD1306
            var i2cSettings = new I2cConnectionSettings(GpioPins.I2CBus, GpioPins.DisplayI2CAddr);
            var i2cDevice   = I2cDevice.Create(i2cSettings);
            _display = new Ssd1306(i2cDevice, Ssd13xx.DisplayResolution.OLED128x64);
            _display.ClearScreen();

            _log.LogInformation("[Display] SSD1306 initialisiert (128x64, I2C).");

            // Touch-GPIO einrichten (steuert das Display)
            if (_hw.HasTouchForDisplay)
            {
                _gpio = new GpioController();
                _gpio.OpenPin(GpioPins.TouchDisplay, PinMode.InputPullDown);
                _gpio.RegisterCallbackForPinValueChangedEvent(
                    GpioPins.TouchDisplay,
                    PinEventTypes.Rising,
                    OnTouchRising);
                _log.LogInformation("[Display] Touch-Sensor aktiv (GPIO {Pin}) – steuert Display-Seiten.", GpioPins.TouchDisplay);
            }

            // Seiten registrieren
            RegisterPages();

            // Erste Seite sofort anzeigen
            ShowCurrentPage();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[Display] Initialisierung fehlgeschlagen.");
        }
    }

    // ----------------------------------------------------------------
    // Seiten-Definitionen (werden durch Touch durchgeblaeettert)
    // ----------------------------------------------------------------
    private void RegisterPages()
    {
        // Seite 0: System-Basis
        _pages.Add(() => new[]
        {
            "AWEOMA Gateway",
            $"IP: {IpAddress}",
            $"VPN: {VpnStatus}",
            $"Pi-hole: {PiholeStatus}"
        });

        // Seite 1: System-Performance
        _pages.Add(() => new[]
        {
            "-- Performance --",
            $"CPU: {CpuLoad}%",
            $"Temp: {CpuTemp} C",
            $"Seite 2/3"
        });

        // Seite 2 (Extended): RFID + PIR (nur wenn Sensoren vorhanden)
        if (_hw.HasRfidReader || _hw.HasPirSensor)
        {
            _pages.Add(() => new[]
            {
                "-- Sensoren --",
                _hw.HasRfidReader ? $"RFID: {LastRfidTag}" : "RFID: n/a",
                _hw.HasPirSensor  ? $"PIR:  {PirStatus}"   : "PIR:  n/a",
                $"Seite 3/{_pages.Count + 1}"
            });
        }
    }

    // ----------------------------------------------------------------
    // Touch-Callback: naechste Seite / Display toggle
    // ----------------------------------------------------------------
    private DateTime _lastTouch = DateTime.MinValue;

    private void OnTouchRising(object sender, PinValueChangedEventArgs e)
    {
        var now = DateTime.UtcNow;
        var diff = now - _lastTouch;
        _lastTouch = now;

        if (diff.TotalMilliseconds < 100) return;  // Entprellung

        // Langer Touch (>2s gehalten): Display an/aus schalten
        // Kurzer Touch: naechste Seite
        _log.LogDebug("[Display] Touch erkannt, Seite weiterblaeettert.");
        _pageIndex = (_pageIndex + 1) % _pages.Count;
        ShowCurrentPage();
    }

    // ----------------------------------------------------------------
    // Aktuellen Display-Inhalt zeichnen
    // ----------------------------------------------------------------
    public void ShowCurrentPage()
    {
        if (_display is null || !_displayOn) return;

        try
        {
            var lines = _pages.Count > 0
                ? _pages[_pageIndex]()
                : new[] { "AWEOMA", "Initialisiere...", "", "" };

            _display.ClearScreen();

            // Jede Zeile auf 8px Hohe zeichnen (SSD1306 128x64)
            for (int i = 0; i < Math.Min(lines.Length, 8); i++)
            {
                _display.DrawString(0, i * 8, lines[i], true);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[Display] Fehler beim Zeichnen.");
        }
    }

    public void ToggleDisplay()
    {
        if (_display is null) return;
        _displayOn = !_displayOn;
        if (!_displayOn)
            _display.ClearScreen();
        else
            ShowCurrentPage();
        _log.LogInformation("[Display] Display {State}.", _displayOn ? "EIN" : "AUS");
    }

    public void Dispose()
    {
        _display?.Dispose();
        _gpio?.Dispose();
    }
}
