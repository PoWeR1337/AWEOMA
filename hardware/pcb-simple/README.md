# AWEOMA PCB Simple

Die einfache PCB-Variante fuer den Grundbetrieb des AWEOMA Gateways.

---

## Features

| Feature | Detail |
|---|---|
| **Formfaktor** | HAT-kompatibel (65 x 56 mm), 40-Pin GPIO |
| **Stromversorgung** | 5 V / 3 A via USB-C oder Barrel-Jack (5.5/2.1 mm) |
| **Spannungsregler** | LM2596 Schaltregler (Step-Down) |
| **4-Pin LED** | 4 separate Status-LEDs (Power, VPN, WAN, Error) |
| **2x PWM-Ausgang** | Hardware-PWM via GPIO 12 und GPIO 13 (fuer Luefter, Dimmer, etc.) |
| **Touch-Sensor** | Kapazitiver Touch-Button (TTP223) – GPIO 24 |
| **5V-Ausgang** | Geregelter 5V-Ausgang-Pin fuer externe Komponenten (max. 500 mA) |
| **Reset-Taster** | Momentary Push-Button, verbunden mit GPIO 3 (Pi-Reset) |
| **Verpolungsschutz** | P-MOSFET Schutzschaltung am Eingang |
| **Gehaeuse** | Kompatibel mit Standard-RPi 4 HAT-Gehaeuse |

---

## GPIO-Belegung

| GPIO (BCM) | Pin | Funktion | Richtung | Beschreibung |
|---|---|---|---|---|
| GPIO 3 | 5 | Reset-Taster | Input (Pull-up) | Kurzes Druecken: Soft-Reset, Langes Druecken: Shutdown |
| GPIO 12 | 32 | PWM0 | Output | Hardware-PWM Kanal 0 (Luefter / Dimmer) |
| GPIO 13 | 33 | PWM1 | Output | Hardware-PWM Kanal 1 (freie Verwendung) |
| GPIO 17 | 11 | LED Power | Output | Gruen – System laeuft |
| GPIO 22 | 15 | LED WAN | Output | Gelb – Netzwerkverbindung |
| GPIO 23 | 16 | LED Error | Output | Rot – Fehler / Alarm |
| GPIO 24 | 18 | Touch-Sensor | Input | Kapazitiver Touch (TTP223) |
| GPIO 27 | 13 | LED VPN | Output | Blau – VPN aktiv |
| 5V (Pin 2) | 2 | 5V-Ausgang | Power | Geregelter 5V-Ausgang (ext. Komponenten) |
| GND | 6/9/... | Masse | GND | |

---

## Stückliste (BOM)

| Ref. | Bauteil | Wert / Typ | Menge |
|---|---|---|---|
| U1 | Spannungsregler | LM2596T-5.0 (TO-263) | 1 |
| Q1 | P-MOSFET (Verpolungsschutz) | AO3401A | 1 |
| D1 | Schottky-Diode | 1N5822 | 1 |
| L1 | Induktivitaet | 68 uH (Ringkern) | 1 |
| C1, C2 | Elektrolytkondensator | 100 uF / 35 V | 2 |
| C3, C4 | Keramikkondensator | 100 nF (0805) | 2 |
| U2 | Touch-Sensor | TTP223 Modul | 1 |
| LED1 | Status-LED Power | Gruen 3mm, 20 mA | 1 |
| LED2 | Status-LED VPN | Blau 3mm, 20 mA | 1 |
| LED3 | Status-LED WAN | Gelb 3mm, 20 mA | 1 |
| LED4 | Status-LED Error | Rot 3mm, 20 mA | 1 |
| R1–R4 | Vorwiderstand LEDs | 330 Ohm, 1/4 W | 4 |
| R5 | Pull-up Reset | 10 kOhm | 1 |
| SW1 | Reset-Taster | 6x6 mm Momentary, THT | 1 |
| J1 | USB-C Buchse | 5V Power Input | 1 |
| J2 | Barrel Jack | 5.5 / 2.1 mm | 1 |
| J3 | 40-Pin GPIO Header | Female, 2.54 mm, Pitch 2x20 | 1 |
| J4 | 5V-Ausgang Header | 2-Pin Male, 2.54 mm | 1 |

Vollstaendige BOM: `bom/pcb-simple-bom.csv`

---

## PWM verwenden (Software-Beispiel)

```python
import RPi.GPIO as GPIO
import time

GPIO.setmode(GPIO.BCM)
GPIO.setup(12, GPIO.OUT)

# PWM auf GPIO 12, Frequenz 1000 Hz
pwm = GPIO.PWM(12, 1000)
pwm.start(0)  # 0% Duty Cycle

# Luefter auf 50%
pwm.ChangeDutyCycle(50)
time.sleep(5)

pwm.stop()
GPIO.cleanup()
```

## Touch-Sensor auslesen

```python
import RPi.GPIO as GPIO

TOUCH_PIN = 24
GPIO.setmode(GPIO.BCM)
GPIO.setup(TOUCH_PIN, GPIO.IN)

def touch_callback(channel):
    print("Touch erkannt!")

GPIO.add_event_detect(TOUCH_PIN, GPIO.RISING, callback=touch_callback, bouncetime=200)

try:
    input("Warten auf Touch... (Enter zum Beenden)")
finally:
    GPIO.cleanup()
```

## LEDs ansteuern

```python
import RPi.GPIO as GPIO

LED_POWER = 17
LED_VPN   = 27
LED_WAN   = 22
LED_ERROR = 23

GPIO.setmode(GPIO.BCM)
for pin in [LED_POWER, LED_VPN, LED_WAN, LED_ERROR]:
    GPIO.setup(pin, GPIO.OUT, initial=GPIO.LOW)

# Power-LED an, alle anderen aus
GPIO.output(LED_POWER, GPIO.HIGH)
```

---

## Fertigungsparameter

| Parameter | Wert |
|---|---|
| Lagen | 2 (FR4) |
| Dicke | 1.6 mm |
| Kupferdicke | 1 oz |
| Loetstopplack | Gruen |
| Silkscreen | Weiss |
| Mindest-Leiterbahn | 0.2 mm |
| Mindest-Bohrung | 0.3 mm |

Gerber-Dateien: `gerber/pcb-simple-gerber.zip`

---

> Fotos und 3D-Renders folgen nach der ersten Fertigung.
