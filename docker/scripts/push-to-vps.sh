#!/usr/bin/env bash
# Deploy from your laptop to VPS via rsync + SSH.
#
# Usage:
#   bash docker/scripts/push-to-vps.sh user@YOUR_VPS_IP yourdomain.com [api.yourdomain.com]
#
# Example:
#   bash docker/scripts/push-to-vps.sh root@123.45.67.89 stayhub.vn
#   bash docker/scripts/push-to-vps.sh ubuntu@123.45.67.89 stayhub.vn api.stayhub.vn
#
# Prerequisites:
#   - SSH key access to VPS
#   - DNS A record: yourdomain.com -> VPS IP
#   - DNS A record: api.yourdomain.com -> VPS IP
#   - Ports 80/443 open on VPS
set -euo pipefail

VPS_SSH="${1:-}"
DOMAIN="${2:-}"
API_DOMAIN="${3:-api.${2:-}}"
ACME_EMAIL="${4:-}"

if [ -z "$VPS_SSH" ] || [ -z "$DOMAIN" ]; then
  echo "Usage: bash docker/scripts/push-to-vps.sh user@VPS_IP domain.com [api.domain.com] [acme_email]" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECTS_ROOT="$(cd "$BACKEND_ROOT/../.." && pwd)"
REMOTE_DIR="/opt/stayhub"

if [ ! -f "$BACKEND_ROOT/.env" ]; then
  echo "Missing $BACKEND_ROOT/.env" >&2
  exit 1
fi

if [ -z "$ACME_EMAIL" ]; then
  ACME_EMAIL="admin@${DOMAIN}"
fi

echo "==> Target: $VPS_SSH"
echo "    Domain: https://$DOMAIN"
echo "    API   : https://$API_DOMAIN"
echo

echo "==> Bootstrapping VPS (Docker + firewall)..."
ssh -o StrictHostKeyChecking=accept-new "$VPS_SSH" "mkdir -p $REMOTE_DIR"
rsync -avz "$BACKEND_ROOT/docker/scripts/vps-bootstrap.sh" "$VPS_SSH:/tmp/vps-bootstrap.sh"
ssh "$VPS_SSH" "sudo bash /tmp/vps-bootstrap.sh"

echo "==> Uploading Backend + Frontend..."
rsync -avz --delete \
  --exclude 'bin/' --exclude 'obj/' --exclude '.vs/' --exclude 'node_modules/' --exclude '.git/' \
  "$PROJECTS_ROOT/Backend/" "$VPS_SSH:$REMOTE_DIR/Backend/"

rsync -avz --delete \
  --exclude 'node_modules/' --exclude 'dist/' --exclude '.git/' \
  "$PROJECTS_ROOT/Frontend/" "$VPS_SSH:$REMOTE_DIR/Frontend/"

echo "==> Writing production .env on VPS..."
PROD_ENV="$(mktemp)"
trap 'rm -f "$PROD_ENV"' EXIT

cp "$BACKEND_ROOT/.env" "$PROD_ENV"
sed -i \
  -e "s|^DOMAIN=.*|DOMAIN=${DOMAIN}|" \
  -e "s|^API_DOMAIN=.*|API_DOMAIN=${API_DOMAIN}|" \
  -e "s|^ACME_EMAIL=.*|ACME_EMAIL=${ACME_EMAIL}|" \
  -e "s|^PUBLIC_API_URL=.*|PUBLIC_API_URL=https://${API_DOMAIN}|" \
  -e "s|^PUBLIC_FRONTEND_URL=.*|PUBLIC_FRONTEND_URL=https://${DOMAIN}|" \
  -e "s|^VITE_API_BASE_URL=.*|VITE_API_BASE_URL=https://${API_DOMAIN}|" \
  -e "s|^SQL_HOST_PORT=.*|SQL_HOST_PORT=14333|" \
  "$PROD_ENV"

# Append keys if missing from local .env
grep -q '^DOMAIN=' "$PROD_ENV" || echo "DOMAIN=${DOMAIN}" >> "$PROD_ENV"
grep -q '^API_DOMAIN=' "$PROD_ENV" || echo "API_DOMAIN=${API_DOMAIN}" >> "$PROD_ENV"
grep -q '^ACME_EMAIL=' "$PROD_ENV" || echo "ACME_EMAIL=${ACME_EMAIL}" >> "$PROD_ENV"

rsync -avz "$PROD_ENV" "$VPS_SSH:$REMOTE_DIR/Backend/SEP490_08-Backend/.env"

echo "==> Running production deploy on VPS..."
ssh "$VPS_SSH" "cd $REMOTE_DIR/Backend/SEP490_08-Backend && chmod +x docker/scripts/*.sh && bash docker/scripts/vps-deploy.sh"

echo
echo "Done! Open https://${DOMAIN}"
echo "Check: ssh $VPS_SSH 'cd $REMOTE_DIR/Backend/SEP490_08-Backend && docker compose -f docker-compose.yml -f docker-compose.prod.yml ps'"
