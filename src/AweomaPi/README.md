# AweomaPi — C# Hardware Controller

Der C#-Controller steuert alle PCB-Komponenten des AWEOMA Pi Gateways.
Er erkennt beim Start automatisch die verbaute Hardware und aktiviert nur die vorhandenen Services.

## Projektstruktur

```
src/AweomaPi/
├── Hardware/
│   ├── GpioPins.cs          # GPIO-Pin-Definitionen (LEDs, PWM, Touch, BTN1, BTN2, RFID, PIR)
│   └── HardwareDetector.cs  # Automatische Hardware-Erkennung beim Start
├── Services/
│   ├── LedService.cs        # 4-Pin LED-Steuerung (Power/VPN/WAN/Error) + Helligkeit + Blink
│   ├── DisplayService.cs    # SSD1306 OLED + Touch-Sensor (Display-Navigation)
│   ├── RfidService.cs       # RC522 RFID-Karten-Erkennung (nur PCB Extended)
│   ├── PirService.cs        # HC-SR501 Bewegungsmelder (nur PCB Extended)
│   ├── ModeService.cs       # Betriebsmodi-Verwaltung (Normal/Minimal/Night/Standby/MasterKey/PIR)
│   └── ButtonService.cs     # BTN1 (GPIO 5) + BTN2 (GPIO 6) mit Kurzklick/Langklick/Kombo
├── Program.cs               # Einstiegspunkt: Hardware-Detection + Service-Init + Hauptschleife
└── AweomaPi.csproj
```

## GPIO-Pins Uebersicht

| Funktion | GPIO | Physischer Pin | PCB |
|---|---|---|---|
| LED Power (gruen) | 17 | Pin 11 | Simple + Extended |
| LED VPN (blau) | 22 | Pin 15 | Simple + Extended |
| LED WAN (gelb) | 23 | Pin 16 | Simple + Extended |
| LED Error (rot) | 27 | Pin 13 | Simple + Extended |
| PWM Kanal 1 | 12 | Pin 32 | Simple + Extended |
| PWM Kanal 2 | 13 | Pin 33 | Simple + Extended |
| Touch (TTP223) | 24 | Pin 18 | Simple + Extended |
| **BTN1** | **5** | **Pin 29** | **Simple + Extended** |
| **BTN2** | **6** | **Pin 31** | **Simple + Extended** |
| RFID CE (RC522) | 8 | Pin 24 | Extended |
| RFID Reset | 25 | Pin 22 | Extended |
| PIR (HC-SR501) | 25 | Pin 22 | Extended |
| OLED SDA | 2 | Pin 3 | Extended |
| OLED SCL | 3 | Pin 5 | Extended |

## Buttons

### BTN1 — GPIO 5, Physischer Pin 29

| Aktion | Funktion |
|---|---|
| Kurz druecken | Naechster Modus (zyklisch: Normal→Minimal→Night→Standby→Normal) |
| Lang halten 3s | Master-Key Modus aktivieren (schaltet einmalig durch alle Modi) |

### BTN2 — GPIO 6, Physischer Pin 31

| Aktion | Funktion |
|---|---|
| Kurz druecken | PIR Naehrungssensor an/aus toggle |
| Lang halten 3s | Standby — alles aus (Pi laeuft im Hintergrund weiter) |

### Beide Buttons gleichzeitig

| Aktion | Funktion | LED-Feedback |
|---|---|---|
| 2x kurz zusammen | Reboot | 3x gelb (WAN-LED) blinken |
| 3x kurz zusammen | Shutdown | 3x rot (Error-LED) blinken |

## Die 6 Betriebsmodi

