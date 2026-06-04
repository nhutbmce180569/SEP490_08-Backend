#!/usr/bin/env bash
# Backup all StayHub SQL databases to ./backups/
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

# shellcheck disable=SC1091
source .env

BACKUP_DIR="$ROOT_DIR/backups/$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"

echo "Backing up to $BACKUP_DIR ..."

databases=(
  StayHub_IdentityDb StayHub_CatalogDb StayHub_BookingDb StayHub_PaymentDb
  StayHub_VoucherDb StayHub_SocialDb StayHub_AiDb StayHub_ContentDb StayHub_SystemDb
)

for db in "${databases[@]}"; do
  echo "  -> $db"
  docker compose exec -T sqlserver \
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$DB_PASSWORD" -C -Q \
    "BACKUP DATABASE [$db] TO DISK = N'/var/opt/mssql/data/${db}.bak' WITH FORMAT, INIT" -b

  docker cp "stayhub-sqlserver:/var/opt/mssql/data/${db}.bak" "$BACKUP_DIR/${db}.bak"
done

echo "Backup complete: $BACKUP_DIR"
