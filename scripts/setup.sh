#!/bin/bash
# AWEOMA – Setup Script
# Installiert und konfiguriert alle Services auf dem Raspberry Pi.
# Ausfuehren mit: sudo ./scripts/setup.sh

set -euo pipefail

# ============================================================
# Farben fuer Ausgabe
# ============================================================
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info()    { echo -e "${BLUE}[INFO]${NC}  $1"; }
log_ok()      { echo -e "${GREEN}[OK]${NC}    $1"; }
log_warn()    { echo -e "${YELLOW}[WARN]${NC}  $1"; }
log_error()   { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

# ============================================================
# Vorbedingungen pruefen
# ============================================================
check_root() {
  if [[ $EUID -ne 0 ]]; then
    log_error "Bitte als root ausfuehren: sudo $0"
  fi
}

check_os() {
  if ! grep -q "Raspberry Pi" /proc/cpuinfo 2>/dev/null; then
    log_warn "Kein Raspberry Pi erkannt – Setup laeuft trotzdem weiter."
  fi
  if ! command -v apt &>/dev/null; then
    log_error "Nur Debian/Ubuntu-basierte Systeme werden unterstuetzt."
  fi
}

# ============================================================
# System aktualisieren
# ============================================================
update_system() {
  log_info "System aktualisieren..."
  apt update -qq && apt upgrade -y -qq
  apt install -y -qq curl wget git htop net-tools ufw fail2ban
  log_ok "System aktualisiert."
}

# ============================================================
# Docker installieren
# ============================================================
install_docker() {
  if command -v docker &>/dev/null; then
    log_ok "Docker bereits installiert: $(docker --version)"
    return
  fi
  log_info "Docker installieren..."
  curl -fsSL https://get.docker.com | sh
  usermod -aG docker "${SUDO_USER:-pi}"
  systemctl enable docker
  systemctl start docker
  log_ok "Docker installiert."
}

# ============================================================
# WireGuard installieren
# ============================================================
install_wireguard() {
  log_info "WireGuard installieren..."
  apt install -y -qq wireguard wireguard-tools
  
  if [[ ! -f /etc/wireguard/wg0.conf ]]; then
    if [[ -f "config/wireguard/wg0.conf" ]]; then
      cp config/wireguard/wg0.conf /etc/wireguard/wg0.conf
      chmod 600 /etc/wireguard/wg0.conf
      log_ok "wg0.conf kopiert."
    else
      log_warn "Keine wg0.conf gefunden. Bitte manuell unter /etc/wireguard/wg0.conf anlegen."
    fi
  fi
  
  systemctl enable wg-quick@wg0
  log_ok "WireGuard konfiguriert."
}

# ============================================================
# Pi-hole installieren
# ============================================================
install_pihole() {
  if command -v pihole &>/dev/null; then
    log_ok "Pi-hole bereits installiert."
    return
  fi
  log_info "Pi-hole installieren..."
  curl -sSL https://install.pi-hole.net | bash /dev/stdin --unattended
  log_ok "Pi-hole installiert."
}

# ============================================================
# Fail2ban konfigurieren
# ============================================================
configure_fail2ban() {
  log_info "Fail2ban konfigurieren..."
  if [[ -f "config/fail2ban/jail.local" ]]; then
    cp config/fail2ban/jail.local /etc/fail2ban/jail.local
  fi
  systemctl enable fail2ban
  systemctl restart fail2ban
  log_ok "Fail2ban konfiguriert."
}

# ============================================================
# CrowdSec installieren
# ============================================================
install_crowdsec() {
  if command -v cscli &>/dev/null; then
    log_ok "CrowdSec bereits installiert."
    return
  fi
  log_info "CrowdSec installieren..."
  curl -s https://packagecloud.io/install/repositories/crowdsec/crowdsec/script.deb.sh | bash
  apt install -y crowdsec
  
  # Collections installieren
  cscli collections install crowdsecurity/linux
  cscli collections install crowdsecurity/sshd
  cscli collections install crowdsecurity/traefik
  
  # Bouncer fuer iptables
  apt install -y crowdsec-firewall-bouncer-iptables
  
  if [[ -f "config/crowdsec/acquis.yaml" ]]; then
    cp config/crowdsec/acquis.yaml /etc/crowdsec/acquis.yaml
  fi
  
  systemctl enable crowdsec
  systemctl restart crowdsec
  log_ok "CrowdSec installiert."
}

# ============================================================
# ntpdate konfigurieren
# ============================================================
configure_ntp() {
  log_info "NTP konfigurieren..."
  apt install -y -qq ntpdate
  ntpdate -u pool.ntp.org
  # Cronjob fuer regelmaessige Synchronisation
  (crontab -l 2>/dev/null; echo "0 */6 * * * /usr/sbin/ntpdate -u pool.ntp.org") | crontab -
  log_ok "NTP konfiguriert."
}

# ============================================================
# btop installieren
# ============================================================
install_btop() {
  if command -v btop &>/dev/null; then
    log_ok "btop bereits installiert."
    return
  fi
  log_info "btop installieren..."
  apt install -y -qq btop 2>/dev/null || {
    # Fallback: aus Snap oder Binary
    snap install btop 2>/dev/null || log_warn "btop konnte nicht installiert werden."
  }
  log_ok "btop installiert."
}

# ============================================================
# Firewall (UFW) konfigurieren
# ============================================================
configure_firewall() {
  log_info "Firewall konfigurieren..."
  ufw --force reset
  ufw default deny incoming
  ufw default allow outgoing
  ufw allow ssh
  ufw allow 51820/udp  # WireGuard
  ufw allow 53         # DNS (Pi-hole)
  ufw allow 80/tcp     # HTTP
  ufw allow 443/tcp    # HTTPS
  ufw --force enable
  log_ok "Firewall konfiguriert."
}

# ============================================================
# IP-Forwarding aktivieren
# ============================================================
enable_ip_forwarding() {
  log_info "IP-Forwarding aktivieren..."
  if ! grep -q "net.ipv4.ip_forward=1" /etc/sysctl.conf; then
    echo "net.ipv4.ip_forward=1" >> /etc/sysctl.conf
    echo "net.ipv6.conf.all.forwarding=1" >> /etc/sysctl.conf
  fi
  sysctl -p
  log_ok "IP-Forwarding aktiviert."
}

# ============================================================
# Opencloud starten
# ============================================================
start_opencloud() {
  log_info "Opencloud starten..."
  if [[ -f "config/opencloud/docker-compose.yml" ]]; then
    if [[ ! -f "config/opencloud/.env" ]]; then
      log_warn "config/opencloud/.env fehlt! Bitte .env.example kopieren und anpassen."
      return
    fi
    cd config/opencloud
    docker compose up -d
    cd ../..
    log_ok "Opencloud gestartet."
  else
    log_warn "docker-compose.yml fuer Opencloud nicht gefunden."
  fi
}

# ============================================================
# Hauptprogramm
# ============================================================
main() {
  echo ""
  echo "======================================================"
  echo "  AWEOMA – Raspberry Pi Gateway Setup"
  echo "======================================================"
  echo ""
  
  check_root
  check_os
  update_system
  enable_ip_forwarding
  install_docker
  install_wireguard
  install_pihole
  configure_fail2ban
  install_crowdsec
  configure_ntp
  install_btop
  configure_firewall
  start_opencloud
  
  echo ""
  echo "======================================================"
  log_ok "Setup abgeschlossen!"
  echo ""
  echo "Naechste Schritte:"
  echo "  1. WireGuard starten:    sudo wg-quick up wg0"
  echo "  2. Pi-hole pruefen:      pihole status"
  echo "  3. Systemmonitor:        btop"
  echo "  4. Firewall-Status:      sudo ufw status"
  echo "======================================================"
  echo ""
}

main "$@"
