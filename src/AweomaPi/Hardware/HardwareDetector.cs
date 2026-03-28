using System.Device.I2c;
using System.Device.Spi;
using System.Device.Gpio;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Hardware;

/// <summary>
/// Ergebnis der Hardware-Erkennung.
/// </summary>
public sealed record HardwareProfile
{
    // PCB-Variante
    public PcbVariant Variant         { get; init; } = PcbVariant.Unknown;

    // Immer vorhanden (PCB Simple)
    public bool HasLeds               { get; init; }
    public bool HasPwm                { get; init; }
    public bool HasTouchForDisplay    { get; init; }  // Touch steuert das Display
    public bool HasResetButton        { get; init; }

    // Nur PCB Extended
    public bool HasOledDisplay        { get; init; }  // I2C Display (SSD1306)
    public bool HasRfidReader         { get; init; }  // SPI RC522
    public bool HasPirSensor          { get; init; }  // PIR HC-SR501

    public override string ToString() =>
        $"PCB: {Variant} | LEDs={HasLeds} | PWM={HasPwm} | " +
        $"Touch(Display)={HasTouchForDisplay} | OLED={HasOledDisplay} | " +
        $"RFID={HasRfidReader} | PIR={HasPirSensor}";
}

public enum PcbVariant
{
    Unknown,
    Simple,
    Extended
}

/// <summary>
/// Erkennt welche Hardware-Komponenten aktiv/vorhanden sind.
/// Strategie:
///   1. I2C-Bus auf bekannte Adressen pruefen -> Display vorhanden?
///   2. SPI-Bus pruefen -> RFID vorhanden?
///   3. GPIO-Pins pruefen -> Touch, PIR, LEDs, PWM
///   4. Aus den Ergebnissen PCB-Variante ableiten
/// </summary>
public sealed class HardwareDetector
{
    private readonly ILogger<HardwareDetector> _log;

    public HardwareDetector(ILogger<HardwareDetector> log)
    {
        _log = log;
    }

    public HardwareProfile Detect()
    {
        _log.LogInformation("=== AWEOMA Hardware-Erkennung gestartet ===");

        bool hasDisplay = ProbeI2cDisplay();
        bool hasRfid    = ProbeSpiRfid();
        bool hasPir     = ProbeGpioInput(GpioPins.PirSensor, "PIR HC-SR501");
        bool hasTouch   = ProbeGpioInput(GpioPins.TouchDisplay, "Touch TTP223 (Display)");
        bool hasLeds    = ProbeGpioOutputs();
        bool hasPwm     = ProbeGpioPwm();

        // PCB-Variante ableiten:
        // Extended = Display (I2C) UND/ODER RFID (SPI) UND/ODER PIR vorhanden
        var variant = (hasDisplay || hasRfid || hasPir)
            ? PcbVariant.Extended
            : PcbVariant.Simple;

        var profile = new HardwareProfile
        {
            Variant          = variant,
            HasLeds          = hasLeds,
            HasPwm           = hasPwm,
            HasTouchForDisplay = hasTouch,
            HasResetButton   = true,  // immer vorhanden, nicht detektierbar ohne Druecken
            HasOledDisplay   = hasDisplay,
            HasRfidReader    = hasRfid,
            HasPirSensor     = hasPir,
        };

        _log.LogInformation("=== Hardware-Profil: {Profile} ===", profile);
        return profile;
    }

    // ----------------------------------------------------------------
    // I2C: Pruefe ob SSD1306 OLED auf Adresse 0x3C antwortet
    // ----------------------------------------------------------------
    private bool ProbeI2cDisplay()
    {
        try
        {
            var settings = new I2cConnectionSettings(GpioPins.I2CBus, GpioPins.DisplayI2CAddr);
            using var device = I2cDevice.Create(settings);

            // Sende 0x00 (Command-Byte) und pruefe ob ACK kommt
            device.WriteByte(0x00);
            _log.LogInformation("[I2C] OLED-Display (SSD1306) erkannt auf Bus {Bus}, Adresse 0x{Addr:X2}",
                GpioPins.I2CBus, GpioPins.DisplayI2CAddr);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogDebug("[I2C] Kein Display erkannt: {Msg}", ex.Message);
            return false;
        }
    }

