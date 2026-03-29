namespace AweomaPi.Hardware
{
    /// <summary>
    /// Zentrale Definition aller GPIO-Pins fuer das AWEOMA Pi Gateway.
    /// Gilt fuer beide PCB-Varianten (Simple + Extended).
    /// </summary>
    public static class GpioPins
    {
        // ─── LEDs (4-Pin LED-Leiste) ────────────────────────────────────────────
        /// <summary>Power-LED (gruen) — System laeuft</summary>
        public const int LedPower = 17;

        /// <summary>VPN-LED (blau) — WireGuard aktiv</summary>
        public const int LedVpn = 22;

        /// <summary>WAN-LED (gelb) — Internet-Verbindung</summary>
        public const int LedWan = 23;

        /// <summary>Error-LED (rot) — Fehler oder Warnung</summary>
        public const int LedError = 27;

        // ─── PWM-Ausgaenge (PCB Simple + Extended) ──────────────────────────────
        /// <summary>PWM Kanal 1 — z.B. Luefter-Steuerung</summary>
        public const int Pwm1 = 12;

        /// <summary>PWM Kanal 2 — z.B. zweiter Luefter oder Dimmer</summary>
        public const int Pwm2 = 13;

        // ─── Touch (PCB Simple + Extended) ──────────────────────────────────────
        /// <summary>
        /// Touch-Sensor (TTP223) — ausschliesslich fuer Display-Navigation.
        /// Weckt das Display bei Beruehrung auf (Backlight an).
        /// </summary>
        public const int Touch = 24;

        // ─── Buttons (PCB Simple + Extended) ────────────────────────────────────
        /// <summary>
        /// BTN1 — GPIO 5, Physischer Pin 29.
        /// Kurz: Naechster Modus (zyklisch).
        /// Lang 3s: Master-Key Modus aktivieren.
        /// </summary>
        public const int Button1 = 5;

        /// <summary>
        /// BTN2 — GPIO 6, Physischer Pin 31.
        /// Kurz: PIR-Sensor an/aus toggle.
        /// Lang 3s: Standby — alles aus.
        /// </summary>
        public const int Button2 = 6;

        // ─── RFID (nur PCB Extended) ─────────────────────────────────────────────
        /// <summary>RC522 SPI Chip-Select</summary>
        public const int RfidSpiCe = 8;   // SPI0 CE0

        /// <summary>RC522 Reset-Pin</summary>
        public const int RfidReset = 25;

        // ─── PIR Bewegungsmelder (nur PCB Extended) ──────────────────────────────
        /// <summary>HC-SR501 Signalpin</summary>
        public const int Pir = 25; // Hinweis: teilt sich GPIO 25 mit RfidReset — ggf. Hardware-seitig trennen

        // ─── Display I2C (nur PCB Extended) ─────────────────────────────────────
        /// <summary>SSD1306 OLED — I2C Adresse 0x3C (SDA=GPIO2, SCL=GPIO3 — Hardware I2C)</summary>
        public const int I2cSda = 2;
        public const int I2cScl = 3;
    }
}
