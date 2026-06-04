#!/usr/bin/env bash
# Pull Backend + Frontend rồi build & up Docker (dùng cho CI/CD trên máy bạn).
#
#   bash docker/scripts/local-auto-deploy.sh
#   GIT_BRANCH=main STAYHUB_SHARE=1 bash docker/scripts/local-auto-deploy.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

CONFIG="${BACKEND_DIR}/.github/local-ci.env"
if [ -f "$CONFIG" ]; then
  # shellcheck source=/dev/null
  source "$CONFIG"
fi

BACKEND="${STAYHUB_BACKEND:-$BACKEND_DIR}"
FRONTEND="${STAYHUB_FRONTEND:-$(dirname "$(dirname "$BACKEND")")/Frontend/SEP490_08-Frontend}"
GIT_BRANCH="${GIT_BRANCH:-main}"
STAYHUB_SHARE="${STAYHUB_SHARE:-1}"

pull_repo() {
  local dir="$1"
  if [ ! -d "$dir/.git" ]; then
    echo "ERROR: Not a git repo: $dir" >&2
    exit 1
  fi
  echo "==> Pull $dir (branch: $GIT_BRANCH)"
  cd "$dir"
  git fetch origin --prune
  local branch="$GIT_BRANCH"
  if ! git rev-parse --verify "origin/$branch" >/dev/null 2>&1; then
    branch="master"
  fi
  if ! git rev-parse --verify "origin/$branch" >/dev/null 2>&1; then
    echo "ERROR: origin/$GIT_BRANCH and origin/master not found in $dir" >&2
    exit 1
  fi
  git reset --hard "origin/$branch"
}

pull_repo "$BACKEND"
pull_repo "$FRONTEND"

cd "$BACKEND"
chmod +x docker/scripts/*.sh 2>/dev/null || true

if [ ! -f .env ] && [ -f .env.example ]; then
  cp .env.example .env
fi

compose_files=(-f docker-compose.yml)
if [ "$STAYHUB_SHARE" = "1" ]; then
  compose_files+=(-f docker-compose.share.yml)
fi

echo "==> Docker compose up --build"
if docker compose version >/dev/null 2>&1; then
  docker compose "${compose_files[@]}" up -d --build
else
  docker-compose "${compose_files[@]}" up -d --build
fi

IP="$(hostname -I | awk '{print $1}')"
echo
echo "Deploy xong."
echo "  Local: http://localhost:8888"
echo "  LAN:   http://${IP}:8888"
echo "  Tunnel: bash docker/scripts/host-local.sh tunnel"
