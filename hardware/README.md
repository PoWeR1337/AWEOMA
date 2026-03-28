# AWEOMA – Hardware

Dieses Verzeichnis enthaelt alle Hardware-Dateien fuer die AWEOMA Custom PCBs.
Es gibt zwei Varianten:

---

## PCB Varianten

### PCB Simple – [Dokumentation](pcb-simple/README.md)

Die einfache Variante fuer den Grundbetrieb des Gateways.

| Feature | Detail |
|---|---|
| Formfaktor | HAT-kompatibel (65 x 56 mm) |
| Stromversorgung | 5 V / 3 A via USB-C oder Barrel-Jack |
| **4-Pin LED** | Power, VPN, WAN, Error |
| **2x PWM-Ausgang** | GPIO 12 und GPIO 13 (Luefter, Dimmer) |
| **Touch-Sensor** | Kapazitiver Touch-Button (TTP223) |
| **5V-Ausgang** | Geregelter Ausgang fuer externe Komponenten |
| Reset-Taster | Hardware-Reset via GPIO 3 |
| Verpolungsschutz | P-MOSFET Eingangsschutz |

---

### PCB Extended – [Dokumentation](pcb-extended/README.md)

Alle Features des PCB Simple, plus:

| Zusatz-Feature | Detail |
|---|---|
| **RFID-Reader** | RC522 Modul (SPI) – Zugangskontrolle / Key-Tags |
| **Mini-LCD** | SSD1306 0.96" OLED (I2C) – Statusanzeige |
| **Bewegungsmelder** | HC-SR501 PIR-Sensor – GPIO 25 |

---

## Verzeichnisstruktur

```
hardware/
|-- pcb-simple/
|   |-- README.md          # Dokumentation PCB Simple
|   |-- schematic/         # KiCad Schaltplan (.kicad_sch)
|   |-- gerber/            # Gerber-Dateien fuer Hersteller
|   |-- bom/               # Stueckliste (BOM)
|   +-- images/            # Fotos & 3D-Renders
|
+-- pcb-extended/
    |-- README.md          # Dokumentation PCB Extended
    |-- schematic/         # KiCad Schaltplan
    |-- gerber/            # Gerber-Dateien
    |-- bom/               # Stueckliste
    +-- images/            # Fotos & 3D-Renders
```

---

## Schnellvergleich

| Feature | PCB Simple | PCB Extended |
|---|:---:|:---:|
| Stromversorgung 5V/3A | ✅ | ✅ |
| 4-Pin Status-LED | ✅ | ✅ |
| 2x PWM-Ausgang | ✅ | ✅ |
| Touch-Sensor | ✅ | ✅ |
| 5V-Ausgang | ✅ | ✅ |
| Reset-Taster | ✅ | ✅ |
| RFID-Reader (RC522) | ❌ | ✅ |
| Mini-LCD / OLED | ❌ | ✅ |
| Bewegungsmelder (PIR) | ❌ | ✅ |

---

> KiCad-Schaltplaene, Gerber-Dateien, BOMs und Fotos folgen nach der ersten Fertigung.# AWEOMA – Hardware / Custom PCB

Dieses Verzeichnis enthaelt alle Hardware-Dateien fuer das AWEOMA Custom PCB.

---

## Verzeichnisstruktur

```
hardware/
|-- schematic/      # KiCad Schaltplan (.kicad_sch, .sch)
|-- gerber/         # Gerber-Dateien fuer PCB-Hersteller
|-- bom/            # Bill of Materials (Stueckliste)
+-- images/         # Fotos & 3D-Renders des PCBs
```

---

## PCB-Features

| Feature             | Detail                                      |
|---------------------|---------------------------------------------|
| Formfaktor          | HAT-kompatibel (65 x 56 mm)                 |
| Anschluss           | 40-Pin GPIO Header (Pi 4 kompatibel)        |
| Stromversorgung     | 5 V / 3 A via USB-C oder Barrel-Jack (5.5/2.1 mm) |
| Spannungsregler     | LM2596 Schaltregler                         |
| Status-LEDs         | 4x LED: Power, VPN, WAN, Error              |
| LED-Treiber         | Direkt via GPIO (330 Ohm Vorwiderstaende)   |
| Reset-Taster        | Momentary Push-Button, verbunden mit GPIO 3 |
| GPIO-Breakout       | 2x 20-Pin Header fuer freie GPIO-Pins       |
| RJ45 Halterung      | Zugentlastung fuer LAN-Kabel                |
| Schutzschaltung     | Verpolungsschutz fuer Eingangsspannung      |
| Gehaeuse            | Kompatibel mit Standard-RPi 4 Hut-Gehaeuse |

