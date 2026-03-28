# AWEOMA — Raspberry Pi Gateway

> **A**ll-in-one **W**ireguard, **E**dge, **O**pencloud & **M**onitoring **A**ppliance
> Ein selbst gehostetes Netzwerk-Gateway auf Basis eines Raspberry Pi mit eigenem PCB, VPS-Anbindung und einem vollstaendigen Service-Stack.

---

## Inhalt

- [Projektuebersicht](#projektuebersicht)
- [Hardware](#hardware)
  - [Raspberry Pi](#raspberry-pi)
  - [PCB Simple](#pcb-simple)
  - [PCB Extended](#pcb-extended)
- [Netzwerkarchitektur](#netzwerkarchitektur)
- [Software-Stack](#software-stack)
- [VPS-Anbindung](#vps-anbindung)
- [Verzeichnisstruktur](#verzeichnisstruktur)
- [Setup & Installation](#setup--installation)

---

## Projektuebersicht

AWEOMA ist ein kompaktes, selbst gehostetes Netzwerk-Gateway auf einem **Raspberry Pi**, erweitert durch ein **massgeschneidertes PCB** (in zwei Varianten). Das System fungiert als DNS-Resolver, VPN-Router, Reverse Proxy, Cloud-Storage-Server, Passwort-Manager und Intrusion-Prevention-System gleichzeitig.

---

## Hardware

### Raspberry Pi

| Eigenschaft | Wert |
|---|---|
| Modell | Raspberry Pi 4 Model B (4 GB / 8 GB RAM) |
| OS | Raspberry Pi OS Lite 64-bit |
| Speicher | microSD >= 32 GB oder USB-SSD |
| Stromversorgung | 5 V / 3 A USB-C (ueber PCB) |

---

### PCB Simple

Die einfache PCB-Variante fuer den Grundbetrieb des Gateways.

| Feature | Beschreibung |
|---|---|
| **Stromversorgung** | Geregelte 5 V / 3 A fuer den Pi via USB-C oder Barrel-Jack |
| **4-Pin LED** | RGB+W oder 4x Einzel-LEDs: Power, VPN, WAN, Error |
| **2x PWM-Ausgang** | Steuerbare PWM-Ausgaenge (z.B. fuer Luefter, Dimmer) |
| **Touch-Sensor** | Kapazitiver Touch-Button fuer Interaktion ohne Taster |
| **5V-Ausgang** | Geregelter 5V-Pin fuer externe Komponenten |
| **Reset-Taster** | Hardware-Reset ohne SSH-Zugang |
| **Formfaktor** | HAT-kompatibel (65 x 56 mm), passt in Standard-Hut-Gehaeuse |

> Dateien: `hardware/pcb-simple/`

---

### PCB Extended

Die erweiterte Variante mit zusaetzlichen Sensoren und Anzeige – ideal fuer einen vollwertigen Standalone-Betrieb.

Enthaelt alle Features des **PCB Simple**, plus:

| Zusatz-Feature | Beschreibung |
|---|---|
| **RFID-Reader** | RC522 Modul (SPI) fuer Zugangskontrolle / Key-Tags |
| **Mini-LCD** | 0.96" oder 1.3" OLED / I2C-LCD fuer Status-Anzeige |
| **Bewegungsmelder** | PIR-Sensor fuer Praesenz-Erkennung (Aktivierung, Alarm) |

> Dateien: `hardware/pcb-extended/`

---

## Netzwerkarchitektur

```
Internet
   |
   v
 VPS  (oeffentliche IP, Traefik, WireGuard-Server)
   |
   | WireGuard Tunnel (verschluesselt)
   |
   v
 Raspberry Pi Gateway (AWEOMA PCB + RPi 4)
   |-- Pi-hole        --> DNS fuer LAN
   |-- WireGuard      --> ProtonVPN Exit
   |-- Traefik        --> lokale Dienste
   |-- Opencloud      --> Cloud-Storage
   |-- Passbolt       --> Passwort-Manager
   |-- CrowdSec       --> IPS
   |-- Fail2ban       --> Brute-Force-Schutz
   |
   v
 Heimnetzwerk (LAN-Geraete)
```

---

## Software-Stack

### Pi-hole

Netzwerkweiter DNS-Resolver und Ad-Blocker. Blockiert Werbung, Tracker und schaedliche Domains.

- Upstream-DNS: Cloudflare (1.1.1.1)
- Blocklisten: StevenBlack, OISD, uBlock Origin
- Web-Interface auf Port 80 (intern)

### WireGuard + ProtonVPN

Zwei WireGuard-Verbindungen gleichzeitig:

1. **VPS-Tunnel** (wg0): Sicherer Kanal fuer eingehenden Traffic vom VPS
2. **ProtonVPN-Ausgang** (wg1): Gesamter ausgehender Traffic laeuft verschluesselt ueber ProtonVPN

### Traefik

Laeuft auf VPS und lokal. Uebernimmt automatische TLS-Zertifikate (Lets Encrypt), Routing und Middleware (Auth, Rate-Limiting, Secure-Headers).

### Opencloud

Privater Cloud-Speicher mit WebDAV, Desktop-/Mobil-Sync und Dateiversionierung.

### Passbolt

Open-Source Passwort-Manager fuer Teams und Einzelpersonen. Laeuft als Docker-Container hinter Traefik.

- End-to-End verschluesselt (OpenPGP)
- Browser-Extension fuer Chrome/Firefox
- API fuer CLI und Automatisierung
- Web-Interface unter eigenem Sub-Domain

### CrowdSec

Analysiert Logs und blockiert Angreifer automatisch via kollaborative Threat-Intelligence.

### Fail2ban

Ueberwacht SSH und Dienste, bannt IPs nach zu vielen Fehlversuchen.

### ntpdate / Netdate

Zeitsynchronisation beim Systemstart und per Cronjob. Notwendig fuer WireGuard und TLS.

### btop

Echtzeit-Systemmonitor fuer CPU, RAM, Netzwerk und Prozesse im Terminal.

---

## VPS-Anbindung

Der VPS uebernimmt:

- Oeffentlicher Einstiegspunkt (einzige oeffentliche IP)
- Traefik als Edge-Proxy mit TLS-Terminierung
- WireGuard-Server-Endpunkt
- CrowdSec-Agent fuer VPS-seitigen Schutz

Der Pi verbindet sich beim Start automatisch per WireGuard mit dem VPS. Eingehende Anfragen (Opencloud, Passbolt, etc.) werden durch den Tunnel an den Pi weitergeleitet.

---

## Verzeichnisstruktur

```
AWEOMA/
|-- hardware/
|   |-- pcb-simple/        # PCB Simple: PWM, Touch, 5V, 4-Pin-LED
|   |   |-- schematic/     # KiCad Schaltplan
|   |   |-- gerber/        # Fertigungsdateien
|   |   |-- bom/           # Stueckliste
|   |   +-- images/        # Fotos & Renders
|   +-- pcb-extended/      # PCB Extended: + RFID, LCD, Bewegungsmelder
|       |-- schematic/
|       |-- gerber/
|       |-- bom/
|       +-- images/
|-- config/
|   |-- pihole/            # Pi-hole Konfiguration
|   |-- wireguard/         # WireGuard Configs (wg0.conf, wg1.conf)
|   |-- traefik/           # Traefik Static & Dynamic Config
|   |-- opencloud/         # Opencloud docker-compose & env
|   |-- passbolt/          # Passbolt docker-compose & env
|   |-- crowdsec/          # CrowdSec Profile & Bouncer
|   +-- fail2ban/          # Jail-Konfigurationen
|-- scripts/
|   |-- setup.sh           # Vollstaendiges Setup-Skript
|   |-- update.sh          # Update aller Dienste
|   +-- backup.sh          # Backup-Skript
|-- docs/
|   |-- network-diagram.md
|   +-- troubleshooting.md
+-- README.md
```

---

## Setup & Installation

### Voraussetzungen

- Raspberry Pi 4 mit Raspberry Pi OS Lite (64-bit)
- VPS mit Ubuntu 22.04 oder Debian 12
- Domain (fuer TLS-Zertifikate und Subdomains)
- PCB Simple oder PCB Extended betueckt und angeschlossen

### 1. Repository klonen

```bash
git clone https://github.com/PoWeR1337/AWEOMA.git
cd AWEOMA
```

### 2. Konfiguration anpassen

```bash
# WireGuard
cp config/wireguard/wg0.conf.example config/wireguard/wg0.conf
nano config/wireguard/wg0.conf

# Opencloud
cp config/opencloud/.env.example config/opencloud/.env
nano config/opencloud/.env

# Passbolt
cp config/passbolt/.env.example config/passbolt/.env
nano config/passbolt/.env
```

### 3. Setup-Skript ausfuehren

```bash
chmod +x scripts/setup.sh
sudo ./scripts/setup.sh
```

### 4. Dienste pruefen

```bash
sudo wg show                # WireGuard Status
pihole status               # Pi-hole Status
docker compose -f config/opencloud/docker-compose.yml ps
docker compose -f config/passbolt/docker-compose.yml ps
btop                        # System-Monitor
```

---

> Projekt von [PoWeR1337](https://github.com/PoWeR1337)
