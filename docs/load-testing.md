# Load & Performance Testing

This document defines the SLOs, capacity plan, and runbook for validating the
Payment service's throughput claims for the **create-intent → confirm → capture**
path (ROADMAP H46 / TASK-043). Load tests live in `tests/load/` and use
[k6](https://k6.io) — a single binary / Docker image, no Node dependencies.

## SLOs under test

| SLO | Target |
|---|---|
| Availability / success | ≥ 99% of create-intent + confirm + capture requests succeed |
| Create-intent latency | p95 < 500 ms, p99 < 1000 ms |
| Confirm (authorize) latency | p95 < 500 ms, p99 < 1000 ms |
| Capture latency | p95 < 500 ms, p99 < 1000 ms |
| Soak stability | 30 min at sustained load with no latency/error degradation |

These are baseline targets for the reference deployment (docker-compose / single
node). They are enforced as k6 `thresholds`, so a run fails loudly if they break.

## Capacity plan

A full payment is **3 sequential API round-trips** per unit of work:
`POST /v1/payments/intents` → `POST /v1/payments/{id}/confirm` →
`POST /api/v1/payments/{id}/capture`.

- **Load profile**: ramp to 20 concurrent VUs, hold 3 minutes → ≈ 40 requests/s
  (20 VUs × 3 requests per ~1.5 s cycle).
- **Throughput claim**: at 20 VUs the stack must sustain ≈ 40 req/s with p95
  < 500 ms and < 1% errors. Scale roughly linearly with DB/broker capacity; the
  hot path touches the Payment DB (intent + outbox), RabbitMQ
  (PaymentAuthorized / PaymentCaptured events), and the Ledger/Notification
  consumers downstream.
- **Soak**: 10 VUs for 30 minutes catches accumulation (outbox backlog, RabbitMQ
  queue depth, webhook dispatch retries) that short runs hide.
- **Headroom**: keep steady-state load ≤ 70% of the measured peak so spikes do
  not breach SLOs. Rerun the load profile after any change to the payment hot
  path, the outbox publisher, or gateway routing.

## Provisioning (once per environment)

The load test assumes a fully verified merchant. Provision it manually through
the UIs/APIs, then export the env vars below:

1. **Register** a merchant owner at `/api/v1/auth/register` and **verify the
   email** (the verification token arrives by email; complete it manually).
2. **Login** as the owner and **onboard** the merchant (`POST /api/v1/merchants`
   with `BusinessName`, `Email`).
3. As an **admin**: `POST /api/v1/merchants/{merchantId}/approve` and
   `POST /api/v1/merchants/{merchantId}/activate`.
4. As the owner: `POST /api/v1/merchants/{merchantId}/apikeys` with
   `{ "Environment": "test" }` → keep the returned `plainTextKey`
   (`sk_test_...`).

Export for the run:

```bash
export PAYMENT_BASE_URL=http://localhost:8080     # payment-api public endpoint
export MERCHANT_BASE_URL=http://localhost:8080    # identity-api login endpoint
export API_KEY=sk_test_...
export MERCHANT_EMAIL=load-owner@example.com
export MERCHANT_PASSWORD='<owner password>'
```

## Running the tests

k6 is not a dependency of this repo; install it or use the Docker image:

```bash
# Load test (5 minutes)
docker run --rm -i -v "$PWD/tests/load:/load" \
  -e PAYMENT_BASE_URL="$PAYMENT_BASE_URL" -e MERCHANT_BASE_URL="$MERCHANT_BASE_URL" \
  -e API_KEY="$API_KEY" -e MERCHANT_EMAIL="$MERCHANT_EMAIL" -e MERCHANT_PASSWORD="$MERCHANT_PASSWORD" \
  grafana/k6 run /load/create-intent-capture.js

# Soak test (30 minutes)
docker run --rm -i -v "$PWD/tests/load:/load" \
  -e PAYMENT_BASE_URL="$PAYMENT_BASE_URL" -e MERCHANT_BASE_URL="$MERCHANT_BASE_URL" \
  -e API_KEY="$API_KEY" -e MERCHANT_EMAIL="$MERCHANT_EMAIL" -e MERCHANT_PASSWORD="$MERCHANT_PASSWORD" \
  grafana/k6 run /load/soak.js
```

Or, with a local k6 binary:

```bash
k6 run tests/load/create-intent-capture.js
k6 run tests/load/soak.js
```

### Scenario mix and knobs

| Env var | Default | Meaning |
|---|---|---|
| `PAYMENT_BASE_URL` | `http://localhost:8080` | Payment API base |
| `MERCHANT_BASE_URL` | `http://localhost:8080` | Identity API base for login |
| `API_KEY` | — | Merchant test secret key (`sk_test_...`) |
| `MERCHANT_EMAIL` / `MERCHANT_PASSWORD` | — | Owner credentials for the capture JWT |
| `PAYMENT_AMOUNT` | `10000` | Amount in minor units |
| `PAYMENT_CURRENCY` | `USD` | ISO code |
| `CARD_LAST_FOUR` | `4242` | `4242` = clean authorize; `3001` = 3DS RequiresAction |
| `CARD_BRAND` | `Visa` | Must pair with the gateway router (Visa/Master → Stripe mock) |

## Interpreting results

A run exits non-zero when any threshold breaks. Review:

- `http_req_duration` p95/p99 per `flow` tag (create/confirm/capture) — which hop
  is slowest.
- `http_req_failed` — 4xx/5xx rate; non-200 create/confirm/capture failures
  surface as failed checks.
- Downstream health: outbox `Processed` backlog, RabbitMQ queue depth, and
  Ledger/Notification consumer lag after the run — a clean soak leaves no
  growing backlog.

## Runbook for a breached SLO

1. Confirm the stack is at the assumed capacity (single node vs. scaled-out);
   re-run against the same topology that is in production.
2. Check the slowest hop (create/confirm/capture) from the k6 per-flow report.
3. If **create/confirm** are slow: DB contention (intent + outbox writes) or
   gateway latency — check Payment service logs for the mock gateway path.
4. If **capture** is slow: the outbox publish and downstream Ledger post are
   asynchronous, so capture latency is mostly DB; check for lock waits on the
   intent row.
5. If error rate climbs during soak: watch for RabbitMQ connection churn, outbox
   backlog, or webhook retry storms (notification/webhook tables growing).
6. Record the run (k6 JSON summary), file the capacity-plan update, and rerun
   the load profile.