using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AweomaPi.Hardware;
using AweomaPi.Services;
using Microsoft.Extensions.Logging;

namespace AweomaPi
{
    /// <summary>
    /// AWEOMA Pi Gateway — Haupt-Einstiegspunkt.
    ///
    /// Ablauf:
    ///   1. Hardware erkennen (PCB Simple oder Extended)
    ///   2. Services initialisieren (nur fuer vorhandene Hardware)
    ///   3. Buttons registrieren (BTN1 / BTN2)
    ///   4. Hauptschleife starten
    /// </summary>
    internal class Program
    {
        private static ILoggerFactory?   _loggerFactory;
        private static ILogger<Program>? _logger;

        // Services
        private static LedService?     _ledService;
        private static DisplayService? _displayService;
        private static PirService?     _pirService;
        private static RfidService?    _rfidService;
        private static ModeService?    _modeService;
        private static ButtonService?  _buttonService;

        private static readonly CancellationTokenSource _cts = new();

        // ─── Main ────────────────────────────────────────────────────────────────
        static async Task Main(string[] args)
        {
            // Logger aufsetzen
            _loggerFactory = LoggerFactory.Create(b =>
                b.AddConsole().SetMinimumLevel(LogLevel.Information));
            _logger = _loggerFactory.CreateLogger<Program>();

            _logger.LogInformation("=== AWEOMA Pi Gateway startet ===");
            _logger.LogInformation("Version: 1.0.0 | Datum: {date}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            // Signalhandler fuer sauberes Beenden (Ctrl+C / systemd stop)
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _logger.LogInformation("Beenden-Signal empfangen.");
                _cts.Cancel();
            };

            // ─── 1. Hardware erkennen ──────────────────────────────────────────
            _logger.LogInformation("--- Hardware-Erkennung ---");
            var detector = new HardwareDetector(_loggerFactory.CreateLogger<HardwareDetector>());
            var hw = detector.Detect();

            _logger.LogInformation("PCB-Variante : {variant}", hw.Variant);
            _logger.LogInformation("Display (OLED): {v}", hw.HasOled     ? "GEFUNDEN (0x3C)" : "nicht vorhanden");
            _logger.LogInformation("RFID (RC522)  : {v}", hw.HasRfid     ? "GEFUNDEN"         : "nicht vorhanden");
            _logger.LogInformation("PIR (HC-SR501): {v}", hw.HasPir      ? "GEFUNDEN"         : "nicht vorhanden");
            _logger.LogInformation("Touch (TTP223): {v}", hw.HasTouch    ? "GEFUNDEN"         : "nicht vorhanden");
            _logger.LogInformation("BTN1 (GPIO 5) : {v}", hw.HasButton1  ? "GEFUNDEN"         : "nicht vorhanden");
            _logger.LogInformation("BTN2 (GPIO 6) : {v}", hw.HasButton2  ? "GEFUNDEN"         : "nicht vorhanden");
            _logger.LogInformation("--------------------------");

            // ─── 2. Services initialisieren ────────────────────────────────────
            _ledService = new LedService(
                _loggerFactory.CreateLogger<LedService>());
            _ledService.Initialize();

            if (hw.HasOled)
            {
                _displayService = new DisplayService(
                    _loggerFactory.CreateLogger<DisplayService>(),
                    touchPin: hw.HasTouch ? GpioPins.Touch : -1);
                _displayService.Initialize();
            }

            if (hw.HasPir)
            {
                _pirService = new PirService(
                    _loggerFactory.CreateLogger<PirService>());
                _pirService.Initialize();
            }

            if (hw.HasRfid)
            {
                _rfidService = new RfidService(
                    _loggerFactory.CreateLogger<RfidService>());
                _rfidService.Initialize();
            }

            // ModeService benoetigt LedService; Display + PIR optional
            _modeService = new ModeService(
                _loggerFactory.CreateLogger<ModeService>(),
                _ledService,
                _displayService,
                _pirService);

            // PIR-Ereignisse weiterleiten an ModeService
            if (_pirService != null)
            {
                _pirService.MotionDetected += () => _modeService.OnMotionDetected();
                _pirService.MotionTimeout  += () => _modeService.OnMotionTimeout();
            }

            // RFID-Karten-Ereignisse an ModeService weiterleiten
            if (_rfidService != null)
            {
                _rfidService.CardDetected += (cardId) => HandleRfidCard(cardId);
            }

            // ─── 3. Buttons initialisieren ──────────────────────────────────────
            if (hw.HasButton1 || hw.HasButton2)
            {
                _buttonService = new ButtonService(
                    _loggerFactory.CreateLogger<ButtonService>(),
                    _modeService,
                    _ledService);

                _buttonService.RebootRequested  += ExecuteReboot;
                _buttonService.ShutdownRequested += ExecuteShutdown;
                _buttonService.Initialize();

                _logger.LogInformation("Buttons initialisiert: BTN1={b1}, BTN2={b2}",
                    hw.HasButton1, hw.HasButton2);
            }
            else
            {
                _logger.LogWarning("Keine Buttons gefunden — Modus-Wechsel nur per RFID moeglich.");
            }

            // ─── 4. Startmodus setzen ───────────────────────────────────────────
            _modeService.SetMode(GatewayMode.Normal);
            _ledService.SetStatusAsync(LedStatus.Normal);

            _logger.LogInformation("=== AWEOMA Gateway bereit ===");
            _logger.LogInformation("BTN1 kurz = naechster Modus | BTN1 3s = Master-Key");
            _logger.LogInformation("BTN2 kurz = PIR toggle      | BTN2 3s = Standby");
            _logger.LogInformation("Beide 2x  = Reboot          | Beide 3x = Shutdown");

            // ─── 5. Hauptschleife ───────────────────────────────────────────────
            try
            {
                await RunMainLoopAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Hauptschleife beendet.");
            }
            finally
            {
                Cleanup();
            }

            _logger.LogInformation("=== AWEOMA Gateway gestoppt ===");
        }

