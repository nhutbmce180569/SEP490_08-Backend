#!/bin/bash
PASSWORD="StayHub@Dev12345!"

echo "=========================================================="
echo " StayHub Docker Database Initializer"
echo "=========================================================="

echo "Creating temp directory inside SQL Server container..."
docker exec sqlserver mkdir -p /tmp/db

echo "Copying SQL files to SQL Server container..."
docker cp StayHub_DatabaseSchema.sql sqlserver:/tmp/db/
docker cp StayHub_SystemDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_IdentityDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_ContentDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_CatalogDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_VoucherDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_PaymentDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_SocialDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_BookingDb_Data.sql sqlserver:/tmp/db/
docker cp StayHub_AiDb_Data.sql sqlserver:/tmp/db/

# Also copy sample data insert in case user wants to run it manually later
if [ -f StayHub_SampleData_Insert.sql ]; then
  docker cp StayHub_SampleData_Insert.sql sqlserver:/tmp/db/
fi

echo "Waiting for SQL Server to be ready..."
for i in {1..30}; do
  if docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$PASSWORD" -C -Q "SELECT 1" &>/dev/null; then
    echo "SQL Server is ready!"
    break
  fi
  echo -n "."
  sleep 2
done
echo ""

echo "Executing Database Schema..."
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$PASSWORD" -C -i /tmp/db/StayHub_DatabaseSchema.sql

FILES=(
  "StayHub_SystemDb_Data.sql"
  "StayHub_IdentityDb_Data.sql"
  "StayHub_ContentDb_Data.sql"
  "StayHub_CatalogDb_Data.sql"
  "StayHub_VoucherDb_Data.sql"
  "StayHub_PaymentDb_Data.sql"
  "StayHub_SocialDb_Data.sql"
  "StayHub_BookingDb_Data.sql"
  "StayHub_AiDb_Data.sql"
)

for f in "${FILES[@]}"; do
  echo "Executing data import for $f..."
  docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$PASSWORD" -C -i /tmp/db/$f
done

echo "=========================================================="
echo " Database recovery completed successfully!"
echo "=========================================================="
