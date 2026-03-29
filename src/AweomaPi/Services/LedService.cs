using System;
using System.Device.Gpio;
using System.Threading.Tasks;
using AweomaPi.Hardware;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Services
{
    /// <summary>
    /// LED-Status-Objekt fuer SetStatusAsync.
    /// </summary>
    public class LedStatus
    {
        public bool Power { get; set; } = true;
        public bool Vpn   { get; set; }
        public bool Wan   { get; set; }
        public bool Error { get; set; }

        /// <summary>Vordefinieter Normal-Status (alle Flags false — werden per CheckWan/VPN gesetzt)</summary>
        public static LedStatus Normal => new() { Power = true };
    }

    /// <summary>
    /// Steuert die 4 Status-LEDs (Power/gruen, VPN/blau, WAN/gelb, Error/rot).
    /// Verfuegbar auf beiden PCB-Varianten (Simple + Extended).
    ///
    /// LEDs:
    ///   Power  — GPIO 17 (gruen)  — System laeuft
    ///   VPN    — GPIO 22 (blau)   — WireGuard aktiv
    ///   WAN    — GPIO 23 (gelb)   — Internet-Verbindung
    ///   Error  — GPIO 27 (rot)    — Fehler oder Warnung
    /// </summary>
    public sealed class LedService : IDisposable
    {
        private readonly ILogger<LedService> _log;
        private GpioController? _gpio;
        private int _brightness = 100; // 0-100 Prozent (nur fuer PWM-faehige Pins relevant)

        public LedService(ILogger<LedService> log)
        {
            _log = log;
        }

        // ─── Initialisierung ─────────────────────────────────────────────────────
        public void Initialize()
        {
            try
            {
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

                _log.LogInformation("[LED] 4 Status-LEDs initialisiert (Power/VPN/WAN/Error).");
            }
            catch (Exception ex)
            {
                _log.LogWarning("[LED] GPIO konnte nicht geoeffnet werden: {msg}", ex.Message);
                _gpio = null;
            }
        }

        // ─── Einzel-LED Steuerung ────────────────────────────────────────────────
        public void SetPower(bool on) => SetPin(GpioPins.LedPower, on);
        public void SetVpn(bool on)   => SetPin(GpioPins.LedVpn,   on);
        public void SetWan(bool on)   => SetPin(GpioPins.LedWan,    on);
        public void SetError(bool on) => SetPin(GpioPins.LedError,  on);

        /// <summary>Alle LEDs ausschalten.</summary>
        public void AllOff()
        {
            SetPin(GpioPins.LedPower, false);
            SetPin(GpioPins.LedVpn,   false);
            SetPin(GpioPins.LedWan,   false);
            SetPin(GpioPins.LedError, false);
        }

        // ─── Status setzen ────────────────────────────────────────────────────────
        /// <summary>Setzt alle 4 LEDs auf einen Status.</summary>
        public Task SetStatusAsync(LedStatus status)
        {
            if (_brightness == 0) return Task.CompletedTask; // Standby / Night

            SetPin(GpioPins.LedPower, status.Power);
            SetPin(GpioPins.LedVpn,   status.Vpn);
            SetPin(GpioPins.LedWan,   status.Wan);
            SetPin(GpioPins.LedError, status.Error);
            return Task.CompletedTask;
        }

        // ─── Helligkeit (0-100%) ──────────────────────────────────────────────────
        /// <summary>
        /// Setzt die LED-Helligkeit in Prozent (0-100).
        /// 0  = alle LEDs aus (Standby).
        /// 10 = Night-Modus (gedimmt).
        /// 100 = volle Helligkeit.
        /// Hinweis: echtes PWM-Dimmen benoetigt Hardware-PWM-Pins.
        /// Bei einfachen GPIO-Pins wird 0=Aus, alles andere=An gewertet.
        /// </summary>
        public void SetBrightness(int percent)
        {
            _brightness = Math.Clamp(percent, 0, 100);
            _log.LogDebug("[LED] Helligkeit: {p}%", _brightness);

            if (_brightness == 0)
            {
                AllOff();
            }
            // Fuer echtes PWM-Dimmen: SoftPwm oder Hardware-PWM implementieren
        }

        // ─── Blink-Effekte ────────────────────────────────────────────────────────
        /// <summary>
        /// Blinkt eine LED n-mal.
        /// Wird fuer Reboot (3x gelb/WAN) und Shutdown (3x rot/Error) genutzt.
        /// </summary>
        public async Task BlinkAsync(int pin, int count = 3, int onMs = 200, int offMs = 200)
        {
            for (int i = 0; i < count; i++)
            {
                SetPin(pin, true);
                await Task.Delay(onMs);
                SetPin(pin, false);
                await Task.Delay(offMs);
            }
        }

        // ─── Private Hilfsmethoden ───────────────────────────────────────────────
        private void SetPin(int pin, bool on)
        {
            if (_gpio is null || !_gpio.IsPinOpen(pin)) return;
            _gpio.Write(pin, on ? PinValue.High : PinValue.Low);
        }

        // ─── Dispose ─────────────────────────────────────────────────────────────
        public void Dispose() => _gpio?.Dispose();
    }
}
