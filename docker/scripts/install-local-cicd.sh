#!/usr/bin/env bash
# Cài CI/CD tự động trên máy Ubuntu (một lần).
#
#   bash docker/scripts/install-local-cicd.sh
#
# Chọn:
#   1) GitHub self-hosted runner — push lên GitHub → tự deploy (khuyên dùng)
#   2) systemd timer — mỗi 5 phút git fetch, có commit mới thì deploy
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
USER_NAME="${USER:-$(whoami)}"
RUNNER_DIR="${HOME}/actions-runner-stayhub-backend"

echo "StayHub — cài CI/CD local"
echo "Backend: $BACKEND_DIR"
echo

if ! command -v docker >/dev/null 2>&1; then
  echo "Cần cài Docker trước." >&2
  exit 1
fi

if ! groups "$USER_NAME" | grep -q docker; then
  echo "Cảnh báo: user $USER_NAME chưa trong group 'docker'."
  echo "  sudo usermod -aG docker $USER_NAME && newgrp docker"
  echo
fi

mkdir -p "$BACKEND_DIR/.github"
if [ ! -f "$BACKEND_DIR/.github/local-ci.env" ]; then
  cp "$BACKEND_DIR/.github/local-ci.env.example" "$BACKEND_DIR/.github/local-ci.env"
  echo "Đã tạo .github/local-ci.env"
fi

chmod +x "$BACKEND_DIR/docker/scripts/local-auto-deploy.sh"
chmod +x "$BACKEND_DIR/docker/scripts/install-local-cicd.sh"

echo "Chọn cách tự động:"
echo "  1) GitHub Actions self-hosted runner (push → deploy)"
echo "  2) systemd timer (poll git mỗi 5 phút)"
echo "  3) Cả hai"
read -r -p "Nhập 1 / 2 / 3 [1]: " choice
choice="${choice:-1}"

install_runner() {
  local default_url="https://github.com/nhutbmce180569/SEP490_08-Backend"

  echo
  echo "=== GitHub self-hosted runner ==="
  echo "1. Mở: https://github.com/nhutbmce180569/SEP490_08-Backend/settings/actions/runners/new"
  echo "2. Chọn Linux → copy TOKEN (chuỗi dài, hết hạn ~1h)"
  echo
  echo "Lưu ý:"
  echo "  - Repo URL: chỉ bấm Enter (dùng mặc định), KHÔNG dán token vào đây."
  echo "  - Token: dán vào ô thứ hai."
  echo

  read -r -p "Repo URL [Enter = mặc định]: " cfg_url
  cfg_url="${cfg_url:-$default_url}"

  if [[ ! "$cfg_url" =~ ^https://github.com/ ]]; then
    if [ -n "$cfg_url" ] && [[ "$cfg_url" =~ ^[A-Z0-9]{20,}$ ]]; then
      echo
      echo "Có vẻ bạn dán TOKEN vào ô Repo URL. Chạy lại script." >&2
      echo "  Repo URL → chỉ Enter"
      echo "  Token    → dán token GitHub"
      echo
      read -r -p "Dùng chuỗi vừa nhập làm Token? [y/N]: " use_as_token
      if [ "${use_as_token,,}" = "y" ]; then
        cfg_token="$cfg_url"
        cfg_url="$default_url"
      else
        return 1
      fi
    else
      echo "Repo URL phải bắt đầu bằng https://github.com/ ..." >&2
      return 1
    fi
  fi

  if [ -z "${cfg_token:-}" ]; then
    read -r -p "Token (dán từ GitHub, không hiện khi gõ): " cfg_token
  fi
  if [ -z "$cfg_token" ]; then
    echo "Thiếu token. Lấy token mới trên GitHub (token cũ có thể đã hết hạn)." >&2
    return 1
  fi

  mkdir -p "$RUNNER_DIR"
  cd "$RUNNER_DIR"

  if [ ! -f ./config.sh ]; then
    echo "Tải actions-runner..."
    arch="x64"
    [ "$(uname -m)" = "aarch64" ] && arch="arm64"
    ver="2.323.0"
    curl -fsSL -o actions-runner.tar.gz \
      "https://github.com/actions/runner/releases/download/v${ver}/actions-runner-linux-${arch}-${ver}.tar.gz"
    tar xzf actions-runner.tar.gz
    rm -f actions-runner.tar.gz
  fi

  ./config.sh --url "$cfg_url" --token "$cfg_token" --name "stayhub-$(hostname -s)" --labels self-hosted,Linux,X64 --unattended

  echo "Cài service (chạy nền, tự khởi động khi boot)..."
  sudo ./svc.sh install
  sudo ./svc.sh start
  sudo ./svc.sh status || true

  echo
  echo "Runner OK. Push lên nhánh main → workflow Self-hosted deploy chạy tự động."
}

install_timer() {
  echo
  echo "=== systemd user timer (poll 5 phút) ==="
  UNIT_DIR="${HOME}/.config/systemd/user"
  mkdir -p "$UNIT_DIR"

  cat > "$UNIT_DIR/stayhub-deploy.service" <<EOF
[Unit]
Description=StayHub local auto deploy

[Service]
Type=oneshot
Environment=GIT_BRANCH=main
ExecStart=${BACKEND_DIR}/docker/scripts/local-auto-deploy.sh
WorkingDirectory=${BACKEND_DIR}
EOF

  cat > "$UNIT_DIR/stayhub-deploy.timer" <<'EOF'
[Unit]
Description=StayHub deploy poll every 5 minutes

[Timer]
OnBootSec=2min
OnUnitActiveSec=5min
AccuracySec=1min

[Install]
WantedBy=timers.target
EOF

  systemctl --user daemon-reload
  systemctl --user enable --now stayhub-deploy.timer
  systemctl --user list-timers stayhub-deploy.timer

  echo
  echo "Timer OK. Log: journalctl --user -u stayhub-deploy.service -f"
  echo "Lưu ý: cần 'loginctl enable-linger $USER_NAME' để timer chạy khi không đăng nhập."
  read -r -p "Bật linger cho user này? [y/N]: " linger
  if [ "${linger,,}" = "y" ]; then
    sudo loginctl enable-linger "$USER_NAME"
  fi
}

case "$choice" in
  1) install_runner ;;
  2) install_timer ;;
  3) install_runner; install_timer ;;
  *) echo "Chọn không hợp lệ." >&2; exit 1 ;;
esac

echo
echo "Thử deploy thủ công:"
echo "  bash $BACKEND_DIR/docker/scripts/local-auto-deploy.sh"
