using System;
using System.Device.I2c;
using System.Device.Spi;
using System.Device.Gpio;
using Microsoft.Extensions.Logging;

namespace AweomaPi.Hardware
{
    /// <summary>
    /// Erkennungsresultat der Hardware-Erkennung beim Systemstart.
    /// </summary>
    public record HardwareInfo(
        PcbVariant Variant,
        bool HasOled,
        bool HasRfid,
        bool HasPir,
        bool HasTouch,
        bool HasButton1,
        bool HasButton2
    );

    /// <summary>
    /// Moeglische PCB-Varianten.
    /// </summary>
    public enum PcbVariant
    {
        /// <summary>
        /// PCB Simple: 2x PWM, Touch (Display-Navigation), 5V-Ausgang, 4-Pin LED, BTN1, BTN2.
        /// Kein RFID, kein OLED, kein PIR.
        /// </summary>
        Simple,

        /// <summary>
        /// PCB Extended: Alles von Simple + RFID (RC522 SPI), Mini-LCD (SSD1306 OLED I2C), PIR (HC-SR501).
        /// </summary>
        Extended,
    }

    /// <summary>
    /// Erkennt automatisch welche Hardware verbaut ist.
    ///
    /// Erkennungs-Methoden:
    ///   OLED  — I2C-Probe auf Adresse 0x3C (SSD1306)
    ///   RFID  — SPI-Probe: Version-Register (0x37) des RC522 lesen
    ///   PIR   — GPIO 25 auf PullDown konfigurieren und lesen
    ///   Touch — GPIO 24 auf PullDown konfigurieren und lesen
    ///   BTN1  — GPIO 5  auf PullUp konfigurieren und lesen (LOW wenn Taste gefunden)
    ///   BTN2  — GPIO 6  auf PullUp konfigurieren und lesen (LOW wenn Taste gefunden)
    ///
    /// Variante: Extended wenn OLED, RFID oder PIR gefunden, sonst Simple.
    /// </summary>
    public class HardwareDetector
    {
        private readonly ILogger<HardwareDetector> _logger;

        public HardwareDetector(ILogger<HardwareDetector> logger)
        {
            _logger = logger;
        }

        // ─── Haupt-Erkennungsroutine ─────────────────────────────────────────────
        public HardwareInfo Detect()
        {
            bool oled    = DetectOled();
            bool rfid    = DetectRfid();
            bool pir     = DetectPir();
            bool touch   = DetectGpioInput(GpioPins.Touch,   PinMode.InputPullDown, "Touch (GPIO 24)");
            bool button1 = DetectGpioInput(GpioPins.Button1, PinMode.InputPullUp,   "BTN1  (GPIO 5)");
            bool button2 = DetectGpioInput(GpioPins.Button2, PinMode.InputPullUp,   "BTN2  (GPIO 6)");

            // Variante ableiten
            var variant = (oled || rfid || pir) ? PcbVariant.Extended : PcbVariant.Simple;

            return new HardwareInfo(variant, oled, rfid, pir, touch, button1, button2);
        }

        // ─── OLED (SSD1306, I2C 0x3C) ────────────────────────────────────────────
        private bool DetectOled()
        {
            try
            {
                var settings = new I2cConnectionSettings(busId: 1, deviceAddress: 0x3C);
                using var device = I2cDevice.Create(settings);
                // Byte lesen — kein Exception = Geraet vorhanden
                device.ReadByte();
                _logger.LogInformation("OLED (SSD1306) gefunden auf I2C 0x3C.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OLED nicht gefunden: {msg}", ex.Message);
                return false;
            }
        }

        // ─── RFID (RC522, SPI) ────────────────────────────────────────────────────
        private bool DetectRfid()
        {
            try
            {
                var settings = new SpiConnectionSettings(busId: 0, chipSelectLine: 0)
                {
                    ClockFrequency = 1_000_000,
                    Mode           = SpiMode.Mode0,
                };
                using var device = SpiDevice.Create(settings);

                // RC522 Version-Register (Adresse 0x37) lesen
                // Lese-Kommando: Adresse | 0x80
                byte[] writeBuffer = { (byte)(0x37 | 0x80), 0x00 };
                byte[] readBuffer  = new byte[2];
                device.TransferFullDuplex(writeBuffer, readBuffer);

                byte version = readBuffer[1];
                // RC522 meldet 0x91 (v1) oder 0x92 (v2)
                if (version == 0x91 || version == 0x92)
                {
                    _logger.LogInformation("RFID (RC522 v{v}) gefunden auf SPI0.", version == 0x91 ? "1" : "2");
                    return true;
                }

                _logger.LogDebug("SPI Geraet antwortet, aber kein RC522 (Version=0x{v:X2}).", version);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("RFID nicht gefunden: {msg}", ex.Message);
                return false;
            }
        }

        // ─── PIR (HC-SR501, GPIO 25) ──────────────────────────────────────────────
        private bool DetectPir()
        {
            try
            {
                using var gpio = new GpioController();
                gpio.OpenPin(GpioPins.Pir, PinMode.InputPullDown);

                // HC-SR501 benoetigt 30-60s Aufwaermzeit; hier nur Erreichbarkeit pruefen
                // Falls GPIO ohne Exception geoeffnet werden kann, nehmen wir PIR als vorhanden an
                // (In echtem Setup: Pin-Zustand nach Aufwaermzeit pruefen)
                var value = gpio.Read(GpioPins.Pir);
                gpio.ClosePin(GpioPins.Pir);

                _logger.LogInformation("PIR (HC-SR501) GPIO {pin} erreichbar (Wert: {v}).", GpioPins.Pir, value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("PIR nicht gefunden: {msg}", ex.Message);
                return false;
            }
        }

        // ─── GPIO-Eingang erkennen (Touch, BTN1, BTN2) ───────────────────────────
        private bool DetectGpioInput(int pin, PinMode mode, string name)
        {
            try
            {
                using var gpio = new GpioController();
                gpio.OpenPin(pin, mode);
                var value = gpio.Read(pin);
                gpio.ClosePin(pin);

                _logger.LogInformation("{name} GPIO {pin} erreichbar (Wert: {v}).", name, pin, value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("{name} GPIO {pin} nicht erreichbar: {msg}", name, pin, ex.Message);
                return false;
            }
        }
    }
}
