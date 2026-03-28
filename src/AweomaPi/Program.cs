using AweomaPi.Hardware;
using AweomaPi.Services;
using Microsoft.Extensions.Logging;

// ====================================================================
// AWEOMA – Raspberry Pi Gateway Hardware Controller
// Einstiegspunkt: erkennt aktive Hardware, startet passende Services.
// ====================================================================

// Logging einrichten
using var logFactory = LoggerFactory.Create(b =>
    b.AddConsole().SetMinimumLevel(LogLevel.Information));

var log = logFactory.CreateLogger("AWEOMA");

Console.WriteLine();
Console.WriteLine("  ╔══════════════════════════════════════╗");
Console.WriteLine("  ║   AWEOMA – Raspberry Pi Gateway      ║");
Console.WriteLine("  ║   Hardware Controller v1.0           ║");
Console.WriteLine("  ╚══════════════════════════════════════╝");
Console.WriteLine();

// ----------------------------------------------------------------
// Schritt 1: Hardware erkennen
// ----------------------------------------------------------------
log.LogInformation("Schritt 1/3 – Hardware-Erkennung...");
var detector = new HardwareDetector(logFactory.CreateLogger<HardwareDetector>());
HardwareProfile hw = detector.Detect();

Console.WriteLine();
Console.WriteLine($"  PCB-Variante : {hw.Variant}");
Console.WriteLine($"  LEDs (4x)    : {(hw.HasLeds            ? "✓ erkannt" : "✗ nicht verfuegbar")}");
Console.WriteLine($"  PWM (2x)     : {(hw.HasPwm             ? "✓ erkannt" : "✗ nicht verfuegbar")}");
Console.WriteLine($"  Touch/Display: {(hw.HasTouchForDisplay  ? "✓ erkannt" : "✗ nicht verfuegbar")}");
Console.WriteLine($"  OLED-Display : {(hw.HasOledDisplay      ? "✓ erkannt" : "✗ nicht vorhanden (Simple)")}");
Console.WriteLine($"  RFID RC522   : {(hw.HasRfidReader       ? "✓ erkannt" : "✗ nicht vorhanden (Simple)")}");
Console.WriteLine($"  PIR HC-SR501 : {(hw.HasPirSensor        ? "✓ erkannt" : "✗ nicht vorhanden (Simple)")}");
Console.WriteLine();

// ----------------------------------------------------------------
// Schritt 2: Services initialisieren (nur aktive Komponenten)
// ----------------------------------------------------------------
log.LogInformation("Schritt 2/3 – Services starten...");

// LEDs – immer (beide Varianten)
var leds = new LedService(hw, logFactory.CreateLogger<LedService>());
leds.Initialize();

// Display + Touch-Navigation – nur Extended (oder Simple ohne Display -> kein Fehler)
var display = new DisplayService(hw, logFactory.CreateLogger<DisplayService>());
display.Initialize();

// RFID – nur Extended
var rfid = new RfidService(hw, logFactory.CreateLogger<RfidService>());
rfid.Initialize();

// PIR – nur Extended
var pir = new PirService(hw, logFactory.CreateLogger<PirService>());
pir.Initialize();

// ----------------------------------------------------------------
// Events verbinden
// ----------------------------------------------------------------

// RFID-Tag -> LED blinken + Display aktualisieren
rfid.TagDetected += (_, e) =>
{
    log.LogInformation("[Event] RFID-Tag: {Tag}", e.TagId);
    display.LastRfidTag = e.TagId;
    display.ShowCurrentPage();
    _ = leds.BlinkAsync(GpioPins.LedVpn, times: 2, intervalMs: 100);
};

// Bewegung -> Error-LED an + Display aktualisieren
pir.MotionDetected += (_, e) =>
{
    log.LogInformation("[Event] Bewegung erkannt um {Time}", e.Timestamp.ToLocalTime());
    display.PirStatus = $"Aktiv {e.Timestamp:HH:mm:ss}";
    display.ShowCurrentPage();
    leds.SetError(true);
};

pir.MotionEnded += (_, e) =>
{
    display.PirStatus = "Ruhig";
    display.ShowCurrentPage();
    leds.SetError(false);
};

// ----------------------------------------------------------------
// Schritt 3: Haupt-Loop
// ----------------------------------------------------------------
log.LogInformation("Schritt 3/3 – Haupt-Loop gestartet. [Ctrl+C zum Beenden]");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    log.LogInformation("Beende AWEOMA...");
};

// Statusdaten periodisch aktualisieren
var updateTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));

try
{
    while (await updateTimer.WaitForNextTickAsync(cts.Token))
    {
        // System-Infos lesen
        display.IpAddress    = GetLocalIp();
        display.CpuTemp      = GetCpuTemp();
        display.CpuLoad      = GetCpuLoad();
        display.VpnStatus    = GetWgStatus();
        display.PiholeStatus = GetPiholeStatus();
        display.PirStatus    = pir.StatusText;

        // Display aktualisieren (nur wenn Extended + Display vorhanden)
        display.ShowCurrentPage();

        // WAN-LED: Netzwerkverbindung pruefen
        leds.SetWan(CheckInternet());
    }
}
catch (OperationCanceledException)
{
    // Normales Ende durch Ctrl+C
}

// ----------------------------------------------------------------
// Cleanup
// ----------------------------------------------------------------
log.LogInformation("Services werden beendet...");
display.Dispose();
rfid.Dispose();
pir.Dispose();
leds.SetPower(false);
leds.Dispose();
Console.WriteLine("Auf Wiedersehen!");

// ====================================================================
// Hilfsmethoden
// ====================================================================

static string GetLocalIp()
{
    try
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return ip.ToString();
        return "N/A";
    }
    catch { return "Fehler"; }
}

static string GetCpuTemp()
{
    try
    {
        var raw = File.ReadAllText("/sys/class/thermal/thermal_zone0/temp");
        return $"{int.Parse(raw.Trim()) / 1000.0:F1}";
    }
    catch { return "N/A"; }
}

static string GetCpuLoad()
{
    try
    {
        // /proc/stat auslesen fuer CPU-Last
        var lines = File.ReadAllLines("/proc/stat");
        var cpu   = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        long user = long.Parse(cpu[1]), nice = long.Parse(cpu[2]),
             sys  = long.Parse(cpu[3]), idle = long.Parse(cpu[4]);
        long total = user + nice + sys + idle;
        double load = total > 0 ? (total - idle) * 100.0 / total : 0;
        return $"{load:F1}";
    }
    catch { return "N/A"; }
}

static string GetWgStatus()
{
    try
    {
        var result = RunCommand("wg", "show wg0");
        return result.Contains("peer") ? "Aktiv" : "Getrennt";
    }
    catch { return "N/A"; }
}

static string GetPiholeStatus()
{
    try
    {
        var result = RunCommand("pihole", "status");
        return result.Contains("Active") ? "ON" : "OFF";
    }
    catch { return "N/A"; }
}

static bool CheckInternet()
{
    try
    {
        using var ping = new System.Net.NetworkInformation.Ping();
        var reply = ping.Send("1.1.1.1", 1000);
        return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
    }
    catch { return false; }
}

static string RunCommand(string cmd, string args)
{
    var psi = new System.Diagnostics.ProcessStartInfo(cmd, args)
    {
        RedirectStandardOutput = true,
        UseShellExecute        = false,
        CreateNoWindow         = true
    };
    using var proc = System.Diagnostics.Process.Start(psi)!;
    string output = proc.StandardOutput.ReadToEnd();
    proc.WaitForExit();
    return output;
}
