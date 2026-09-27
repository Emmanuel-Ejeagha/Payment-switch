# Frontend tests

Two layers cover the Next.js frontends:

1. **Vitest + Testing Library** — fast component tests for `apps/merchant` and
   `apps/admin`, runnable anywhere (no services needed).
2. **Playwright** — browser smoke tests against a live stack (compose up + seeded
   data). Written and wired, but gated on environment variables because the full
   stack is usually not running during development.

## Vitest (component tests)

```bash
npm test --workspace=merchant
npm test --workspace=admin
```

Runs `vitest run` (jsdom) against `src/**/*.{test,spec}.{ts,tsx}`.

Configuration:

- `apps/{merchant,admin}/vitest.config.ts` — jsdom, globals, `@` /
  `@paymentswitch/shared` / `@paymentswitch/ui` aliases, setup file.
- `apps/{merchant,admin}/vitest.setup.ts` — imports `@testing-library/jest-dom/vitest`
  for the DOM matchers.

Coverage today (critical + regression-prone):

- Merchant: `copy-button` (clipboard API + `execCommand` fallback), `confirm-dialog`
  (`useConfirm` confirm/cancel), `field` (label binding, hint/error precedence,
  `AmountInput` currency chip), `stat-tile`.
- Admin: `empty-state`, `@paymentswitch/ui` `status-badge`, `stats-card`.

Notes:

- jsdom 30 removed `document.execCommand`; the copy-button fallback test re-adds a
  stub so the component's secure-context fallback path is exercised.
- `Field` labels render a `*` suffix (aria-hidden); match labels with a regex
  (`getByLabelText(/Amount/)`) instead of an exact string.

## Playwright (E2E smoke tests)

```bash
npx playwright install chromium
npm run test:e2e                    # runs the whole suite
npm run test:e2e -- --project=merchant
npm run test:e2e -- --grep "logs in"
```

Config: `playwright.config.ts` at the repo root (so vitest files under `apps/*`
are never picked up). Specs live in `e2e/tests/`. Two projects map to the two
frontends: `merchant` → `http://localhost:3000`, `admin` → `http://localhost:3001`
(overridable with `MERCHANT_URL` / `ADMIN_URL`).

### Prerequisites

1. Full stack up: `docker compose up -d --build` (services are not started by the
   test runner).
2. Frontends running: `npm run dev --workspace=merchant` and
   `npm run dev --workspace=admin` (or production builds).
3. Browser: `npx playwright install chromium` (one-time).
4. Seeded data (see `docs/testing.md`): a verified merchant owner and an admin.

### Required environment variables

Unset variables **skip** the affected test (skips are reported by the runner), so
the suite can run without secrets or a provisioned checkout page.

| Variable            | Needed by            | Description                                   |
| ------------------- | -------------------- | --------------------------------------------- |
| `E2E_EMAIL`         | merchant-login       | Seeded verified merchant owner email.         |
| `E2E_PASSWORD`      | merchant-login       | Owner password.                               |
| `E2E_ADMIN_EMAIL`   | admin-dashboard      | Seeded admin email.                           |
| `E2E_ADMIN_PASSWORD`| admin-dashboard      | Admin password.                               |
| `E2E_CHECKOUT_URL`  | checkout             | Live payment-link checkout URL.               |

Example:

```bash
E2E_EMAIL=owner@example.com E2E_PASSWORD='...' \
E2E_ADMIN_EMAIL=admin@example.com E2E_ADMIN_PASSWORD='...' \
E2E_CHECKOUT_URL=http://localhost:3000/checkout/demo \
npm run test:e2e
```

### Specs

- `merchant-login.spec.ts` — invalid credentials show an error; valid credentials
  land on `/dashboard`.
- `merchant-register.spec.ts` — creates a fresh account and lands on the email
  verification step.
- `checkout.spec.ts` — completes a card checkout with the test card
  (`4242 4242 4242 4242`, expiry `12/30`, CVC `123`).
- `admin-dashboard.spec.ts` — admin login reaches the dashboard overview.

## CI

- Component tests run in the `Frontend Build & Lint` job (`npm test --workspace=...`).
- E2E is intentionally **not** wired into CI: it needs the full stack, a Postgres
  seed, and a provisioned payment link. Run it locally (or a self-hosted runner
  with the stack up) against a staging environment with the env vars above.