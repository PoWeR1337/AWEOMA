# AweomaPi – C# Hardware Controller

.NET 8 Programm fuer den Raspberry Pi. Steuert alle Hardware-Komponenten des AWEOMA PCBs und prueft beim Start automatisch welche Sensoren vorhanden sind.

---

## Projektstruktur

```
src/AweomaPi/
|-- Program.cs                    # Einstiegspunkt – Hardware-Detection + Haupt-Loop
|-- AweomaPi.csproj               # .NET 8 Projekt-Datei
|-- Hardware/
|   |-- GpioPins.cs               # Zentrale GPIO-Pin-Definitionen (BCM)
|   +-- HardwareDetector.cs       # Erkennt aktive Sensoren (I2C, SPI, GPIO)
+-- Services/
    |-- DisplayService.cs         # SSD1306 OLED – Touch navigiert Seiten
    |-- LedService.cs             # 4 Status-LEDs (Power, VPN, WAN, Error)
    |-- RfidService.cs            # RC522 RFID-Reader (SPI) – nur Extended
    +-- PirService.cs             # HC-SR501 PIR-Bewegungsmelder – nur Extended
```

---

## Hardware-Erkennung beim Start

Das Programm prueft beim Start automatisch welche Komponenten vorhanden sind:

| Sensor/Modul | Methode | Ergebnis |
|---|---|---|
| OLED SSD1306 | I2C-Bus 1, Adresse 0x3C, ACK-Pruefung | PCB Extended erkannt |
| RFID RC522 | SPI Bus 0, Version-Register 0x37 lesen | PCB Extended erkannt |
| PIR HC-SR501 | GPIO 25 als Input oeffnen | PCB Extended erkannt |
| Touch TTP223 | GPIO 24 als Input oeffnen | Beide Varianten |
| LEDs (4x) | GPIO 17/22/23/27 als Output | Beide Varianten |
| PWM (2x) | GPIO 12 als Output | Beide Varianten |

Daraus wird die **PCB-Variante** abgeleitet:
- Mindestens einer der Extended-Sensoren erkannt → `PcbVariant.Extended`
- Kein Extended-Sensor → `PcbVariant.Simple`

Nicht verfuegbare Sensoren werden **still deaktiviert** – kein Absturz, kein Fehler.

---

## Touch-Sensor: Display-Steuerung

Der TTP223 Touch-Sensor (GPIO 24) steuert das OLED-Display:

- **Kurzer Touch** → naechste Display-Seite (blaettern)
- **Langer Touch** (>2s) → Display an/aus

### Display-Seiten

| Seite | Inhalt |
|---|---|
| 0 | Gateway-Name, IP, VPN-Status, Pi-hole |
| 1 | CPU-Last, CPU-Temperatur |
| 2 (nur Extended) | RFID letzter Tag, PIR-Status |

---

## Voraussetzungen

### System

```bash
# .NET 8 auf dem Pi installieren
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
export PATH=$PATH:$HOME/.dotnet

# I2C und SPI aktivieren (fuer PCB Extended)
sudo raspi-config
# -> Interfaces -> I2C -> Enable
# -> Interfaces -> SPI -> Enable
```

### NuGet-Pakete (werden automatisch geladen)

| Paket | Zweck |
|---|---|
| System.Device.Gpio | GPIO, I2C, SPI Zugriff |
| Iot.Device.Bindings | SSD1306, MFRC522, etc. |
| Microsoft.Extensions.Hosting | Logging, DI |

---

## Build & Run

```bash
# Im Repository-Root
cd src/AweomaPi

# Build
dotnet build

# Direkt ausfuehren (root fuer GPIO-Zugriff)
sudo dotnet run

# Als Release bauen und deployen
dotnet publish -c Release -r linux-arm64 --self-contained
sudo ./bin/Release/net8.0/linux-arm64/publish/AweomaPi
```

---

## Systemd-Service (Autostart)

```ini
# /etc/systemd/system/aweoma-pi.service
[Unit]
Description=AWEOMA Pi Hardware Controller
After=network.target wg-quick@wg0.service

[Service]
Type=simple
User=root
WorkingDirectory=/opt/aweoma/src/AweomaPi
ExecStart=/opt/aweoma/src/AweomaPi/bin/Release/net8.0/linux-arm64/publish/AweomaPi
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable aweoma-pi
sudo systemctl start aweoma-pi
sudo journalctl -u aweoma-pi -f   # Logs live ansehen
```

---

## Beispiel-Ausgabe beim Start

```
  ╔══════════════════════════════════════╗
  ║   AWEOMA – Raspberry Pi Gateway      ║
  ║   Hardware Controller v1.0           ║
  ╚══════════════════════════════════════╝

[INFO] Schritt 1/3 – Hardware-Erkennung...
[INFO] [I2C] OLED-Display (SSD1306) erkannt auf Bus 1, Adresse 0x3C
[INFO] [SPI] RFID RC522 erkannt (Version: 0x92)
[INFO] [GPIO] PIR HC-SR501 erkannt auf Pin BCM 25
[INFO] [GPIO] Touch TTP223 (Display) erkannt auf Pin BCM 24
[INFO] [GPIO] LEDs: 4/4 Pins erreichbar
[INFO] [GPIO] PWM-Pins erkannt (GPIO 12, 13)
[INFO] === Hardware-Profil: PCB: Extended | LEDs=True | PWM=True | Touch(Display)=True | OLED=True | RFID=True | PIR=True ===

  PCB-Variante : Extended
  LEDs (4x)    : ✓ erkannt
  PWM (2x)     : ✓ erkannt
  Touch/Display: ✓ erkannt
  OLED-Display : ✓ erkannt
  RFID RC522   : ✓ erkannt
  PIR HC-SR501 : ✓ erkannt

[INFO] Schritt 2/3 – Services starten...
[INFO] [LED] 4 Status-LEDs initialisiert.
[INFO] [Display] SSD1306 initialisiert (128x64, I2C).
[INFO] [Display] Touch-Sensor aktiv (GPIO 24) – steuert Display-Seiten.
[INFO] [RFID] RC522 bereit. Warte auf Tags...
[INFO] [PIR] HC-SR501 aktiv auf GPIO 25. Hinweis: 30s Aufwaermzeit benoetigt.
[INFO] Schritt 3/3 – Haupt-Loop gestartet. [Ctrl+C zum Beenden]
```
