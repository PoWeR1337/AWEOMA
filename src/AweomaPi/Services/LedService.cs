using System.Device.Gpio;
using AweomaPi.Hardware;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Services;

/// <summary>
/// Steuert die 4 Status-LEDs (Power, VPN, WAN, Error).
/// Verfuegbar auf beiden PCB-Varianten.
/// </summary>
public sealed class LedService : IDisposable
{
    private readonly ILogger<LedService> _log;
    private readonly HardwareProfile _hw;
    private GpioController? _gpio;

    public LedService(HardwareProfile hw, ILogger<LedService> log)
    {
        _hw  = hw;
        _log = log;
    }

    public void Initialize()
    {
        if (!_hw.HasLeds)
        {
            _log.LogWarning("[LED] Keine LED-Pins verfuegbar.");
            return;
        }

        _gpio = new GpioController();
        foreach (var pin in new[] { GpioPins.LedPower, GpioPins.LedVpn, GpioPins.LedWan, GpioPins.LedError })
        {
            _gpio.OpenPin(pin, PinMode.Output);
            _gpio.Write(pin, PinValue.Low);
        }

        // Startup-Sequenz: alle LEDs kurz an
        _ = Task.Run(async () =>
        {
            foreach (var pin in new[] { GpioPins.LedPower, GpioPins.LedVpn, GpioPins.LedWan, GpioPins.LedError })
            {
                SetPin(pin, true);
                await Task.Delay(150);
            }
            await Task.Delay(300);
            foreach (var pin in new[] { GpioPins.LedPower, GpioPins.LedVpn, GpioPins.LedWan, GpioPins.LedError })
                SetPin(pin, false);

            // Power-LED dauerhaft an
            SetPin(GpioPins.LedPower, true);
        });

        _log.LogInformation("[LED] 4 Status-LEDs initialisiert.");
    }

    public void SetPower(bool on)  => SetPin(GpioPins.LedPower, on);
    public void SetVpn(bool on)    => SetPin(GpioPins.LedVpn,   on);
    public void SetWan(bool on)    => SetPin(GpioPins.LedWan,    on);
    public void SetError(bool on)  => SetPin(GpioPins.LedError,  on);

    /// <summary>Blinkt eine LED n-mal mit gegebenem Intervall.</summary>
    public async Task BlinkAsync(int pin, int times = 3, int intervalMs = 200)
    {
        for (int i = 0; i < times; i++)
        {
            SetPin(pin, true);
            await Task.Delay(intervalMs);
            SetPin(pin, false);
            await Task.Delay(intervalMs);
        }
    }

    private void SetPin(int pin, bool on)
    {
        if (_gpio is null || !_gpio.IsPinOpen(pin)) return;
        _gpio.Write(pin, on ? PinValue.High : PinValue.Low);
    }

    public void Dispose() => _gpio?.Dispose();
}
