#!/usr/bin/env bash
set -euo pipefail

DOTNET="/home/kiuthi/.dotnet/dotnet"
ROOT="/home/kiuthi/Projects/Backend/SEP490_08-Backend/StayHub"
LOGS_DIR="$ROOT/logs"

mkdir -p "$LOGS_DIR"

services=(
  "AuthAPI"
  "BookingAPI"
  "PaymentAPI"
  "SocialAPI"
  "TourAPI"
  "VoucherAPI"
  "AIAPI"
  "ContentAPI"
  "SystemAPI"
  "GatewayAPI"
)

echo "Checking Docker containers (SQL Server & Redis)..."
if ! docker ps | grep -q "sqlserver"; then
  echo "WARNING: sqlserver container is not running!"
fi
if ! docker ps | grep -q "redis"; then
  echo "WARNING: redis container is not running!"
fi

echo "Starting StayHub backend microservices..."
for service in "${services[@]}"; do
  echo "Launching $service..."
  nohup "$DOTNET" run --project "$ROOT/$service/$service.csproj" --launch-profile https > "$LOGS_DIR/$service.log" 2>&1 &
  # Small delay to prevent resource contention
  sleep 2
done

echo "--------------------------------------------------"
echo "All 10 services have been launched in the background!"
echo "You can check the logs in:"
echo "  $LOGS_DIR/"
echo "--------------------------------------------------"