        // ─── Hauptschleife ────────────────────────────────────────────────────────
        private static async Task RunMainLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Status-LED periodisch aktualisieren (alle 30 Sekunden)
                if (_modeService?.CurrentMode != GatewayMode.Standby)
                {
                    await UpdateStatusLedsAsync();
                }

                await Task.Delay(30_000, ct);
            }
        }

        // ─── LED-Status-Update ────────────────────────────────────────────────────
        private static async Task UpdateStatusLedsAsync()
        {
            if (_ledService == null) return;

            // WAN pruefen (Ping Google DNS)
            bool wanOk = await CheckWanAsync();
            bool vpnOk = CheckVpnActive();

            await _ledService.SetStatusAsync(new LedStatus
            {
                Power = true,
                Vpn   = vpnOk,
                Wan   = wanOk,
                Error = !wanOk || !vpnOk,
            });
        }

        private static async Task<bool> CheckWanAsync()
        {
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 2000);
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
            catch { return false; }
        }

        private static bool CheckVpnActive()
        {
            try
            {
                // WireGuard wg0 Interface pruefen
                var result = RunCommand("wg", "show wg0");
                return result.Contains("interface: wg0");
            }
            catch { return false; }
        }

        // ─── RFID Karten-Handler ──────────────────────────────────────────────────
        private static void HandleRfidCard(string cardId)
        {
            if (_modeService == null) return;

            _logger?.LogInformation("RFID Karte erkannt: {id}", cardId);

            // Karten-IDs => Modus (in Produktion aus Config-Datei lesen)
            var mode = cardId switch
            {
                "CARD_1" => GatewayMode.Normal,
                "CARD_2" => GatewayMode.Minimal,
                "CARD_3" => GatewayMode.Night,
                "CARD_4" => GatewayMode.Standby,
                "MASTER" => GatewayMode.MasterKey,
                _        => (GatewayMode?)null,
            };

            if (mode.HasValue)
                _modeService.SetMode(mode.Value);
            else
                _logger?.LogWarning("Unbekannte RFID Karte: {id}", cardId);
        }

        // ─── Reboot / Shutdown ────────────────────────────────────────────────────
        private static void ExecuteReboot()
        {
            _logger?.LogWarning("REBOOT wird ausgefuehrt...");
            Cleanup();
            RunCommand("sudo", "reboot");
        }

        private static void ExecuteShutdown()
        {
            _logger?.LogWarning("SHUTDOWN wird ausgefuehrt...");
            Cleanup();
            RunCommand("sudo", "shutdown -h now");
        }

        // ─── Cleanup ─────────────────────────────────────────────────────────────
        private static void Cleanup()
        {
            _logger?.LogInformation("Cleanup...");
            _buttonService?.Dispose();
            _pirService?.Dispose();
            _rfidService?.Dispose();
            _displayService?.Dispose();
            _ledService?.Dispose();
            _loggerFactory?.Dispose();
        }

        // ─── Hilfsmethode: Shell-Befehl ausfuehren ────────────────────────────────
        private static string RunCommand(string cmd, string args)
        {
            try
            {
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo(cmd, args)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                };
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("RunCommand Fehler ({cmd} {args}): {msg}", cmd, args, ex.Message);
                return string.Empty;
            }
        }
    }
}
