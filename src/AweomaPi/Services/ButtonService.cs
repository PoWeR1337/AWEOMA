using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AweomaPi.Hardware;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Services
{
    /// <summary>
    /// Verwaltet BTN1 und BTN2 auf dem AWEOMA PCB.
    ///
    /// BTN1 (GPIO 5, Pin 29):
    ///   Kurz  => Naechster Modus (zyklisch)
    ///   Lang  => Master-Key Modus aktivieren (3 Sekunden halten)
    ///
    /// BTN2 (GPIO 6, Pin 31):
    ///   Kurz  => PIR-Sensor an/aus toggle
    ///   Lang  => Standby — alles aus (3 Sekunden halten)
    ///
    /// Beide gleichzeitig:
    ///   2x kurz => Reboot  (3x gelb blinken)
    ///   3x kurz => Shutdown (3x rot blinken)
    /// </summary>
    public class ButtonService : IDisposable
    {
        // ─── Konstanten ──────────────────────────────────────────────────────────
        private const int LongPressMs     = 3000; // 3 Sekunden fuer Langdruck
        private const int DebounceMs      = 50;   // Entprellzeit
        private const int SimultaneousMs  = 200;  // Max. Zeitfenster fuer "gleichzeitig"
        private const int ShortPressMaxMs = 2999; // Alles unter 3s ist kurz

        // ─── Felder ──────────────────────────────────────────────────────────────
        private readonly ILogger<ButtonService> _logger;
        private readonly ModeService            _modeService;
        private readonly LedService             _ledService;

        private System.Device.Gpio.GpioController? _gpio;
        private bool _btn1Pressed;
        private bool _btn2Pressed;
        private DateTime _btn1PressTime;
        private DateTime _btn2PressTime;
        private readonly object _lock = new();
        private bool _disposed;

        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action? RebootRequested;
        public event Action? ShutdownRequested;

        // ─── Konstruktor ─────────────────────────────────────────────────────────
        public ButtonService(
            ILogger<ButtonService> logger,
            ModeService modeService,
            LedService ledService)
        {
            _logger      = logger;
            _modeService = modeService;
            _ledService  = ledService;
        }

        // ─── Initialisierung ─────────────────────────────────────────────────────
        public void Initialize()
        {
            try
            {
                _gpio = new System.Device.Gpio.GpioController();

                // BTN1 — Modus / Master-Key
                _gpio.OpenPin(GpioPins.Button1, System.Device.Gpio.PinMode.InputPullUp);
                _gpio.RegisterCallbackForPinValueChangedEvent(
                    GpioPins.Button1,
                    System.Device.Gpio.PinEventTypes.Rising | System.Device.Gpio.PinEventTypes.Falling,
                    OnBtn1Changed);

                // BTN2 — PIR Toggle / Standby
                _gpio.OpenPin(GpioPins.Button2, System.Device.Gpio.PinMode.InputPullUp);
                _gpio.RegisterCallbackForPinValueChangedEvent(
                    GpioPins.Button2,
                    System.Device.Gpio.PinEventTypes.Rising | System.Device.Gpio.PinEventTypes.Falling,
                    OnBtn2Changed);

                _logger.LogInformation("ButtonService: BTN1 (GPIO {b1}) und BTN2 (GPIO {b2}) initialisiert.",
                    GpioPins.Button1, GpioPins.Button2);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ButtonService: Konnte GPIO nicht oeffnen — {msg}", ex.Message);
                _gpio = null;
            }
        }

        // ─── BTN1 Handler ────────────────────────────────────────────────────────
        private void OnBtn1Changed(object sender, System.Device.Gpio.PinValueChangedEventArgs e)
        {
            lock (_lock)
            {
                if (e.ChangeType == System.Device.Gpio.PinEventTypes.Falling)
                {
                    // Taste gedrückt
                    _btn1Pressed  = true;
                    _btn1PressTime = DateTime.UtcNow;
                }
                else if (e.ChangeType == System.Device.Gpio.PinEventTypes.Rising && _btn1Pressed)
                {
                    // Taste losgelassen
                    _btn1Pressed = false;
                    var held = (DateTime.UtcNow - _btn1PressTime).TotalMilliseconds;

                    if (held >= LongPressMs)
                        HandleBtn1Long();
                    else if (held >= DebounceMs)
                        HandleBtn1Short();
                }
            }
        }

        // ─── BTN2 Handler ────────────────────────────────────────────────────────
        private void OnBtn2Changed(object sender, System.Device.Gpio.PinValueChangedEventArgs e)
        {
            lock (_lock)
            {
                if (e.ChangeType == System.Device.Gpio.PinEventTypes.Falling)
                {
                    _btn2Pressed  = true;
                    _btn2PressTime = DateTime.UtcNow;
                }
                else if (e.ChangeType == System.Device.Gpio.PinEventTypes.Rising && _btn2Pressed)
                {
                    _btn2Pressed = false;
                    var held = (DateTime.UtcNow - _btn2PressTime).TotalMilliseconds;

                    if (held >= LongPressMs)
                        HandleBtn2Long();
                    else if (held >= DebounceMs)
                        HandleBtn2Short();
                }
            }
        }

        // ─── Aktionen BTN1 ───────────────────────────────────────────────────────
        private void HandleBtn1Short()
        {
            // Pruefen ob BTN2 gleichzeitig gehalten wird (Kombo)
            if (_btn2Pressed && (DateTime.UtcNow - _btn2PressTime).TotalMilliseconds < SimultaneousMs)
            {
                HandleSimultaneous();
                return;
            }

            _logger.LogInformation("BTN1 kurz — Naechster Modus.");
            _modeService.NextMode();
        }

        private void HandleBtn1Long()
        {
            _logger.LogInformation("BTN1 lang (3s) — Master-Key Modus aktiviert.");
            _modeService.SetMode(GatewayMode.MasterKey);
        }

        // ─── Aktionen BTN2 ───────────────────────────────────────────────────────
        private void HandleBtn2Short()
        {
            // Pruefen ob BTN1 gleichzeitig gehalten wird (Kombo)
            if (_btn1Pressed && (DateTime.UtcNow - _btn1PressTime).TotalMilliseconds < SimultaneousMs)
            {
                HandleSimultaneous();
                return;
            }

            _logger.LogInformation("BTN2 kurz — PIR Toggle.");
            _modeService.TogglePir();
        }

        private void HandleBtn2Long()
        {
            _logger.LogInformation("BTN2 lang (3s) — Standby wird aktiviert.");
            _modeService.SetMode(GatewayMode.Standby);
        }

        // ─── Gleichzeitig-Kombo ──────────────────────────────────────────────────
        private int _simultaneousCount;
        private DateTime _lastSimultaneous = DateTime.MinValue;

        private void HandleSimultaneous()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastSimultaneous).TotalMilliseconds > 2000)
                _simultaneousCount = 0;

            _simultaneousCount++;
            _lastSimultaneous = now;

            _logger.LogInformation("Beide Buttons gleichzeitig — Zaehler: {c}", _simultaneousCount);

            if (_simultaneousCount == 2)
            {
                // 2x kurz => Reboot (3x gelb blinken)
                _logger.LogWarning("Reboot angefordert (2x gleichzeitig).");
                _ = Task.Run(async () =>
                {
                    await _ledService.BlinkAsync(GpioPins.LedWan, count: 3, onMs: 200, offMs: 200); // gelb
                    RebootRequested?.Invoke();
                });
                _simultaneousCount = 0;
            }
            else if (_simultaneousCount == 3)
            {
                // 3x kurz => Shutdown (3x rot blinken)
                _logger.LogWarning("Shutdown angefordert (3x gleichzeitig).");
                _ = Task.Run(async () =>
                {
                    await _ledService.BlinkAsync(GpioPins.LedError, count: 3, onMs: 200, offMs: 200); // rot
                    ShutdownRequested?.Invoke();
                });
                _simultaneousCount = 0;
            }
        }

        // ─── Dispose ─────────────────────────────────────────────────────────────
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _gpio?.ClosePin(GpioPins.Button1);
                _gpio?.ClosePin(GpioPins.Button2);
                _gpio?.Dispose();
            }
            catch { /* ignorieren */ }
        }
    }
}
