#!/bin/bash
# AWEOMA – Update Script
# Aktualisiert alle installierten Services.
# Ausfuehren mit: sudo ./scripts/update.sh

set -euo pipefail

GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC}  $1"; }
log_ok()   { echo -e "${GREEN}[OK]${NC}    $1"; }

echo ""
echo "======================================================"
echo "  AWEOMA – Update"
echo "======================================================"
echo ""

# System-Pakete aktualisieren
log_info "System-Pakete aktualisieren..."
apt update -qq && apt upgrade -y -qq
log_ok "System aktualisiert."

# Docker-Container aktualisieren
log_info "Docker-Images aktualisieren..."
docker compose -f config/opencloud/docker-compose.yml pull 2>/dev/null || true
docker compose -f config/opencloud/docker-compose.yml up -d 2>/dev/null || true
docker image prune -f
log_ok "Docker-Container aktualisiert."

# Pi-hole aktualisieren
if command -v pihole &>/dev/null; then
  log_info "Pi-hole aktualisieren..."
  pihole -up
  log_ok "Pi-hole aktualisiert."
fi

# CrowdSec aktualisieren
if command -v cscli &>/dev/null; then
  log_info "CrowdSec Hub aktualisieren..."
  cscli hub update
  cscli hub upgrade
  systemctl restart crowdsec
  log_ok "CrowdSec aktualisiert."
fi

# Fail2ban neu starten
if systemctl is-active --quiet fail2ban; then
  systemctl restart fail2ban
  log_ok "Fail2ban neugestartet."
fi

# WireGuard-Verbindung pruefen
log_info "WireGuard-Status:"
wg show 2>/dev/null || echo "WireGuard nicht aktiv."

echo ""
log_ok "Update abgeschlossen! Bitte Logs pruefen."
echo "======================================================"
