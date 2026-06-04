#!/usr/bin/env bash
# Host StayHub trên máy Ubuntu của bạn (0đ).
#
#   bash docker/scripts/host-local.sh          # bật full stack
#   bash docker/scripts/host-local.sh share    # bật + proxy cổng 8888 (share WiFi)
#   bash docker/scripts/host-local.sh tunnel   # bật + link public Cloudflare
#   bash docker/scripts/host-local.sh stop     # tắt hết
#   bash docker/scripts/host-local.sh status   # xem trạng thái
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

compose() {
  if docker compose version >/dev/null 2>&1; then
    docker compose "$@"
  else
    docker-compose "$@"
  fi
}

compose_share() {
  if docker compose version >/dev/null 2>&1; then
    docker compose -f docker-compose.yml -f docker-compose.share.yml "$@"
  else
    docker-compose -f docker-compose.yml -f docker-compose.share.yml "$@"
  fi
}

cmd="${1:-up}"

case "$cmd" in
  up|start)
    if [ ! -f .env ]; then
      [ -f .env.example ] && cp .env.example .env
    fi
    chmod +x docker/scripts/init-db.sh 2>/dev/null || true
    echo "==> Starting StayHub on this machine..."
    compose up -d --build
    LAN_IP="$(hostname -I | awk '{print $1}')"
    echo
    echo "  Máy bạn:     http://localhost:5173"
    echo "  Cùng WiFi:   http://${LAN_IP}:5173  (cần rebuild IP — dùng 'share' bên dưới)"
    echo "  API:         http://localhost:7010"
    echo
    echo "  Share dễ hơn: bash docker/scripts/host-local.sh share"
    ;;
  share)
    exec bash docker/scripts/share-free.sh lan
    ;;
  tunnel)
    exec bash docker/scripts/share-free.sh tunnel
    ;;
  stop|down)
    echo "==> Stopping StayHub..."
    compose_share down 2>/dev/null || true
    compose down
    echo "Done."
    ;;
  status|ps)
    compose ps
    ;;
  logs)
    compose logs -f "${2:-gateway-api}"
    ;;
  *)
    echo "Usage: bash docker/scripts/host-local.sh [up|share|tunnel|stop|status|logs [service]]" >&2
    exit 1
    ;;
esac
