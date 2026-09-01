#!/usr/bin/env bash
# Per-boot reconciliation: start PostgreSQL and ensure the busca role,
# database and seed data exist. Safe to run repeatedly.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "==> Starting PostgreSQL 16 cluster"
if ! sudo pg_lsclusters -h 2>/dev/null | awk '{print $4}' | grep -q online; then
  sudo pg_ctlcluster 16 main start || true
fi

echo "==> Waiting for PostgreSQL to accept connections"
for _ in $(seq 1 30); do
  if sudo -u postgres pg_isready -q; then
    break
  fi
  sleep 1
done

echo "==> Ensuring role and database 'busca'"
if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='busca'" | grep -q 1; then
  sudo -u postgres psql -c "CREATE ROLE busca LOGIN PASSWORD 'busca';"
fi
if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='busca'" | grep -q 1; then
  sudo -u postgres createdb -O busca busca
fi

echo "==> Applying schema and seed if the database is empty"
if [ "$(PGPASSWORD=busca psql -h localhost -U busca -d busca -tAc "SELECT to_regclass('public.conta')")" != "conta" ]; then
  PGPASSWORD=busca psql -h localhost -U busca -d busca -v ON_ERROR_STOP=1 -f db/01_schema.sql
  PGPASSWORD=busca psql -h localhost -U busca -d busca -v ON_ERROR_STOP=1 -f db/02_seed.sql
fi

echo "==> start.sh completed"
