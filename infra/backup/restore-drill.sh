#!/usr/bin/env bash
# Restore drill for the compose Postgres backups (Step 10.1).
#
# Proves the backups are actually restorable — a backup never tested is a
# hope, not a control. Spins a SCRATCH postgres container (never touches the
# live data volume), restores globals + every latest-*.dump.gz into it, then
# asserts each database opens and its core tables exist and are countable.
#
# Usage (on the EC2 host, backups visible at ./volumes/pgbackups or a copy):
#   bash infra/backup/restore-drill.sh [/path/to/backups] [scratch-port]
#
# Exit 0 = drill passed; non-zero = investigate before you need it for real.
set -euo pipefail

BACKUP_DIR="${1:-./pgbackups}"
SCRATCH_PORT="${2:-5544}"
SCRATCH_NAME="paymentswitch-restore-drill"
DATABASES="IdentityDb MerchantDb PaymentDb LedgerDb NotificationDb SettlementDb"

# Core tables that must exist per database after a faithful restore.
check_table() { # check_table <db> <table>
  local count
  count="$(docker exec "$SCRATCH_NAME" psql -U paymentswitch -d "$1" -tAc "SELECT count(*) FROM \"$2\"" 2>&1)" || return 1
  [[ "$count" =~ ^[0-9]+$ ]]
}

cleanup() {
  docker rm -f "$SCRATCH_NAME" >/dev/null 2>&1 || true
}
trap cleanup EXIT

echo "-- verifying backup artifacts in $BACKUP_DIR ..."
for db in $DATABASES; do
  [[ -f "$BACKUP_DIR/latest-$db.dump.gz" ]] || { echo "FAIL missing latest-$db.dump.gz"; exit 1; }
done
if [[ -f "$BACKUP_DIR/SHA256SUMS" ]]; then
  (cd "$BACKUP_DIR" && sha256sum -c SHA256SUMS --quiet) || { echo "FAIL checksum manifest"; exit 1; }
  echo "-- checksums OK"
else
  echo "-- no SHA256SUMS manifest; skipping integrity check"
fi

echo "-- starting scratch postgres on 127.0.0.1:$SCRATCH_PORT ..."
docker run -d --rm --name "$SCRATCH_NAME" \
  -e POSTGRES_USER=paymentswitch -e POSTGRES_PASSWORD=drill \
  -p "127.0.0.1:$SCRATCH_PORT:5432" \
  postgres:16-alpine >/dev/null

echo "-- waiting for scratch postgres ..."
for _ in $(seq 1 30); do
  docker exec "$SCRATCH_NAME" pg_isready -U paymentswitch >/dev/null 2>&1 && break
  sleep 2
done
docker exec "$SCRATCH_NAME" pg_isready -U paymentswitch >/dev/null \
  || { echo "FAIL scratch postgres never became ready"; exit 1; }

echo "-- restoring ..."
gunzip -c "$BACKUP_DIR"/globals-*.sql.gz | head -n 50 | grep -q "PostgreSQL database cluster dump" \
  || echo "-- (no globals dump found; continuing with databases only)"
for f in "$BACKUP_DIR"/globals-*.sql.gz; do
  [ -e "$f" ] || continue
  gunzip -c "$f" | docker exec -i "$SCRATCH_NAME" psql -U paymentswitch -d postgres -q -v ON_ERROR_STOP=1 >/dev/null
done
for db in $DATABASES; do
  docker exec "$SCRATCH_NAME" psql -U paymentswitch -d postgres -q -c "CREATE DATABASE \"$db\";" >/dev/null
  gunzip -c "$BACKUP_DIR/latest-$db.dump.gz" \
    | docker exec -i "$SCRATCH_NAME" pg_restore -U paymentswitch -d "$db" --no-owner -q
  echo "-- restored $db"
done

echo "-- verifying core tables ..."
check_table IdentityDb Users
check_table MerchantDb Merchants
check_table PaymentDb PaymentIntents
check_table LedgerDb JournalEntries
check_table NotificationDb Notifications
check_table SettlementDb SettlementBatches
echo "-- all core tables countable"

echo "DRILL PASSED: all $(( $(echo "$DATABASES" | wc -w) )) databases restored and verified"
