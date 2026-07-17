#!/bin/bash
PASSWORD="admin"

echo "Copying files to container..."
docker exec sqlserver mkdir -p /tmp/db
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

echo "Executing Schema..."
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$PASSWORD" -C -i /tmp/db/StayHub_DatabaseSchema.sql

FILES=("StayHub_SystemDb_Data.sql" "StayHub_IdentityDb_Data.sql" "StayHub_ContentDb_Data.sql" "StayHub_CatalogDb_Data.sql" "StayHub_VoucherDb_Data.sql" "StayHub_PaymentDb_Data.sql" "StayHub_SocialDb_Data.sql" "StayHub_BookingDb_Data.sql" "StayHub_AiDb_Data.sql")

for f in "${FILES[@]}"; do
  echo "Executing $f..."
  docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$PASSWORD" -C -i /tmp/db/$f
done

echo "Done running DB!"
