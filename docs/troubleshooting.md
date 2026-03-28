# AWEOMA – Troubleshooting

Haeufige Probleme und Loesungen fuer das AWEOMA Raspberry Pi Gateway.

---

## WireGuard

### VPN-Tunnel startet nicht

**Symptom:** `sudo wg-quick up wg0` schlaegt fehl.

**Moegliche Ursachen & Loesungen:**

1. **Falsche Konfiguration:** Schluessel, IP oder Endpoint pruefen.
   ```bash
   cat /etc/wireguard/wg0.conf
   # Sicherstellen: PrivateKey, PublicKey und Endpoint korrekt
   ```

2. **Port blockiert:** UFW-Regel pruefen.
   ```bash
   sudo ufw status | grep 51820
   # Falls fehlt:
   sudo ufw allow 51820/udp
   ```

3. **WireGuard-Kernel-Modul fehlt:**
   ```bash
   sudo modprobe wireguard
   sudo dmesg | grep wireguard
   ```

### Kein Internet ueber VPN-Tunnel

**Symptom:** VPN verbunden, aber kein Internetzugang.

```bash
# IP-Forwarding pruefen
cat /proc/sys/net/ipv4/ip_forward  # Muss 1 sein

# NAT-Regeln pruefen
sudo iptables -t nat -L POSTROUTING -n -v

# WireGuard Status
sudo wg show
```

---

## Pi-hole

### Pi-hole blockiert zu viel (Whitelist)

```bash
# Domain auf Whitelist setzen
pihole -w beispiel-domain.de

# Whitelist anzeigen
pihole -w -l
```

### Pi-hole blockiert zu wenig (Blockliste aktualisieren)

```bash
pihole -g    # Blocklisten aktualisieren
pihole -f    # Cache leeren
```

### DNS-Aufloesung funktioniert nicht

```bash
# Pi-hole Status
pihole status

# Pi-hole neu starten
pihole restartdns

# Direkt testen
dig @127.0.0.1 google.com
```

---

## Traefik

### Zertifikat wird nicht ausgestellt (Let's Encrypt)

**Haeufige Ursachen:**

1. **Port 80 nicht erreichbar** – Let's Encrypt braucht HTTP-Zugang fuer die Challenge.
2. **DNS zeigt nicht auf VPS** – A-Record pruefen.
3. **Rate-Limit erreicht** – Staging-Server verwenden.

```bash
# Traefik Logs pruefen
docker logs traefik --tail=100 | grep -i "error|acme"

# ACME-Daten loeschen und neu anfordern (Vorsicht: Rate-Limit!)
# rm /etc/traefik/acme/acme.json && docker restart traefik
```

### Dienst nicht erreichbar (502 Bad Gateway)

```bash
# Traefik-Routing pruefen
docker logs traefik --tail=50

# Container-Netzwerk pruefen
docker network inspect proxy

# Service-Labels pruefen
docker inspect CONTAINER_NAME | grep -i traefik
```

---

## Fail2ban

### IP wurde gebaent – manuell freischalten

```bash
# Gebannte IPs anzeigen
sudo fail2ban-client status sshd

# IP freischalten
sudo fail2ban-client set sshd unbanip 1.2.3.4

# Fail2ban Log pruefen
sudo tail -f /var/log/fail2ban.log
```

### Fail2ban laeuft nicht

```bash
sudo systemctl status fail2ban
sudo journalctl -u fail2ban --since "10 minutes ago"
sudo fail2ban-client ping
```

---

## CrowdSec

### CrowdSec-Agent laeuft nicht

```bash
sudo systemctl status crowdsec
sudo journalctl -u crowdsec --since "10 minutes ago"
```

### Gebannte IPs anzeigen

```bash
sudo cscli decisions list
# IP manuell bannen
sudo cscli decisions add --ip 1.2.3.4 --reason "manuell"
# IP freischalten
sudo cscli decisions delete --ip 1.2.3.4
```

---

## Opencloud

### Opencloud Container startet nicht

```bash
cd config/opencloud
docker compose logs opencloud

# Haeufige Ursache: .env fehlt oder falsch
ls -la .env
docker compose config
```

### Speicherplatz voll

```bash
df -h
docker system df
# Unbenutzte Images und Volumes loeschen
docker system prune -a --volumes
```

---

## Allgemeines

### System-Logs pruefen

```bash
# Kernel-Logs
sudo dmesg | tail -50

# Systemd-Journal
sudo journalctl -xe --since "1 hour ago"

# Alle Services-Status
systemctl --failed
```

### Netzwerkverbindung debuggen

```bash
# Netzwerk-Interfaces
ip addr show
ip route show

# Verbindung zum VPS testen
ping VPS_IP
traceroute VPS_IP

# DNS testen
nslookup google.com 127.0.0.1
```

### Raspberry Pi GPIO / PCB

```bash
# GPIO-Status anzeigen
gpio readall 2>/dev/null || raspi-gpio get

# LED-Test (GPIO 17 als Beispiel)
gpio -g write 17 1   # LED an
gpio -g write 17 0   # LED aus
```

---

## Log-Dateien Uebersicht

| Service    | Log-Pfad                              |
|------------|---------------------------------------|
| WireGuard  | `sudo journalctl -u wg-quick@wg0`    |
| Pi-hole    | `/var/log/pihole/pihole.log`          |
| Traefik    | `/var/log/traefik/traefik.log`        |
| Fail2ban   | `/var/log/fail2ban.log`               |
| CrowdSec   | `/var/log/crowdsec/crowdsec.log`      |
| System     | `/var/log/syslog`                     |
| Auth/SSH   | `/var/log/auth.log`                   |