| Modus | Anzeige | Trigger |
|---|---|---|
| Normal | Alles an — Display + LCD + LEDs + Luefter auto | Karte 1 / BTN1 kurz |
| Minimal | Nur LCD + LEDs, grosses Display aus | Karte 2 / BTN1 kurz |
| Night | Nur LEDs gedimmt 10%, LCD + Display aus | Karte 3 / BTN1 kurz |
| Standby | Alles aus, Pi laeuft nur noch im Hintergrund | Karte 4 / BTN2 lang 3s |
| Master Key | Schaltet einmalig durch alle Modi der Reihe nach | Master-Karte / BTN1 lang 3s |
| PIR Auto | Display/LEDs folgen Bewegungssensor automatisch | Automatisch aktiv wenn PIR an |

### PIR-Logik (Modus PIR Auto)

| Zustand | Aktion |
|---|---|
| Bewegung erkannt | Display + LEDs einschalten |
| Keine Bewegung 5 min | Display + LEDs in Standby |
| PIR deaktiviert (BTN2) | Display + LEDs immer an |

## Hardware-Erkennung beim Start

Beim Start prueft `HardwareDetector` automatisch welche Hardware vorhanden ist:

| Hardware | Erkennungs-Methode | GPIO/Bus |
|---|---|---|
| OLED SSD1306 | I2C-Probe auf Adresse 0x3C | I2C Bus 1 |
| RFID RC522 | SPI-Probe: Version-Register 0x37 lesen | SPI0 CE0 |
| PIR HC-SR501 | GPIO 25 oeffnen ohne Exception | GPIO 25 |
| Touch TTP223 | GPIO 24 oeffnen ohne Exception | GPIO 24 |
| BTN1 | GPIO 5 oeffnen ohne Exception | GPIO 5 |
| BTN2 | GPIO 6 oeffnen ohne Exception | GPIO 6 |

**PCB-Variante wird abgeleitet:**
- OLED, RFID oder PIR gefunden → **Extended**
- Sonst → **Simple**

## Beispiel-Startausgabe

```
=== AWEOMA Pi Gateway startet ===
Version: 1.0.0 | Datum: 2026-03-29 08:00
--- Hardware-Erkennung ---
PCB-Variante : Extended
Display (OLED): GEFUNDEN (0x3C)
RFID (RC522)  : GEFUNDEN
PIR (HC-SR501): GEFUNDEN
Touch (TTP223): GEFUNDEN
BTN1 (GPIO 5) : GEFUNDEN
BTN2 (GPIO 6) : GEFUNDEN
--------------------------
[LED] 4 Status-LEDs initialisiert (Power/VPN/WAN/Error).
[DISPLAY] SSD1306 OLED initialisiert. Touch-Wakeup aktiv (GPIO 24).
[PIR] HC-SR501 initialisiert auf GPIO 25. Timeout: 5min.
[RFID] RC522 initialisiert auf SPI0.
ButtonService: BTN1 (GPIO 5) und BTN2 (GPIO 6) initialisiert.
=== AWEOMA Gateway bereit ===
BTN1 kurz = naechster Modus | BTN1 3s = Master-Key
BTN2 kurz = PIR toggle      | BTN2 3s = Standby
Beide 2x  = Reboot          | Beide 3x = Shutdown
```

## Build & Deployment

```bash
# Bauen
cd src/AweomaPi
dotnet build

# Direkt ausfuehren (als root fuer GPIO-Zugriff)
sudo dotnet run

# Release-Build fuer Raspberry Pi
dotnet publish -c Release -r linux-arm64 --self-contained true

# Binaer liegt unter:
# bin/Release/net8.0/linux-arm64/publish/AweomaPi
```

## Systemd-Service (Autostart)

```ini
# /etc/systemd/system/aweoma-pi.service
[Unit]
Description=AWEOMA Pi Hardware Controller
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/opt/aweoma
ExecStart=/opt/aweoma/AweomaPi
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable aweoma-pi
sudo systemctl start aweoma-pi
sudo journalctl -fu aweoma-pi
```

## Abhaengigkeiten

- .NET 8.0 (Raspberry Pi OS 64-bit)
- `System.Device.Gpio` — GPIO/I2C/SPI-Zugriff
- `Iot.Device.Bindings` — SSD1306, RC522 Treiber
- `Microsoft.Extensions.Logging` — Logging
