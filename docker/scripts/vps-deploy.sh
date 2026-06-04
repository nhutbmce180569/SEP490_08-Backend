#!/usr/bin/env bash
# Run ON the VPS inside Backend/SEP490_08-Backend after code is uploaded.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

compose() {
  if docker compose version >/dev/null 2>&1; then
    docker compose -f docker-compose.yml -f docker-compose.prod.yml "$@"
  elif command -v docker-compose >/dev/null 2>&1; then
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml "$@"
  else
    echo "Docker Compose not installed. Run: sudo bash docker/scripts/vps-bootstrap.sh" >&2
    exit 1
  fi
}

env_val() {
  grep -E "^${1}=" .env | head -1 | cut -d= -f2- | tr -d '\r'
}

if [ ! -f .env ]; then
  echo "Missing .env — run push-to-vps.sh from your laptop or copy .env.production.example" >&2
  exit 1
fi

DOMAIN="$(env_val DOMAIN)"
API_DOMAIN="$(env_val API_DOMAIN)"
PUBLIC_API_URL="$(env_val PUBLIC_API_URL)"
PUBLIC_FRONTEND_URL="$(env_val PUBLIC_FRONTEND_URL)"
VITE_API_BASE_URL="$(env_val VITE_API_BASE_URL)"

for var_name in DOMAIN API_DOMAIN PUBLIC_API_URL PUBLIC_FRONTEND_URL VITE_API_BASE_URL; do
  if [ -z "${!var_name:-}" ]; then
    echo "Missing required .env variable: $var_name" >&2
    exit 1
  fi
done

if [ -z "$(env_val ACME_EMAIL)" ]; then
  echo "Warning: ACME_EMAIL not set — set admin email in .env for Let's Encrypt." >&2
fi

chmod +x docker/scripts/init-db.sh

echo "==> Building and starting StayHub (production)..."
compose --profile prod up -d --build

echo
echo "==> Deployment started."
echo "  Site : https://${DOMAIN}"
echo "  API  : https://${API_DOMAIN}"
echo
echo "DNS must point to this server. First HTTPS cert may take 1–2 minutes."
echo "Logs : docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f caddy gateway-api"
