#!/usr/bin/env bash
# Share StayHub FREE — no VPS needed.
#
# Mode 1 — Same WiFi (LAN):
#   bash docker/scripts/share-free.sh lan
#
# Mode 2 — Public link (Cloudflare Tunnel, free HTTPS):
#   bash docker/scripts/share-free.sh tunnel
#   (install: sudo apt install cloudflared  OR  download from developers.cloudflare.com)
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

MODE="${1:-lan}"

compose() {
  if docker compose version >/dev/null 2>&1; then
    docker compose -f docker-compose.yml -f docker-compose.share.yml "$@"
  else
    docker-compose -f docker-compose.yml -f docker-compose.share.yml "$@"
  fi
}

if [ ! -f .env ]; then
  echo "Missing .env — copy from .env.example first." >&2
  exit 1
fi

echo "==> Building frontend (SAME_ORIGIN API) + share proxy on :8888 ..."
compose up -d --build share-proxy frontend gateway-api

LAN_IP="$(hostname -I | awk '{print $1}')"

echo
echo "=========================================="
case "$MODE" in
  lan)
    echo "  StayHub LAN demo (cùng WiFi / cùng mạng trường)"
    echo "  URL: http://${LAN_IP}:8888"
    echo
    echo "  Gửi link trên cho bạn / giảng viên (cùng mạng)."
    echo "  Nếu không vào được: WiFi trường có thể chặn máy-máy → thử: bash docker/scripts/share-free.sh tunnel"
    ;;
  tunnel)
    if ! command -v cloudflared >/dev/null 2>&1; then
      if [ -x "${HOME}/.local/bin/cloudflared" ]; then
        export PATH="${HOME}/.local/bin:${PATH}"
      else
      echo "  Chưa có cloudflared. Cài một trong các cách sau:" >&2
      echo
      echo "  Cách 1 (khuyến nghị — không cần apt):"
      echo "    bash docker/scripts/install-cloudflared.sh"
      echo "    source ~/.zshrc   # nếu script báo thêm PATH"
      echo
      echo "  Cách 2 (file .deb đã tải về /tmp):"
      echo "    curl -fsSL https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -o /tmp/cloudflared.deb"
      echo "    sudo dpkg -i /tmp/cloudflared.deb"
      echo
      echo "  Sau khi cài, chạy lại:"
      echo "    bash docker/scripts/host-local.sh tunnel"
      echo
      echo "  Tạm thời dùng WiFi (không cần cloudflared):"
      echo "    http://${LAN_IP}:8888"
      exit 1
      fi
    fi
    echo "  Cloudflare Tunnel (miễn phí, HTTPS)"
    echo "  Đang tạo link public... (Ctrl+C để dừng)"
    echo "=========================================="
    echo
    exec cloudflared tunnel --url "http://127.0.0.1:8888"
    ;;
  *)
    echo "Usage: bash docker/scripts/share-free.sh [lan|tunnel]" >&2
    exit 1
    ;;
esac
echo "=========================================="
echo "  API + SignalR đi chung cổng 8888 (không cần rebuild IP từng lần)."
echo "  Chi phí: 0đ"
