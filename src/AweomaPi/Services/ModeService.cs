using System;
using System.Threading.Tasks;
using AweomaPi.Hardware;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Services
{
    /// <summary>
    /// Die 6 Betriebsmodi des AWEOMA Pi Gateways.
    /// Wechsel per BTN1 (kurz), RFID-Karte oder intern (PIR Auto).
    /// </summary>
    public enum GatewayMode
    {
        /// <summary>Alles an — Display + LCD + LEDs + Luefter auto (Karte 1 / BTN1 kurz)</summary>
        Normal     = 0,

        /// <summary>Nur LCD + LEDs, grosses Display aus (Karte 2 / BTN1 kurz)</summary>
        Minimal    = 1,

        /// <summary>Nur LEDs gedimmt 10%, LCD + Display aus (Karte 3 / BTN1 kurz)</summary>
        Night      = 2,

        /// <summary>Alles aus, Pi laeuft nur im Hintergrund (Karte 4 / BTN2 lang)</summary>
        Standby    = 3,

        /// <summary>Schaltet durch alle Modi der Reihe nach (Master-Karte / BTN1 lang 3s)</summary>
        MasterKey  = 4,

        /// <summary>Display/LEDs folgen PIR automatisch (automatisch aktiv wenn PIR an)</summary>
        PirAuto    = 5,
    }

    /// <summary>
    /// Verwaltet den aktuellen Betriebsmodus und alle Modusuebergaenge.
    /// Steuert Display, LEDs und Luefter entsprechend dem Modus.
    /// </summary>
    public class ModeService
    {
        // ─── Felder ──────────────────────────────────────────────────────────────
        private readonly ILogger<ModeService> _logger;
        private readonly LedService           _ledService;
        private readonly DisplayService?      _displayService;   // null wenn kein Extended-PCB
        private readonly PirService?          _pirService;       // null wenn kein Extended-PCB

        private GatewayMode _currentMode = GatewayMode.Normal;
        private bool _pirEnabled = true;
        private readonly object _lock = new();

        // Reihenfolge fuer BTN1 Zyklus (ohne MasterKey + PirAuto — die werden separat gesetzt)
        private static readonly GatewayMode[] CycleOrder =
        {
            GatewayMode.Normal,
            GatewayMode.Minimal,
            GatewayMode.Night,
            GatewayMode.Standby,
        };

        // ─── Properties ──────────────────────────────────────────────────────────
        public GatewayMode CurrentMode => _currentMode;
        public bool PirEnabled         => _pirEnabled;

        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action<GatewayMode>? ModeChanged;

        // ─── Konstruktor ─────────────────────────────────────────────────────────
        public ModeService(
            ILogger<ModeService> logger,
            LedService ledService,
            DisplayService? displayService = null,
            PirService? pirService = null)
        {
            _logger         = logger;
            _ledService     = ledService;
            _displayService = displayService;
            _pirService     = pirService;
        }

        // ─── Modus-Wechsel ───────────────────────────────────────────────────────

        /// <summary>Naechster Modus im Zyklus (BTN1 kurz).</summary>
        public void NextMode()
        {
            lock (_lock)
            {
                int idx = Array.IndexOf(CycleOrder, _currentMode);
                int next = (idx < 0 ? 0 : (idx + 1) % CycleOrder.Length);
                SetModeInternal(CycleOrder[next]);
            }
        }

        /// <summary>Direkt einen Modus setzen (RFID-Karte, BTN1 lang, BTN2 lang).</summary>
        public void SetMode(GatewayMode mode)
        {
            lock (_lock)
            {
                if (mode == GatewayMode.MasterKey)
                {
                    // MasterKey: einmal durch alle Modi der Reihe schalten
                    _ = Task.Run(CycleThroughAllModesAsync);
                    return;
                }
                SetModeInternal(mode);
            }
        }

        /// <summary>PIR-Sensor an/aus toggle (BTN2 kurz).</summary>
        public void TogglePir()
        {
            lock (_lock)
            {
                _pirEnabled = !_pirEnabled;
                _logger.LogInformation("PIR-Sensor: {state}", _pirEnabled ? "AN" : "AUS");

                if (!_pirEnabled)
                {
                    // PIR aus => Display + LEDs immer an (letzter Modus vor PirAuto wiederherstellen)
                    if (_currentMode == GatewayMode.PirAuto)
                        SetModeInternal(GatewayMode.Normal);
                }
                else
                {
                    // PIR an => PIR-Auto Modus aktivieren
                    SetModeInternal(GatewayMode.PirAuto);
                }
            }
        }

        // ─── Interne Logik ───────────────────────────────────────────────────────
        private void SetModeInternal(GatewayMode mode)
        {
            if (_currentMode == mode && mode != GatewayMode.MasterKey) return;

            _currentMode = mode;
            _logger.LogInformation("Modus gewechselt => {mode}", mode);

            ApplyMode(mode);
            ModeChanged?.Invoke(mode);
        }

        private void ApplyMode(GatewayMode mode)
        {
            switch (mode)
            {
                case GatewayMode.Normal:
                    // Alles an — Display + LCD + LEDs + Luefter auto
                    _displayService?.SetBacklight(true);
                    _ledService.SetBrightness(100);
                    _ledService.SetStatusAsync(LedStatus.Normal);
                    SetFanAuto();
                    break;

                case GatewayMode.Minimal:
                    // Nur LCD + LEDs, grosses Display aus
                    _displayService?.SetBacklight(false);
                    _ledService.SetBrightness(100);
                    _ledService.SetStatusAsync(LedStatus.Normal);
                    break;

                case GatewayMode.Night:
                    // Nur LEDs gedimmt 10%, LCD + Display aus
                    _displayService?.SetBacklight(false);
                    _ledService.SetBrightness(10);
                    _ledService.SetStatusAsync(LedStatus.Normal);
                    SetFanOff();
                    break;

                case GatewayMode.Standby:
                    // Alles aus
                    _displayService?.SetBacklight(false);
                    _ledService.SetBrightness(0);
                    _ledService.AllOff();
                    SetFanOff();
                    break;

                case GatewayMode.PirAuto:
                    // Wird durch PirService gesteuert — hier nur Initialisierung
                    _logger.LogInformation("PIR-Auto Modus aktiv — Display/LEDs folgen Bewegungssensor.");
                    break;
            }
        }

        /// <summary>
        /// Wird vom PirService aufgerufen wenn Bewegung erkannt wird (PIR-Auto Modus).
        /// </summary>
        public void OnMotionDetected()
        {
            if (_currentMode != GatewayMode.PirAuto) return;
            _displayService?.SetBacklight(true);
            _ledService.SetBrightness(100);
            _ledService.SetStatusAsync(LedStatus.Normal);
        }

        /// <summary>
        /// Wird vom PirService aufgerufen wenn 5 Minuten keine Bewegung (PIR-Auto Modus).
        /// </summary>
        public void OnMotionTimeout()
        {
            if (_currentMode != GatewayMode.PirAuto) return;
            _displayService?.SetBacklight(false);
            _ledService.SetBrightness(0);
            _ledService.AllOff();
        }

        // ─── Master-Key Zyklus ───────────────────────────────────────────────────
        private async Task CycleThroughAllModesAsync()
        {
            _logger.LogInformation("Master-Key: Schalte durch alle Modi.");
            foreach (var mode in CycleOrder)
            {
                lock (_lock) { SetModeInternal(mode); }
                await Task.Delay(1500); // 1,5s pro Modus anzeigen
            }
        }

        // ─── Luefter-Hilfsmethoden ───────────────────────────────────────────────
        private void SetFanAuto()
        {
            // PWM-Luefter auf automatische Temperatur-Steuerung
            // (wird von separatem FanService gesteuert, hier nur Signal)
            _logger.LogDebug("Luefter: Auto-Modus.");
        }

        private void SetFanOff()
        {
            _logger.LogDebug("Luefter: Aus.");
        }
    }
}
