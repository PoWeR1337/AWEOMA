# AWEOMA — Raspberry Pi Gateway

> **A**ll-in-one **W**ireguard, **E**dge, **O**pencloud & **M**onitoring **A**ppliance
> Ein selbst gehostetes Netzwerk-Gateway auf Basis eines Raspberry Pi mit eigenem PCB, VPS-Anbindung und einem vollstaendigen Service-Stack.

---

## Inhalt

- [Projektuebersicht](#projektuebersicht)
- [Hardware](#hardware)
- [Netzwerkarchitektur](#netzwerkarchitektur)
- [Software-Stack](#software-stack)
- [VPS-Anbindung](#vps-anbindung)
- [Verzeichnisstruktur](#verzeichnisstruktur)
- [Setup & Installation](#setup--installation)

---

## Projektuebersicht

AWEOMA ist ein kompaktes, selbst gehostetes Netzwerk-Gateway auf einem **Raspberry Pi**, erweitert durch ein **massgeschneidertes PCB**. Das System fungiert als DNS-Resolver, VPN-Router, Reverse Proxy, Cloud-Storage-Server und Intrusion-Prevention-System gleichzeitig.

---

## Hardware

### Raspberry Pi

| Eigenschaft | Wert |
|---|---|
| Modell | Raspberry Pi 4 Model B (4 GB / 8 GB RAM) |
| OS | Raspberry Pi OS Lite 64-bit |
| Speicher | microSD >= 32 GB oder USB-SSD |
| Stromversorgung | 5 V / 3 A USB-C (ueber PCB) |

### Custom PCB

Das PCB wurde speziell fuer dieses Projekt entworfen:

| Feature | Beschreibung |
|---|---|
| Stromversorgung | Geregelte 5 V / 3 A fuer den Pi via USB-C oder Barrel-Jack |
| Status-LEDs | LEDs fuer Power, VPN-Status, WAN-Link und Fehler |
| Reset-Taster | Hardware-Reset ohne SSH-Zugang |
| GPIO-Breakout | Zugaengliche GPIO-Header fuer Erweiterungen |
| RJ45-Passthrough | Saubere Kabelführung fuer LAN/WAN |
| Formfaktor | HAT-kompatibel, passt in Standard-Hut-Gehaeuse |

> PCB-Dateien (Schaltplan, Gerber, BOM) befinden sich im Verzeichnis /hardware.

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

1. **VPS-Tunnel**: Sicherer Kanal fuer eingehenden Traffic
2. **ProtonVPN-Ausgang**: Gesamter ausgehender Traffic laeuft ueber ProtonVPN

### Traefik

Laeuft auf VPS und lokal. Uebernimmt automatische TLS-Zertifikate (Lets Encrypt), Routing und Middleware (Auth, Rate-Limiting).

### Opencloud

Privater Cloud-Speicher mit WebDAV, Desktop-/Mobil-Sync und Dateiversionierung.

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

Der Pi verbindet sich beim Start automatisch per WireGuard mit dem VPS.

---

## Verzeichnisstruktur

```
AWEOMA/
|-- hardware/
|   |-- schematic/     # KiCad Schaltplan
|   |-- gerber/        # Fertigungsdateien fuer PCB-Hersteller
|   |-- bom/           # Stueckliste
|   +-- images/        # Fotos & Renders des PCBs
|-- config/
|   |-- pihole/        # Pi-hole Konfiguration
|   |-- wireguard/     # WireGuard Configs (wg0.conf, wg1.conf)
|   |-- traefik/       # Traefik Static & Dynamic Config
|   |-- opencloud/     # Opencloud docker-compose & env
|   |-- crowdsec/      # CrowdSec Profile & Bouncer
|   +-- fail2ban/      # Jail-Konfigurationen
|-- scripts/
|   |-- setup.sh       # Vollstaendiges Setup-Skript
|   |-- update.sh      # Update aller Dienste
|   +-- backup.sh      # Backup-Skript
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
- Domain (fuer TLS-Zertifikate)
- PCB betueckt und angeschlossen

### 1. Repository klonen

```bash
git clone https://github.com/PoWeR1337/AWEOMA.git
cd AWEOMA
```

### 2. Konfiguration anpassen

```bash
cp config/wireguard/wg0.conf.example config/wireguard/wg0.conf
nano config/wireguard/wg0.conf
```

### 3. Setup-Skript ausfuehren

```bash
chmod +x scripts/setup.sh
sudo ./scripts/setup.sh
```

### 4. Dienste pruefen

```bash
sudo wg show        # WireGuard Status
pihole status       # Pi-hole Status
docker logs traefik # Traefik Logs
btop                # System-Monitor
```

---

> Projekt von [PoWeR1337](https://github.com/PoWeR1337)
