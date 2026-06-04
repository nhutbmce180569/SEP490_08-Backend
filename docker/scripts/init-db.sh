#!/usr/bin/env bash
set -euo pipefail

SQL_HOST="${SQL_HOST:-sqlserver}"
SA_PASSWORD="${SA_PASSWORD:?SA_PASSWORD is required}"
LOAD_SAMPLE_DATA="${LOAD_SAMPLE_DATA:-true}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

echo "Waiting for SQL Server at ${SQL_HOST}..."
for i in $(seq 1 60); do
  if ${SQLCMD} -S "${SQL_HOST}" -U sa -P "${SA_PASSWORD}" -Q "SELECT 1" -C -b >/dev/null 2>&1; then
    echo "SQL Server is ready."
    break
  fi
  if [ "$i" -eq 60 ]; then
    echo "SQL Server did not become ready in time." >&2
    exit 1
  fi
  sleep 2
done

DB_EXISTS=$(${SQLCMD} -S "${SQL_HOST}" -U sa -P "${SA_PASSWORD}" -C -h -1 -Q "SET NOCOUNT ON; SELECT DB_ID('StayHub_IdentityDb')" | tr -d '[:space:]')

if [ -n "${DB_EXISTS}" ] && [ "${DB_EXISTS}" != "NULL" ]; then
  echo "StayHub databases already initialized — skipping schema and sample data."
  exit 0
fi

echo "Applying StayHub_DatabaseSchema.sql..."
${SQLCMD} -S "${SQL_HOST}" -U sa -P "${SA_PASSWORD}" -C -b -i /scripts/schema.sql

if [ "${LOAD_SAMPLE_DATA}" = "true" ]; then
  echo "Applying StayHub_SampleData_Insert.sql (this may take a few minutes)..."
  ${SQLCMD} -S "${SQL_HOST}" -U sa -P "${SA_PASSWORD}" -C -b -i /scripts/sample.sql
else
  echo "Skipping sample data (LOAD_SAMPLE_DATA=${LOAD_SAMPLE_DATA})."
fi

echo "Database initialization complete."
