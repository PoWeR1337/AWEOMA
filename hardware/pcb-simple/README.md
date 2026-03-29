# AWEOMA PCB Simple

Die einfache PCB-Variante fuer den Grundbetrieb des AWEOMA Gateways.

## Features

| Feature | Detail |
|---|---|
| Formfaktor | Gehaeuse 120 x 120 x 90 mm | PCB HAT-kompatibel, 40-Pin GPIO |
| Stromversorgung | 5 V / 3 A via USB-C oder Barrel-Jack (5.5/2.1 mm) |
| Spannungsregler | LM2596 Schaltregler (Step-Down) |
| 4-Pin LED | 4 separate Status-LEDs (Power, VPN, WAN, Error) |
| 2x PWM-Ausgang | Hardware-PWM via GPIO 12 und GPIO 13 (fuer Luefter, Dimmer, etc.) |
| Touch-Sensor | Kapazitiver Touch-Button (TTP223) – GPIO 24 (Display-Navigation) |
| **BTN1** | **Taster 1 – GPIO 5, Pin 29 (Modus-Wechsel / Master-Key)** |
| **BTN2** | **Taster 2 – GPIO 6, Pin 31 (PIR Toggle / Standby)** |
| 5V-Ausgang | Geregelter 5V-Ausgang-Pin fuer externe Komponenten (max. 500 mA) |
| Reset-Taster | Momentary Push-Button, verbunden mit GPIO 3 (Pi-Reset) |
| Verpolungsschutz | P-MOSFET Schutzschaltung am Eingang |
| Gehaeuse | Kompatibel mit Standard-RPi 4 HAT-Gehaeuse |

## GPIO-Belegung

| GPIO (BCM) | Pin | Funktion | Richtung | Beschreibung |
|---|---|---|---|---|
| GPIO 3 | 5 | Reset-Taster | Input (Pull-up) | Kurzes Druecken: Soft-Reset, Langes Druecken: Shutdown |
| **GPIO 5** | **29** | **BTN1** | **Input (Pull-up)** | **Kurz: Naechster Modus (zyklisch) | Lang 3s: Master-Key Modus** |
| **GPIO 6** | **31** | **BTN2** | **Input (Pull-up)** | **Kurz: PIR Toggle | Lang 3s: Standby** |
| GPIO 12 | 32 | PWM0 | Output | Hardware-PWM Kanal 0 (Luefter / Dimmer) |
| GPIO 13 | 33 | PWM1 | Output | Hardware-PWM Kanal 1 (freie Verwendung) |
| GPIO 17 | 11 | LED Power | Output | Gruen – System laeuft |
| GPIO 22 | 15 | LED WAN | Output | Gelb – Netzwerkverbindung |
| GPIO 23 | 16 | LED Error | Output | Rot – Fehler / Alarm |
| GPIO 24 | 18 | Touch-Sensor | Input | Kapazitiver Touch (TTP223) – Display-Navigation / Wakeup |
| GPIO 27 | 13 | LED VPN | Output | Blau – VPN aktiv |
| 5V (Pin 2) | 2 | 5V-Ausgang | Power | Geregelter 5V-Ausgang (ext. Komponenten) |
| GND | 6/9/... | Masse | GND | |

### Button-Funktionen Detail

**BTN1 (GPIO 5, Pin 29):**

| Aktion | Funktion |
|---|---|
| Kurz druecken | Naechster Modus: Normal → Minimal → Night → Standby → Normal |
| Lang halten 3s | Master-Key Modus (schaltet einmalig durch alle Modi) |

**BTN2 (GPIO 6, Pin 31):**

| Aktion | Funktion |
|---|---|
| Kurz druecken | PIR-Sensor an/aus toggle |
| Lang halten 3s | Standby — alles aus, Pi laeuft im Hintergrund |
