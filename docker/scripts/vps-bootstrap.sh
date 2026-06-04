#!/usr/bin/env bash
# One-time VPS setup (Ubuntu 22.04/24.04). Run as root or with sudo:
#   curl -fsSL ... | bash
#   bash docker/scripts/vps-bootstrap.sh
set -euo pipefail

if [ "$(id -u)" -ne 0 ]; then
  echo "Run as root: sudo bash docker/scripts/vps-bootstrap.sh" >&2
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive

echo "==> Updating packages..."
apt-get update -qq
apt-get upgrade -y -qq

echo "==> Installing Docker..."
if ! command -v docker >/dev/null 2>&1; then
  apt-get install -y -qq ca-certificates curl gnupg
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc
  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
    $(. /etc/os-release && echo "${VERSION_CODENAME}") stable" \
    > /etc/apt/sources.list.d/docker.list
  apt-get update -qq
  apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-compose-plugin
fi

echo "==> Installing docker-compose-v2 (fallback)..."
apt-get install -y -qq docker-compose-v2 2>/dev/null || true

echo "==> Configuring firewall (UFW)..."
apt-get install -y -qq ufw
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

echo "==> Creating app directory..."
mkdir -p /opt/stayhub
chmod 755 /opt/stayhub

echo "==> Docker version:"
docker --version
docker compose version 2>/dev/null || docker-compose version 2>/dev/null || true

echo
echo "Bootstrap complete."
echo "Next: upload project to /opt/stayhub and run docker/scripts/vps-deploy.sh"
