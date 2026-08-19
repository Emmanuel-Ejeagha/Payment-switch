# Load tests (k6)

k6 load/soak tests for the **create-intent → confirm → capture** payment path
(TASK-043 / ROADMAP H46).

- `create-intent-capture.js` — short load profile (ramp → hold → ramp down).
- `soak.js` — 30-minute sustained soak to catch slow leaks.
- `lib/payment-flow.js` — the shared create → confirm → capture scenario.
- `lib/auth.js` — identity login used by the k6 `setup()` stage to get the
  capture JWT.

## Prerequisites

A provisioned merchant (registered, email-verified, approved, activated) with a
test API key. See `docs/load-testing.md` → "Provisioning" for the exact steps and
env vars.

## Run

```bash
docker run --rm -i -v "$PWD/tests/load:/load" \
  -e PAYMENT_BASE_URL=http://localhost:8080 -e MERCHANT_BASE_URL=http://localhost:8080 \
  -e API_KEY=sk_test_... -e MERCHANT_EMAIL=load-owner@example.com -e MERCHANT_PASSWORD='...' \
  grafana/k6 run /load/create-intent-capture.js
```

Full runbook, SLOs, capacity plan, and troubleshooting: `docs/load-testing.md`.
