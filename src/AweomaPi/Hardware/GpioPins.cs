namespace AweomaPi.Hardware;

/// <summary>
/// GPIO-Pin Definitionen fuer beide PCB-Varianten (BCM-Nummerierung).
/// PCB Simple:   alle Pins bis inkl. Touch
/// PCB Extended: zusaetzlich RFID (SPI), Display (I2C), PIR
/// </summary>
public static class GpioPins
{
    // ----------------------------------------------------------------
    // LEDs – beide Varianten
    // ----------------------------------------------------------------
    public const int LedPower = 17;   // Gruen  – System laeuft
    public const int LedVpn   = 27;   // Blau   – VPN aktiv
    public const int LedWan   = 22;   // Gelb   – Netzwerkverbindung
    public const int LedError = 23;   // Rot    – Fehler / Alarm

    // ----------------------------------------------------------------
    // PWM – beide Varianten
    // ----------------------------------------------------------------
    public const int Pwm0 = 12;   // Hardware-PWM Kanal 0 (Luefter, Dimmer)
    public const int Pwm1 = 13;   // Hardware-PWM Kanal 1 (frei verwendbar)

    // ----------------------------------------------------------------
    // Touch-Sensor – steuert das Display (beide Varianten)
    // TTP223 kapazitiver Touch – HIGH = beruehrt
    // ----------------------------------------------------------------
    public const int TouchDisplay = 24;

    // ----------------------------------------------------------------
    // Reset-Taster – beide Varianten
    // GPIO 3 = shared mit I2C SCL (auf PCB Extended entkoppelt)
    // ----------------------------------------------------------------
    public const int ResetButton = 3;

    // ----------------------------------------------------------------
    // PIR Bewegungsmelder – nur PCB Extended
    // HC-SR501 – HIGH = Bewegung erkannt
    // ----------------------------------------------------------------
    public const int PirSensor = 25;

    // ----------------------------------------------------------------
    // SPI – RFID RC522 – nur PCB Extended
    // MISO/MOSI/SCLK werden vom SPI-Treiber verwaltet
    // ----------------------------------------------------------------
    public const int RfidChipSelect = 8;   // CE0 / SPI CS
    public const int RfidReset      = 25;  // Optional: Reset-Pin des RC522

    // ----------------------------------------------------------------
    // I2C – Display SSD1306 – nur PCB Extended
    // I2C-Bus 1: SDA = GPIO 2, SCL = GPIO 3 (vom OS verwaltet)
    // ----------------------------------------------------------------
    public const int I2CBus        = 1;
    public const int DisplayI2CAddr = 0x3C;  // Standard SSD1306 Adresse

    // ----------------------------------------------------------------
    // 5V-Ausgang (nur Referenz, kein steuerbarer GPIO)
    // ----------------------------------------------------------------
    // Pin 2 / 4 auf dem 40-Pin Header
}
