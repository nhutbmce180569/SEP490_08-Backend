#!/usr/bin/env bash
# Install Docker Compose plugin on Ubuntu/Debian if missing:
#   sudo apt-get update && sudo apt-get install -y docker-compose-v2
# Or: https://docs.docker.com/compose/install/

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

compose() {
  if docker compose version >/dev/null 2>&1; then
    docker compose "$@"
  elif command -v docker-compose >/dev/null 2>&1; then
    docker-compose "$@"
  else
    echo "Docker Compose is not installed." >&2
    echo "Install: sudo apt-get install -y docker-compose-v2" >&2
    exit 1
  fi
}

if [ ! -f .env ]; then
  if [ -f .env.example ]; then
    echo "Creating .env from .env.example ..."
    cp .env.example .env
    echo "Review .env and update secrets before production use."
  else
    echo "Missing .env — create one from .env.example or ask for the project .env template." >&2
    exit 1
  fi
fi

chmod +x docker/scripts/init-db.sh

echo "Building and starting StayHub stack..."
compose up -d --build

echo
echo "StayHub is starting."
echo "  Frontend : http://localhost:5173"
echo "  API      : http://localhost:7010"
echo "  SQL      : localhost:${SQL_HOST_PORT:-14333} (user: sa, password: see DB_PASSWORD in .env)"
echo
echo "First boot runs DB schema + sample data — allow 3–10 minutes."
echo "Follow logs:"
echo "  compose logs -f db-init"
echo "  compose logs -f gateway-api"
