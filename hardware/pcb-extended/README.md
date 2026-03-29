# AWEOMA PCB Extended

Die erweiterte PCB-Variante mit RFID, Mini-LCD und Bewegungsmelder – fuer vollstaendigen Standalone-Betrieb.

Enthaelt alle Features des **PCB Simple**, plus die unten aufgefuehrten Erweiterungen.

---

## Alle Features im Ueberblick

### Von PCB Simple uebernommen

| Feature | Detail |
|---|---|
| Stromversorgung | 5 V / 3 A via USB-C oder Barrel-Jack |
| Spannungsregler | LM2596 Schaltregler |
| 4-Pin LED | Power, VPN, WAN, Error |
| 2x PWM-Ausgang | GPIO 12 und GPIO 13 |
| Touch-Sensor | TTP223 kapazitiver Touch – GPIO 24 |
| 5V-Ausgang | Geregelter Ausgang fuer externe Komponenten |
| Reset-Taster | Momentary Push-Button – GPIO 3 |
| **BTN1** | **Taster 1 – GPIO 5, Pin 29 (Modus-Wechsel / Master-Key)** |
| **BTN2** | **Taster 2 – GPIO 6, Pin 31 (PIR Toggle / Standby)** |
| Verpolungsschutz | P-MOSFET Eingangsschutz |
| Formfaktor | Gehaeuse 120 x 120 x 90 mm \| PCB HAT-kompatibel, 40-Pin GPIO |

### Zusaetzlich (Extended)

| Zusatz-Feature | Detail |
|---|---|
| **RFID-Reader** | RC522 Modul via SPI (MISO/MOSI/SCK/CE0) fuer Zugangskontrolle |
| **Mini-LCD** | 0.96" SSD1306 OLED oder 1.3" I2C-LCD via I2C (SDA/SCL) |
| **Bewegungsmelder** | HC-SR501 PIR-Sensor – GPIO 25 |

---

## GPIO-Belegung (Komplett)

| GPIO (BCM) | Pin | Funktion | Richtung | Beschreibung |
|---|---|---|---|---|
| GPIO 2 (SDA) | 3 | I2C SDA | I/O | Display (SSD1306 / LCD) |
| GPIO 3 (SCL) | 5 | I2C SCL + Reset | I/O | Display + Reset-Taster (shared) |
| **GPIO 5** | **29** | **BTN1** | **Input (Pull-up)** | **Taster 1 – Kurz: Naechster Modus (zyklisch) \| Lang 3s: Master-Key** |
| **GPIO 6** | **31** | **BTN2** | **Input (Pull-up)** | **Taster 2 – Kurz: PIR Toggle \| Lang 3s: Standby** |
| GPIO 8 (CE0) | 24 | SPI CS | Output | RFID RC522 Chip Select |
| GPIO 9 (MISO) | 21 | SPI MISO | Input | RFID Daten empfangen |
| GPIO 10 (MOSI) | 19 | SPI MOSI | Output | RFID Daten senden |
| GPIO 11 (SCLK) | 23 | SPI SCLK | Output | RFID Takt |
| GPIO 12 | 32 | PWM0 | Output | Hardware-PWM Kanal 0 |
| GPIO 13 | 33 | PWM1 | Output | Hardware-PWM Kanal 1 |
| GPIO 17 | 11 | LED Power | Output | Gruen – System laeuft |
| GPIO 22 | 15 | LED WAN | Output | Gelb – Netzwerkverbindung |
| GPIO 23 | 16 | LED Error | Output | Rot – Fehler / Alarm |
| GPIO 24 | 18 | Touch-Sensor | Input | Kapazitiver Touch (TTP223) |
| GPIO 25 | 22 | PIR-Sensor | Input | Bewegungsmelder HC-SR501 |
| GPIO 27 | 13 | LED VPN | Output | Blau – VPN aktiv |

> Hinweis: GPIO 3 wird fuer Reset UND I2C SCL verwendet. Auf dem Extended-PCB ist der Reset-Taster ueber einen extra Pulldown-Widerstand entkoppelt, um Konflikte zu vermeiden. Bei gleichzeitiger I2C-Nutzung ist ein seperater Reset-Pin (GPIO 4) als Alternative moeglich.
