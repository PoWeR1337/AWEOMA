#!/bin/bash
# AWEOMA – Backup Script
# Sichert alle wichtigen Konfigurationen und Daten.
# Ausfuehren mit: sudo ./scripts/backup.sh
# Empfohlen: Als Cronjob einrichten (taeglich)

set -euo pipefail

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC}  $1"; }
log_ok()   { echo -e "${GREEN}[OK]${NC}    $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC}  $1"; }

# ============================================================
# Konfiguration
# ============================================================
BACKUP_DIR="${BACKUP_DIR:-/var/backups/aweoma}"
DATE=$(date +%Y-%m-%d_%H-%M)
BACKUP_FILE="${BACKUP_DIR}/aweoma-backup-${DATE}.tar.gz"
KEEP_DAYS="${KEEP_DAYS:-14}"  # Backups aelter als X Tage loeschen

# ============================================================
# Backup-Verzeichnis erstellen
# ============================================================
mkdir -p "${BACKUP_DIR}"

echo ""
echo "======================================================"
echo "  AWEOMA – Backup [${DATE}]"
echo "======================================================"
echo ""

# Temporaeres Verzeichnis fuer Backup-Inhalte
TMP_DIR=$(mktemp -d)
trap "rm -rf ${TMP_DIR}" EXIT

# ============================================================
# WireGuard Konfigurationen sichern
# ============================================================
log_info "WireGuard Konfigurationen sichern..."
if [[ -d /etc/wireguard ]]; then
  mkdir -p "${TMP_DIR}/wireguard"
  cp -r /etc/wireguard/*.conf "${TMP_DIR}/wireguard/" 2>/dev/null || true
  log_ok "WireGuard gesichert."
else
  log_warn "/etc/wireguard nicht gefunden."
fi

# ============================================================
# Pi-hole Konfiguration sichern
# ============================================================
log_info "Pi-hole sichern..."
if command -v pihole &>/dev/null; then
  mkdir -p "${TMP_DIR}/pihole"
  pihole -a -t "${TMP_DIR}/pihole/pihole-backup-${DATE}.tar.gz" 2>/dev/null || true
  log_ok "Pi-hole gesichert."
fi

# ============================================================
# Traefik Konfiguration sichern
# ============================================================
log_info "Traefik Konfigurationen sichern..."
if [[ -d /etc/traefik ]]; then
  mkdir -p "${TMP_DIR}/traefik"
  cp -r /etc/traefik "${TMP_DIR}/traefik/" 2>/dev/null || true
  # ACME-Zertifikate nicht vergessen!
  log_ok "Traefik gesichert."
fi

# ============================================================
# Fail2ban Konfiguration sichern
# ============================================================
log_info "Fail2ban sichern..."
if [[ -f /etc/fail2ban/jail.local ]]; then
  mkdir -p "${TMP_DIR}/fail2ban"
  cp /etc/fail2ban/jail.local "${TMP_DIR}/fail2ban/"
  log_ok "Fail2ban gesichert."
fi

# ============================================================
# Opencloud Daten sichern (Docker Volume)
# ============================================================
log_info "Opencloud-Daten sichern..."
if docker volume inspect opencloud_data &>/dev/null; then
  mkdir -p "${TMP_DIR}/opencloud"
  docker run --rm \
    -v opencloud_data:/data \
    -v "${TMP_DIR}/opencloud":/backup \
    alpine tar czf /backup/opencloud-data-${DATE}.tar.gz -C /data . 2>/dev/null || true
  log_ok "Opencloud-Daten gesichert."
fi

# ============================================================
# Sonstiges sichern
# ============================================================
log_info "Sonstige Konfigurationen sichern..."
mkdir -p "${TMP_DIR}/system"
# Crontab sichern
crontab -l > "${TMP_DIR}/system/crontab-${DATE}.txt" 2>/dev/null || true
# UFW-Regeln sichern
ufw status verbose > "${TMP_DIR}/system/ufw-status-${DATE}.txt" 2>/dev/null || true
log_ok "Sonstiges gesichert."

# ============================================================
# Alles in ein Archiv packen
# ============================================================
log_info "Archiv erstellen: ${BACKUP_FILE}"
tar czf "${BACKUP_FILE}" -C "${TMP_DIR}" .
chmod 600 "${BACKUP_FILE}"
log_ok "Backup erstellt: ${BACKUP_FILE} ($(du -sh ${BACKUP_FILE} | cut -f1))"

# ============================================================
# Alte Backups loeschen
# ============================================================
log_info "Backups aelter als ${KEEP_DAYS} Tage loeschen..."
find "${BACKUP_DIR}" -name "aweoma-backup-*.tar.gz" -mtime +"${KEEP_DAYS}" -delete
log_ok "Alte Backups bereinigt."

echo ""
log_ok "Backup abgeschlossen!"
echo "  Speicherort: ${BACKUP_FILE}"
echo "  Tipp: Backups regelmaessig auf externe Speicher uebertragen!"
echo "======================================================"
echo ""

# Cronjob-Hinweis
# Um dieses Script taeglich um 2 Uhr auszufuehren:
# 0 2 * * * root /opt/aweoma/scripts/backup.sh >> /var/log/aweoma-backup.log 2>&1
