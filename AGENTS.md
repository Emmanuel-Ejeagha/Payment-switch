# AGENTS.md

Working conventions for this repository. Read before making changes.

## Repo at a glance

Six Clean Architecture .NET microservices (`src/Services/{Identity,Merchant,Payment,Ledger,Notification,Settlement}`)
over a shared kernel (`src/BuildingBlocks`), an event-driven backbone
(transactional outbox → RabbitMQ → inbox → DLX/DLQ), two Next.js frontends
(`apps/merchant`, `apps/admin`), and Testcontainers-based integration tests.

The authoritative work list is `TODO.md`. It contains an audit and a
dependency-ordered **Phased Implementation Plan** (Phase 0–9) with concrete
steps and exit criteria. **Do not start a phase before its exit criteria are
green.**

## Workflow

- Implement `TODO.md` step by step, in phase order. A step is done only when its
  exit criteria hold.
- Commit after each step with a short, imperative message (repo style:
  e.g. `Fix gateway failover: ...`, `Add RabbitMQ Testcontainers + ...`).
  Push to `origin` when the user asks.
- Only stage the files that belong to the step. This repo often has unrelated
  in-progress work in the working tree (`apps/merchant`, etc.) — leave it alone.
- Tick `TODO.md` checkboxes and the Phase Progress Tracker as you go.

## The testing convention (mandatory)

**Every fix ships with a test.** Never deferred to the end.

- **Unit tests** for pure logic (domain rules, validators, handlers, value
  objects): add to `tests/Unit/<Project>.Tests/`.
- **Integration tests** for broker/DB paths (outbox/inbox, consumers, EF
  persistence, container-backed flows): add to `tests/Integration/`.
- If a fix is a bug, the test should reproduce the bug first (or be added with
  the fix) and fail before the change, pass after.

## Build & test commands

```bash
# Build the whole solution
dotnet build PaymentSwitch.slnx -c Release

# Unit tests (one project at a time)
dotnet test tests/Unit/Payment.Application.Tests/Payment.Application.Tests.csproj -c Release
# ...repeat for each tests/Unit/<Project>.Tests/ project

# Integration tests (require Docker Desktop running)
dotnet test tests/Integration/Ledger.API.IntegrationTests/Ledger.API.IntegrationTests.csproj -c Release
```

Notes:

- Integration tests use Testcontainers (Postgres, RabbitMQ). **Docker must be
  running.** If the engine is down, start Docker Desktop and wait for `docker ps`
  to succeed.
- Run integration projects one at a time and sequentially. Concurrent Docker
  stacks cause flaky connection failures; test parallelization is disabled per
  project (`tests/Integration/Shared/AssemblyInfo.cs`).
- RabbitMQ test configuration lives in
  `tests/Integration/Shared/TestSecrets.cs`; the API host reads `RabbitMQ__*`
  environment variables (with a configurable `RabbitMQ__Port`).

## Tech stack gotchas

- The ledger/bus layer reads a configured `RabbitMQ:ExchangeName`. In tests the
  `payment.events` exchange must be declared before the API host starts or the
  consumer's first bind attempt fails.
- Money is integer minor-units (`Money` value object); do not introduce floats.
- Postgres uses `xmin` optimistic concurrency for ledger accounts.
