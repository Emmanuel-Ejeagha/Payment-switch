# Production Readiness Review — Findings & Fixes (2026-09-11)

Senior-engineer audit of the full stack against `TODO.md` (Phases 0–9).
Scope: verify build/test gates, close real gaps, document what cannot be
verified locally. Unrelated in-progress working-tree files were left alone
per `AGENTS.md`.

## Verification results (this session)

| Gate | Command | Result |
|---|---|---|
| Backend build | `dotnet build PaymentSwitch.slnx -c Release` | 0 errors, 40 warnings (pre-existing) |
| Unit tests (17 projects) | `dotnet test tests/Unit/* -c Release` | **901/901 pass** |
| Integration tests (7 suites) | Testcontainers (Postgres, RabbitMQ) | **Blocked — Docker daemon down** (see BUG-4) |
| Frontend typecheck | `npm run typecheck --workspace={merchant,admin}` | 0 errors / 0 errors |
| Frontend lint | `npm run lint --workspace={merchant,admin}` | clean / 0 errors, 2 pre-existing warnings |
| Frontend unit | `npx vitest run` (merchant, admin) | 14/14, 7/7 |
| Frontend build | `npm run build --workspace={merchant,admin}` | both pass |
| Format gate | `dotnet format --verify-no-changes` | red on 2 **uncommitted** files only (see BUG-3) |

## Bug list

### BUG-1 — CI frontend job never runs frontend tests (Phase 9 exit gap) — FIXED
- **Finding:** `.github/workflows/ci-cd.yml` job `frontend` runs typecheck, lint,
  `npm audit`, and `next build`, but never runs Vitest. Phase 9 exit criterion
  "Frontend smoke tests run in CI" was unchecked and genuinely unmet.
- **Fix:** added `Test admin` / `Test merchant` steps
  (`npm run test --workspace={admin,merchant}` → `vitest run`) after lint,
  before build. Both suites are green locally (14/14, 7/7).
- **Files:** `.github/workflows/ci-cd.yml` (committed this step).

### BUG-2 — Stale TODO.md tracking contradicts completed work — FIXED (local, gitignored)
- **Findings:**
  1. Phase Progress Tracker marked Phase 5 "In progress" although all 6 Phase 5
     steps and all 4 exit criteria were ticked → tracker now `[x] Complete`.
  2. Phase 9 exit criteria unchecked although the underlying steps were done and
     verified present (retention code + `AddArchivedRecords` migration +
     `docs/RETENTION.md`; k6 `tests/Load/`; `.github/CODEOWNERS`; Vitest suites
     now gated in CI via BUG-1) → both exit boxes ticked.
  3. Legacy checklist sections still showed `[ ]` for items whose TASK sections
     are Complete with evidence; each was spot-verified before ticking:
     TASK-012 merchant prefs UI, TASK-040 mandatory Idempotency-Key, TASK-031
     jti/rotation, TASK-032 CORS, TASK-028 trace propagation
     (`BuildingBlocks.Shared/Messaging/RabbitMqTracing.cs` + consumers),
     TASK-033 indexes, TASK-021 frontend deploy, TASK-019 k8s manifests
     (spot-verified `k8s/identity-deployment.yaml`: resources/securityContext/
     startupProbe), TASK-026 pooling, TASK-029 Grafana provisioning
     (`infra/grafana/provisioning/`), TASK-022 broker/E2E tests, TASK-048
     frontend tests, TASK-020 scanning (dependabot + Trivy + `npm audit`) and
     env separation (`environment: production`), TASK-042 retention, TASK-035
     rotation (`docs/secrets.md` + `infra/secrets/rotate-jwt.sh`), TASK-030
     README, TASK-036 webhook contract, deployment/TLS/runbook docs, TASK-024
     acceptance boxes.
- **Genuinely remaining `[ ]` items (not fixed — real P2/P3 gaps, see Verdict):**
  API-key scopes, payout disbursement/retry, contract tests, admin-form
  zod coverage, Postgres backups, k8s live-cluster validation (deferred to
  staging by design).
- **Files:** `TODO.md` (gitignored — local only, never committed).

### BUG-3 — `dotnet format` red on working tree (line-ending artifact, NOT committed code)
- **Finding:** `--verify-no-changes` fails on exactly 2 files, both with
  uncommitted working-tree changes whose diff is empty under
  `--ignore-cr-at-eol` (pure LF-vs-CRLF artifact):
  `Payment.Application/.../CheckoutPaymentCommand.cs`,
  `Payment.Application/.../CreatePaymentIntentCommand.cs`.
- **Fix:** none applied (unrelated in-progress work — left alone per
  `AGENTS.md`). Owner of that work: normalize line endings before staging
  (then re-run `dotnet format --verify-no-changes`).

### BUG-4 — Docker daemon down: integration/E2E not re-verified this session
- **Finding:** `docker ps` fails (`dockerDesktopLinuxEngine` pipe missing), so
  the 7 Testcontainers suites (last known 75/75) and Playwright E2E could not
  be re-run. Unit + frontend gates above are green.
- **Fix (user action):** start Docker Desktop, wait for `docker ps`, then run
  integration projects **one at a time**:
  `dotnet test tests/Integration/<Suite> -c Release`, plus
  `npm run test:e2e` per `docs/frontend-tests.md`.

### BUG-5 — Merchant payments list silently swallowed load failures — FIXED
- **Finding:** `apps/merchant/src/app/(dashboard)/payments/page.tsx` load effect
  had `catch { /* ignore */ }` and only set state on `res.ok`, so a failed
  fetch rendered the misleading "No payments yet" empty state (TODO.md
  checklist item, P2).
- **Fix (ledger-page convention):** added a `loadError` state rendered via
  `<ErrorPanel title="Failed to load payments" .../>` when the list is empty;
  non-ok statuses include the status code; error clears on retry.
- **Tests:** new `apps/merchant/src/__tests__/payments-page.test.tsx` (3 tests:
  network failure → error panel, HTTP 500 → error panel with status, empty
  success → empty state, no alert). Merchant suite now 7 files / 17 tests,
  typecheck 0 errors, lint clean.
- **Files:** `apps/merchant/src/app/(dashboard)/payments/page.tsx`,
  `apps/merchant/src/__tests__/payments-page.test.tsx` (committed this step).

## Accepted / deferred (documented in TODO.md, not blockers for this step)
- Phase 7 exit: `kubectl apply -f k8s/` live-cluster validation deferred to a
  staging cluster (k8s is the target platform, not the live runtime).
- Branch-protection **repo setting** (require checks + approval) must be
  flipped in GitHub UI; code side (CODEOWNERS, required-check names) is in
  place. `gh` CLI is not installed here.
- Playwright E2E stays out of CI by design (needs browsers + live stack);
  Vitest component tests now gate CI via BUG-1.

## Verdict
Code gates are green (build 0 errors, 901/901 unit, frontend
typecheck/lint/test/build clean) and the closable gaps are fixed in this
step: CI now gates on Vitest (BUG-1), TODO tracking matches verified reality
(BUG-2), and the payments silent-catch is fixed with regression tests (BUG-5).
Honest remaining gaps (all P2/P3 or environmental, none blocking the happy
path): API-key scopes, payout disbursement/retry, contract tests, full
admin-form zod coverage, Postgres backups, k8s live-cluster validation, and
the GitHub branch-protection repo setting. Environmental prerequisites before
calling it done: start Docker + re-run the 7 integration suites one at a
time, provision TLS certs/DNS per `docs/tls.md` and `docs/runbook.md` at
deploy time.
