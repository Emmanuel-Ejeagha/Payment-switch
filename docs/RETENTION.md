# Data Retention & Archival Policy

This document defines how the PaymentSwitch services retain and archive data.
The governing rule: **financial and messaging records are never hard-deleted —
they are relocated to an archive table** (`ArchivedRecords`) as JSON snapshots,
then removed from the live tables.

## Retention table

| Dataset | Service | Live retention | Regulatory basis |
|---|---|---|---|
| Payment intents + transaction history | Payment | 7 years | PCI DSS §3 / card-scheme recordkeeping (usually 6–7 years); payment records must survive chargeback and audit windows |
| Journal entries (double-entry ledger) | Ledger | 7 years | Financial/accounting record retention; statutory books of account |
| Notifications | Notification | 90 days | Operational delivery history; GDPR-style data minimization |
| Inbox / outbox messaging rows | Payment, Ledger, Notification | 7 days | Transient messaging; matches the previous 7-day inbox cleanup |
| Dead-letter records | Ledger, Notification | retained (no expiry) | Failure forensics; small volume, purged manually |

Retention is counted from the record's terminal timestamp: `CreatedAt` for
payment intents, `Timestamp` for journal entries, `ProcessedAt` for delivered
notifications, `ProcessedAt`/`OccurredOn` for inbox/outbox rows.

## How archival works

Each service runs a background `RetentionCleanupService` (a
`RetentionCleanupServiceBase<TOptions>` loop) that, on an interval, moves the
oldest expired rows into the `ArchivedRecords` table and deletes them from the
live table. Copy and delete happen in one transaction, so a crash mid-batch
cannot lose data from both sides. Rows are moved oldest-first in configurable
batches.

### The archive row

Every archived record is a JSON snapshot of the full original row:

```json
{
  "id": "…",
  "sourceTable": "PaymentIntents",
  "payload": "{ ... full row incl. owned children ... }",
  "archivedAt": "2026-08-19T…Z"
}
```

Because the snapshot stores the whole record (payment intents include their
owned `Transactions`; the original `Id` is preserved inside the payload), a
record can always be restored to its live table — even if the live schema
evolves later. The archive is therefore the audit trail that satisfies the
regulatory timeline.

## Configuration

The sweep is configured per service under the `Retention` section
(`appsettings.json` / env vars `Retention__*`):

```json
"Retention": {
  "CleanupIntervalMinutes": 60,
  "BatchSize": 100,
  "MessageRetentionDays": 7,
  "BusinessRetentionDays": 2557   // 90 for Notification
}
```

| Key | Default | Meaning |
|---|---|---|
| `CleanupIntervalMinutes` | 60 | How often the sweep runs |
| `BatchSize` | 100 | Rows moved per batch (oldest first) |
| `MessageRetentionDays` | 7 | Inbox/outbox live retention |
| `BusinessRetentionDays` | 2557 (Payment/Ledger), 90 (Notification) | Business-record live retention |

## Operational notes

- **Restore**: deserialize an `ArchivedRecord.Payload` back into the entity and
  insert into the live table under the original `Id`.
- **Query**: the archive is queryable by `sourceTable` and `archivedAt`; the
  payload is `jsonb` so it is also searchable with Postgres JSON operators.
- **Sensitivity**: archive rows inherit the data classification of their source
  (e.g. `CardDetails` is limited to last-four + gateway token, never a full
  PAN). The archive lives in the same database and is protected identically.
- **Backup**: `ArchivedRecords` is covered by the normal database backups
  (see `runbook.md`); archival does not replace backups.
- **Disabled services**: if an archival sweep is turned off, rows simply stay in
  the live tables; no data is lost.