---

## Stückliste (BOM) – Wichtigste Bauteile

| Referenz | Bauteil            | Wert / Typ          | Menge |
|----------|--------------------|---------------------|-------|
| U1       | Spannungsregler    | LM2596T-5.0         | 1     |
| D1       | Schottky-Diode     | 1N5822              | 1     |
| L1       | Induktivitaet      | 68 uH               | 1     |
| C1, C2   | Elko               | 100 uF / 35 V       | 2     |
| C3, C4   | Keramik-Kondi      | 100 nF              | 2     |
| LED1     | Power-LED          | Gruen 3mm           | 1     |
| LED2     | VPN-LED            | Blau 3mm            | 1     |
| LED3     | WAN-LED            | Gelb 3mm            | 1     |
| LED4     | Error-LED          | Rot 3mm             | 1     |
| R1-R4    | Vorwiderstand      | 330 Ohm 1/4 W       | 4     |
| SW1      | Reset-Taster       | 6x6 mm Momentary    | 1     |
| J1       | USB-C Buchse       | USB-C Power Input   | 1     |
| J2       | Barrel Jack        | 5.5/2.1 mm          | 1     |
| J3       | 40-Pin Header      | Female, 2.54 mm     | 1     |
| J4, J5   | Breakout Header    | Male, 2.54 mm       | 2     |

Die vollstaendige BOM befindet sich in `bom/AWEOMA-BOM.csv`.

---

## Schaltplan

Der Schaltplan wurde mit **KiCad 7** erstellt.

- Schaltplan-Datei: `schematic/AWEOMA.kicad_sch`
- PDF-Export: `schematic/AWEOMA-schematic.pdf`

---

## Gerber-Dateien (PCB-Fertigung)

Die Gerber-Dateien fuer die Bestellung beim PCB-Hersteller befinden sich in `gerber/`.

Empfohlene Parameter fuer die Fertigung:

| Parameter       | Wert               |
|-----------------|--------------------|
| Lagen           | 2 (FR4)            |
| Dicke           | 1.6 mm             |
| Kupferdicke     | 1 oz               |
| Loetstopplack   | Gruen (oder nach Wahl) |
| Silkscreen      | Weiss              |
| Mindest-Leiterbahn | 0.2 mm          |
| Mindest-Bohrung | 0.3 mm             |

---

## GPIO-Belegung

| GPIO | BCM | Funktion      | Richtung |
|------|-----|---------------|----------|
| 11   | 17  | LED Power     | Output   |
| 13   | 27  | LED VPN       | Output   |
| 15   | 22  | LED WAN       | Output   |
| 16   | 23  | LED Error     | Output   |
| 5    | 3   | Reset-Taster  | Input    |

---

## PCB bestellen

1. Gerber-ZIP herunterladen aus `gerber/AWEOMA-gerber.zip`
2. Hochladen bei einem PCB-Hersteller (z.B. JLCPCB, PCBWay, Aisler)
3. Parameter wie oben angegeben einstellen
4. Bauteile gemaess BOM besorgen und betuecken

---

## Hinweise zur Betueckung

1. Zuerst SMD-Bauteile (falls vorhanden), dann THT-Bauteile loeten
2. Spannungsregler mit ausreichend Luft nach oben einbauen (Waermeabfuhr)
3. LEDs: Anode (laengeres Bein) immer Richtung +
4. Elkos: Polung beachten (Minus-Markierung auf dem Gehaeuse)
5. Nach der Betueckung: Kurzschluss-Test mit Multimeter vor dem ersten Einschalten
6. Ersten Start ohne aufgesteckten Raspberry Pi durchfuehren und Ausgangsspannung messen (soll 5 V sein)

---

> Hardware-Dateien werden in Kuerze hinzugefuegt.
> Fotos und 3D-Renders folgen nach der ersten Fertigung.