    // ----------------------------------------------------------------
    // SPI: Pruefe ob RC522 RFID auf SPI Bus 0 antwortet
    // ----------------------------------------------------------------
    private bool ProbeSpiRfid()
    {
        try
        {
            var settings = new SpiConnectionSettings(0, 0)  // Bus 0, CS 0 (CE0 = GPIO 8)
            {
                ClockFrequency = 1_000_000,
                Mode = SpiMode.Mode0
            };
            using var device = SpiDevice.Create(settings);

            // RC522: Register 0x37 lesen (Version-Register)
            // Sende: Read-Bit (MSB=1) + Adresse + 0x00 fuer Empfang
            Span<byte> writeBuffer = stackalloc byte[] { 0x37 << 1 | 0x80, 0x00 };
            Span<byte> readBuffer  = stackalloc byte[2];
            device.TransferFullDuplex(writeBuffer, readBuffer);

            byte version = readBuffer[1];
            bool isRc522 = version is 0x91 or 0x92;  // typische RC522-Versions-Bytes

            if (isRc522)
                _log.LogInformation("[SPI] RFID RC522 erkannt (Version: 0x{V:X2})", version);
            else
                _log.LogDebug("[SPI] SPI-Geraet vorhanden aber unbekannt (Version: 0x{V:X2})", version);

            return isRc522;
        }
        catch (Exception ex)
        {
            _log.LogDebug("[SPI] Kein RFID erkannt: {Msg}", ex.Message);
            return false;
        }
    }

    // ----------------------------------------------------------------
    // GPIO Input: Pruefe ob Pin les- und setzbar ist
    // ----------------------------------------------------------------
    private bool ProbeGpioInput(int pin, string name)
    {
        try
        {
            using var controller = new GpioController();
            controller.OpenPin(pin, PinMode.InputPullDown);
            _ = controller.Read(pin);
            controller.ClosePin(pin);
            _log.LogInformation("[GPIO] {Name} erkannt auf Pin BCM {Pin}", name, pin);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogDebug("[GPIO] {Name} (Pin {Pin}) nicht erreichbar: {Msg}", name, pin, ex.Message);
            return false;
        }
    }

    // ----------------------------------------------------------------
    // GPIO Output: LEDs pruefen (PCB Simple Grundausstattung)
    // ----------------------------------------------------------------
    private bool ProbeGpioOutputs()
    {
        var ledPins = new[] { GpioPins.LedPower, GpioPins.LedVpn, GpioPins.LedWan, GpioPins.LedError };
        int ok = 0;
        foreach (var pin in ledPins)
        {
            try
            {
                using var controller = new GpioController();
                controller.OpenPin(pin, PinMode.Output);
                controller.Write(pin, PinValue.Low);
                controller.ClosePin(pin);
                ok++;
            }
            catch { /* pin nicht verfuegbar */ }
        }
        bool hasLeds = ok == ledPins.Length;
        _log.LogInformation("[GPIO] LEDs: {Ok}/{Total} Pins erreichbar", ok, ledPins.Length);
        return hasLeds;
    }

    // ----------------------------------------------------------------
    // PWM: Pruefe ob Hardware-PWM Pins verfuegbar sind
    // ----------------------------------------------------------------
    private bool ProbeGpioPwm()
    {
        try
        {
            using var controller = new GpioController();
            controller.OpenPin(GpioPins.Pwm0, PinMode.Output);
            controller.ClosePin(GpioPins.Pwm0);
            _log.LogInformation("[GPIO] PWM-Pins erkannt (GPIO 12, 13)");
            return true;
        }
        catch (Exception ex)
        {
            _log.LogDebug("[GPIO] PWM-Pins nicht erreichbar: {Msg}", ex.Message);
            return false;
        }
    }
}
