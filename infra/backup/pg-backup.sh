#!/usr/bin/env bash
# Nightly Postgres backup for the compose stack (Step 10.1).
#
# Produces, in $BACKUP_DIR (default /backups):
#   - globals-<ts>.sql.gz ............ roles/tablespaces (pg_dumpall --globals-only)
#   - <db>-<ts>.dump.gz ............... per-database custom-format dump (6 DBs)
#   - latest-<db>.dump.gz (symlinks) .. stable names for the restore drill
#   - SHA256SUMS ....................... integrity manifest for every artifact
#
# Runs inside the `backup` compose service (postgres:16-alpine image, so
# pg_dump matches the server major version) or anywhere with network access
# to the database host. Retention pruning keeps the newest $KEEP_DAILY
# dailies; off-host copies (S3) are the operator's second tier (see
# docs/runbook.md "Backups").
set -euo pipefail

: "${PGHOST:=postgres}"
: "${PGPORT:=5432}"
: "${PGUSER:=paymentswitch}"
: "${BACKUP_DIR:=/backups}"
: "${KEEP_DAILY:=14}"

DATABASES="IdentityDb MerchantDb PaymentDb LedgerDb NotificationDb SettlementDb"

TS="$(date +%F_%H%M)"
mkdir -p "$BACKUP_DIR"
cd "$BACKUP_DIR"

echo "-- dumping globals (roles) ..."
pg_dumpall --globals-only --no-role-passwords -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" \
  | gzip > "globals-$TS.sql.gz"

for db in $DATABASES; do
  echo "-- dumping $db ..."
  pg_dump -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -F c -Z 9 -f "$db-$TS.dump" "$db"
  gzip -f "$db-$TS.dump"
  ln -sf "$db-$TS.dump.gz" "latest-$db.dump.gz"
done

echo "-- pruning to newest $KEEP_DAILY dailies per database ..."
# The just-created latest-* symlinks are always newest, so they never fall
# into the deletion window.
for db in $DATABASES globals; do
  # shellcheck disable=SC2012
  ls -1t "$db"-*.gz 2>/dev/null | tail -n +"$((KEEP_DAILY + 1))" | xargs -r rm -f
done

sha256sum globals-*.sql.gz IdentityDb-*.dump.gz MerchantDb-*.dump.gz \
  PaymentDb-*.dump.gz LedgerDb-*.dump.gz NotificationDb-*.dump.gz \
  SettlementDb-*.dump.gz > SHA256SUMS 2>/dev/null || true

echo "BACKUP OK: $(ls -1 *-"$TS".gz | wc -l) artifacts in $BACKUP_DIR"
