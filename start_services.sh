#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR/StayHub"

stop_existing_services() {
  echo "Stopping existing services..."
  # Ép buộc tắt sạch toàn bộ tiến trình chứa từ khóa StayHub và vite
  pkill -9 -f "StayHub" 2>/dev/null || true
  pkill -9 -f "vite" 2>/dev/null || true
}

wait_for_port_release() {
  local port="$1"
  for _ in $(seq 1 15); do
    if ! ss -ltn 2>/dev/null | grep -q ":$port "; then
      return 0
    fi
    sleep 1
  done
  echo "Port $port is still busy after waiting." >&2
  return 1
}

check_service_status() {
  local log_file="$1"
  local name="$2"
  if grep -Eq "Hosting failed to start|Unhandled exception|Address already in use|Exception" "$log_file" 2>/dev/null; then
    echo "$name failed to start. See $log_file" >&2
    return 1
  fi
  return 0
}

stop_existing_services

for port in 5046 5050 5069 5072 5087 5091 5141 5218 5298 7001 7002 7003 7004 7005 7006 7007 7008 7009 7010; do
  wait_for_port_release "$port" || true
done

sleep 2

echo "Building Backend Microservices..."
dotnet build StayHub.sln

echo "Starting Backend Microservices..."
# Khởi động các API với launch profile https để chạy đúng cổng cấu hình của dự án
nohup dotnet run --no-build --project GatewayAPI/GatewayAPI.csproj --no-launch-profile --urls "http://0.0.0.0:5046;https://0.0.0.0:7010" > /tmp/gatewayapi.log 2>&1 &
nohup dotnet run --no-build --project AIAPI/AIAPI.csproj --launch-profile "https" > /tmp/aiapi.log 2>&1 &
nohup dotnet run --no-build --project AuthAPI/AuthAPI.csproj --launch-profile "https" > /tmp/authapi.log 2>&1 &
nohup dotnet run --no-build --project BookingAPI/BookingAPI.csproj --launch-profile "https" > /tmp/bookingapi.log 2>&1 &
nohup dotnet run --no-build --project ContentAPI/ContentAPI.csproj --launch-profile "https" > /tmp/contentapi.log 2>&1 &
nohup dotnet run --no-build --project PaymentAPI/PaymentAPI.csproj --launch-profile "https" > /tmp/paymentapi.log 2>&1 &
nohup dotnet run --no-build --project SocialAPI/SocialAPI.csproj --launch-profile "https" > /tmp/socialapi.log 2>&1 &
nohup dotnet run --no-build --project SystemAPI/SystemAPI.csproj --launch-profile "https" > /tmp/systemapi.log 2>&1 &
nohup dotnet run --no-build --project TourAPI/TourAPI.csproj --launch-profile "https" > /tmp/tourapi.log 2>&1 &
nohup dotnet run --no-build --project VoucherAPI/VoucherAPI.csproj --launch-profile "https" > /tmp/voucherapi.log 2>&1 &

sleep 5

for pair in "GatewayAPI:/tmp/gatewayapi.log" "AIAPI:/tmp/aiapi.log" "AuthAPI:/tmp/authapi.log" "BookingAPI:/tmp/bookingapi.log" "ContentAPI:/tmp/contentapi.log" "PaymentAPI:/tmp/paymentapi.log" "SocialAPI:/tmp/socialapi.log" "SystemAPI:/tmp/systemapi.log" "TourAPI:/tmp/tourapi.log" "VoucherAPI:/tmp/voucherapi.log"; do
  name="${pair%%:*}"
  log_file="${pair##*:}"
  if ! check_service_status "$log_file" "$name"; then
    echo "Startup check failed for $name" >&2
    exit 1
  fi
done

echo "Services started successfully in background!"