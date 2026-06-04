#!/usr/bin/env bash
# Install cloudflared when apt package is unavailable (Ubuntu resolute, etc.)
set -euo pipefail

ARCH="$(uname -m)"
case "$ARCH" in
  x86_64|amd64) DEB="cloudflared-linux-amd64.deb"; BIN="cloudflared-linux-amd64" ;;
  aarch64|arm64) DEB="cloudflared-linux-arm64.deb"; BIN="cloudflared-linux-arm64" ;;
  *)
    echo "Unsupported arch: $ARCH" >&2
    exit 1
    ;;
esac

URL_BASE="https://github.com/cloudflare/cloudflared/releases/latest/download"
INSTALL_DIR="${HOME}/.local/bin"
mkdir -p "$INSTALL_DIR"

echo "==> Downloading cloudflared (${BIN})..."
curl -fsSL "${URL_BASE}/${BIN}" -o "${INSTALL_DIR}/cloudflared"
chmod +x "${INSTALL_DIR}/cloudflared"

if ! echo ":${PATH}:" | grep -q ":${INSTALL_DIR}:"; then
  echo
  echo "Add to PATH (one time):"
  echo "  echo 'export PATH=\"\$HOME/.local/bin:\$PATH\"' >> ~/.zshrc"
  echo "  source ~/.zshrc"
fi

echo
"${INSTALL_DIR}/cloudflared" --version
echo
echo "Installed: ${INSTALL_DIR}/cloudflared"
echo "Run tunnel:"
echo "  bash docker/scripts/host-local.sh tunnel"
