using System.Device.Spi;
using AweomaPi.Hardware;
using Iot.Device.Mfrc522;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Services;

/// <summary>
/// Liest RFID-Tags mit dem RC522-Modul (SPI).
/// Nur aktiv wenn PCB Extended und RFID erkannt wurde.
/// Event: TagDetected wird ausgeloest wenn eine Karte/Tag gelesen wird.
/// </summary>
public sealed class RfidService : IDisposable
{
    private readonly ILogger<RfidService> _log;
    private readonly HardwareProfile _hw;

    private MfRc522? _reader;
    private CancellationTokenSource? _cts;
    private Task? _scanTask;

    /// <summary>Wird ausgeloest wenn ein RFID-Tag erkannt wird.</summary>
    public event EventHandler<RfidTagEventArgs>? TagDetected;

    public RfidService(HardwareProfile hw, ILogger<RfidService> log)
    {
        _hw  = hw;
        _log = log;
    }

    public void Initialize()
    {
        if (!_hw.HasRfidReader)
        {
            _log.LogInformation("[RFID] Kein RC522 erkannt (nur PCB Extended) – RfidService deaktiviert.");
            return;
        }

        try
        {
            var spiSettings = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 1_000_000,
                Mode = SpiMode.Mode0
            };
            var spiDevice = SpiDevice.Create(spiSettings);
            _reader = new MfRc522(spiDevice);

            _log.LogInformation("[RFID] RC522 bereit. Warte auf Tags...");

            // Scan-Loop starten
            _cts      = new CancellationTokenSource();
            _scanTask = Task.Run(() => ScanLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[RFID] Initialisierung fehlgeschlagen.");
        }
    }

    private async Task ScanLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_reader is not null && _reader.IsTagPresent())
                {
                    var uid = _reader.ReadCardSerial();
                    if (uid is { Length: > 0 })
                    {
                        var tagHex = BitConverter.ToString(uid).Replace("-", ":");
                        _log.LogInformation("[RFID] Tag erkannt: {Tag}", tagHex);
                        TagDetected?.Invoke(this, new RfidTagEventArgs(tagHex));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug("[RFID] Scan-Fehler: {Msg}", ex.Message);
            }

            await Task.Delay(300, ct);  // Scan-Intervall 300ms
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _scanTask?.Wait(TimeSpan.FromSeconds(2));
        _reader?.Dispose();
        _cts?.Dispose();
    }
}

public sealed record RfidTagEventArgs(string TagId) : EventArgs;
