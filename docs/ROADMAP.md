# PaymentSwitch Roadmap

> Production-grade payment switch (Paystack/Stripe replica) — feature & hardening backlog.
> Status legend: `[ ]` pending · `[x]` completed

## Working order

Phase A → B → C are the non-negotiable foundation (each item is independently committable/pushable). Phases D–F add Paystack/Stripe feature parity. Phases G–H polish for deployment.

---

## Phase A — Security hardening (blockers, highest priority)

- [x] **A1. Merchant ownership enforcement** — link merchant↔user at onboarding; scope all merchant/config/API-key endpoints to the caller's claims (or Admin). Currently any authenticated user can act on any merchant (Merchant controllers never read claims).
- [x] **A2. Secure gRPC** — TLS + JWT/mTLS auth on Merchant/Ledger gRPC servers; internal-only binding for `ResolveApiKey` (currently plaintext HTTP/2, anonymous).
- [x] **A3. Enforce rate limiting** — `[EnableRateLimiting("Strict")]` on login/register/onboard/API-key; `Default` elsewhere. Policies are registered but never applied.
- [x] **A4. Remove committed secrets** — JWT secret, DB passwords, RabbitMQ guest/guest, `Admin123!` seeder; fail-fast startup when unset.
- [x] **A5. Secure SignalR hub** — `[Authorize]` + derive merchant/admin groups from claims, not query strings (anyone can currently join a merchant/admin group).
- [x] **A6. Harden refresh tokens** — hash at rest, reuse/family detection, restrict anonymous `/auth/revoke`.
- [x] **A7. Harden API keys** — KDF (bcrypt) instead of unsalted SHA-256; prefix Identity keys (`sk_live_`/`sk_test_`); require Active merchant before key generation.
- [x] **A8. Stop logging PII** — remove emails at Information level; mask sensitive fields.

## Phase B — Financial correctness

- [x] **B9. Minor-unit money** — replace `decimal` with `long` minor-units (kobo/pence) across Payment/Ledger/Settlement; consistent semantics (currently inconsistent `0`/`>0` rules).
- [x] **B10. Full double-entry GL** — GL accounts (merchant liability, settlement, fees income), balanced debit+credit legs per movement, true reservation (fix dead `ReservedBalance`; current entries are single-sided).
- [x] **B11. Optimistic concurrency** — rowversion/`xmin` on `PaymentIntent`, `LedgerAccount`, `SettlementBatch`; explicit transaction scope on financial ops (double-capture/over-refund risk today).
- [x] **B12. Idempotency overhaul** — per-merchant unique key (fix global-unique index bug); return existing intent on 409; add idempotency to authorize/capture/void/refund.
- [x] **B13. Remove GET side-effect** — `GetBalance` must not auto-create a NGN account.
- [x] **B14. MessageId + correlation** — set `MessageId` on every RabbitMQ publish (inbox dedup is currently broken); propagate correlation IDs for sagas.

## Phase C — Messaging reliability

- [x] **C15. DLQ + retry limits** — RabbitMQ DLX + dead-letter queue, consumer retry-then-DLQ (currently `Nack(requeue:false)` = silent message loss), inbox cleanup policy.
- [x] **C16. Outbox concurrency lease** — prevent double-publish across replicas; add index on `OutboxMessages.Processed`.
- [x] **C17. Resilient consumers** — port Notification's reconnect-loop pattern to all services; start without RabbitMQ (Ledger currently binds in constructor → host fails).

## Phase D — Payment core parity

- [x] **D18. Gateway abstraction + routing** — provider registry, routing rules, simulated declines/latency, fallback, per-provider circuit breaker (currently a single always-success mock).
- [x] **D19. Card tokenization** — `card_` tokens via PCI-safe flow + token vault; keep last4/brand only for display.
- [x] **D20. Hosted Checkout page** — public route + payment links.
- [x] **D21. 3DS/SCA states** — add `RequiresAction`/`Processing` states + async confirmation events.
- [x] **D22. Real webhooks** — HMAC signatures (per-merchant secret), retry with backoff, event log + replay endpoint, "send test event" API (WebhookUrl is currently fetched but never used).
- [x] **D23. Customers** — entity + CRUD API.
- [x] **D24. Subscriptions** — plan → subscription → invoice → recurring charge.
- [ ] **D25. Disputes/chargebacks** — raise, evidence, respond, ledger reversal.
- [ ] **D26. Reconciliation jobs** — gateway↔internal tie-out; ledger balance↔journal verification.

## Phase E — Settlement execution

- [ ] **E27. Real batch lifecycle** — Pending→Processing→Completed/Failed; per-payout status (batch is currently created and immediately Completed).
- [ ] **E28. Payout execution** — bank adapter (mock), payout file/CSV export, unique index on `BatchDate` (app-level dedup is race-prone).
- [ ] **E29. Consume `settlement.events`** — payout notifications via webhooks/email (exchange currently has zero consumers).
- [ ] **E30. Settlement↔Ledger tie-out** — verify before completion; retry/re-run path for failed payouts.

## Phase F — Frontend/portal parity

- [ ] **F31. Silent token refresh** — 401-retry + `/auth/refresh` in Next.js BFF (refresh_token cookie is written but never used); route merchant register through BFF (currently direct browser env exposure).
- [ ] **F32. Hosted checkout UI** + payment link page in merchant portal.
- [ ] **F33. Merchant payout/settlement history** + next-payout widget (currently admin-only).
- [ ] **F34. Webhook test UI** — endpoint list, send-test-event, delivery logs, signature preview.
- [ ] **F35. Customers + subscriptions + disputes UI** (merchant).
- [ ] **F36. Admin upgrades** — global search, accurate dashboard stats (currently first-page counts), payout management, dispute queue.
- [ ] **F37. Password reset / forgot-password** + optional 2FA/MFA.
- [ ] **F38. CSV/export** — payments, ledger, settlements.

## Phase G — Platform/ops hardening

- [ ] **G39. Use Redis** — distributed cache + rate-limit store + outbox lease store (Redis is provisioned but never used by any service).
- [ ] **G40. Real email/SMS providers** behind config (currently log-only simulation).
- [ ] **G41. Feature-flag abstraction.**
- [ ] **G42. Grafana provisioning + alerts** — dashboard + datasource provisioning, alert rules (outbox lag, DLQ depth, failed payments).
- [ ] **G43. Expand integration tests** — endpoint-level flows, gRPC, RabbitMQ consumers, webhook delivery, checkout.
- [ ] **G44. PCI-aligned posture** — encryption-at-rest for token vault, audit trail for card-data access, key rotation.

## Phase H — Launch readiness

- [ ] **H45. Production ingress** — TLS, network policies, secret rotation runbook.
- [ ] **H46. Load/stress test** + capacity plan, documented SLOs and runbooks.

---

*Generated: 2026-08-01. Full codebase review; each item is independently committable.*
