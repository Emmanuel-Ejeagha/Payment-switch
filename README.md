# Real-Time Payment Switch

A production-grade, cloud‑native payment switch built with .NET 10, microservices, event‑driven architecture, and Kubernetes.  
The system simulates the core backend of payment processors like Stripe, Flutterwave, and Paystack.

> **Designed for education, portfolio quality, and deep understanding of distributed systems.**

## Live Deployment

The entire stack is deployed on **AWS EC2** (t3.small) using Docker Compose.
You can explore the live system here:

| Surface | URL |
|---|---|
| Merchant portal | http://16.171.58.173/ |
| Admin portal (login) | http://16.171.58.173/admin/login |
| API health (example) | http://16.171.58.173/identity/health/live |

Swagger UI is **disabled in production** (and denied at the edge by design),
so there are no public `/swagger` links — see `docs/prod-exposure.md`.
API docs for integrators live in `docs/api-reference.md` and `docs/webhooks.md`.

*(The instance stops automatically when credits run out, but you can always restart it.)*

---

## Architecture

The system follows **Domain‑Driven Design (DDD)**, **CQRS**, and **Event‑Driven Architecture**.
```
┌─────────────┐
│ Clients │
│ Merchant / Admin portals (Next.js), public API, checkout │
└──────┬──────┘
│ HTTPS
┌──────▼──────┐
│ Nginx │ (Reverse Proxy, TLS, Routing, per-service prefixes)
└──────┬──────┘
┌─────────────────────┼─────────────────────┐
│ │ │
┌──────▼──────┐ ┌────────▼────────┐ ┌────────▼────────┐
│ Identity │ │ Merchant │ │ Payment │
│ Service │ │ Service │ │ Service │
└──────┬──────┘ └────────┬────────┘ └────────┬────────┘
│ │ │
└──────────────┬─────┴──────────────┬──────┘
│ │
┌──────▼──────┐ ┌────────▼────────┐
│ RabbitMQ │ │ PostgreSQL │
│ (Outbox → exchange → inbox, │ │ (per service) │
│  retry/DLX → DLQ) │ │ │
└──────┬──────┘ └────────┬────────┘
│ │
┌──────────────┼────────────────────┼──────────────┐
│ │ │ │
┌──────▼──────┐ ┌─────▼──────┐ ┌────────▼────────┐ ┌──▼──────────┐
│ Ledger │ │ Notification│ │ Settlement │ │ Redis │
│ Service │ │ Service │ │ Service │ │ (Cache/ │
│ (Double-entry) │ │ (Retry + │ │ (Hangfire, │ │ Idempotency)│
│ │ │ SignalR) │ │ reconcil.) │ │ │
└─────────────┘ └────────────┘ └─────────────────┘ └─────────────┘

```

---

## Technology Stack

| Category               | Technologies                                                                 |
|------------------------|------------------------------------------------------------------------------|
| **Backend**            | .NET 10, ASP.NET Core, PostgreSQL 16, Redis 7, RabbitMQ 3, Hangfire          |
| **Testing**            | xUnit + Moq, Testcontainers (real Postgres/RabbitMQ), k6 (load/soak), Playwright (E2E), Vitest (frontend) |
| **Communication**      | REST (OpenAPI), RabbitMQ (AMQP), gRPC (internal sync calls)                 |
| **Authentication**     | JWT, API Keys, service-to-service tokens (gRPC)                             |
| **Validation**         | FluentValidation                                                             |
| **Observability**      | Serilog (structured logging), OpenTelemetry, Jaeger (tracing), Prometheus (metrics), Grafana (dashboards) |
| **Containerization**   | Docker, Docker Compose (live production runtime on EC2; dev via override-free base file) |
| **Orchestration**      | Kubernetes (Deployments, Services, Ingress, ConfigMaps, Secrets)             |
| **CI/CD**              | GitHub Actions (build, format gate, unit + Testcontainers integration tests, frontend typecheck/lint/build, Trivy/CodeQL/Gitleaks scans, Helm lint + kubeconform manifest validation, Docker build & push, gated deploy, nightly E2E + k6) |
| **Scheduling**         | Hangfire (nightly settlement batch)                                          |

---

## Microservices

| Service        | Database           | Responsibilities                                                                                     |
|----------------|--------------------|------------------------------------------------------------------------------------------------------|
| **Identity**   | `IdentityDb`       | Registration + email verification, login + JWT/refresh rotation, password reset/change, account lockout, RBAC (Admin/Merchant/Support) |
| **Merchant**   | `MerchantDb`       | Merchant onboarding with approval lifecycle, settlement/bank info, webhook config + encrypted signing secrets, API key management |
| **Payment**    | `PaymentDb`        | Payment intent lifecycle (authorize/capture/void/refund, partial ops), guarded state machine, idempotency keys, public secret-key API, card-token vault, payment links + hosted checkout, subscriptions/invoices, signed outbound webhooks |
| **Ledger**     | `LedgerDb`         | Double‑entry ledger, merchant balances (available/pending/reserved), immutable journal entries, crash-safe idempotent posting, reconciliation reports |
| **Notification**| `NotificationDb`  | Email / SMS / Webhook dispatch with retry & exponential backoff, DLQ recording, templates, preferences, SignalR realtime fan-out |
| **Settlement** | `SettlementDb`     | Nightly + on-demand settlement batches with ledger tie-out, idempotent per-date batches, payout rows, scheduled via Hangfire |

All services follow **Clean Architecture** with distinct **Domain**, **Application**, **Infrastructure**, and **API** layers.

---

## Patterns & Practices

- **Domain‑Driven Design** – Aggregates, Entities, Value Objects, Domain Events, Bounded Contexts
- **CQRS** – Commands, Queries, and Handlers with explicit separation
- **Event‑Driven Architecture** – RabbitMQ for inter‑service communication
- **Payment State Machine** – Payment lifecycle (authorize → capture → refund) enforced by a guarded domain state machine, with event‑driven reactions in Ledger
- **Outbox Pattern** – Reliable message publishing (captured events → database → background worker → RabbitMQ)
- **Inbox Pattern** – Idempotent message consumption with deduplication
- **Double‑Entry Ledger** – Ledger stores every financial movement as an immutable journal entry (credit/debit pairs)
- **Result Pattern** – Consistent error propagation across all services
- **Repository & Unit of Work** – Abstraction over EF Core
- **Retry with Exponential Backoff** – For failed notifications

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with Kubernetes enabled
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [Git](https://git-scm.com/)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Engine + Compose v2)
- [Node.js 22+](https://nodejs.org/) (for the Next.js portals)
- [Git](https://git-scm.com/)
- Optional for cluster work: [kubectl](https://kubernetes.io/docs/tasks/tools/), [Helm](https://helm.sh/)

### Local Development (Docker Compose)

1. **Clone the repository**

   ```bash
   git clone https://github.com/Emmanuel-Ejeagha/Payment-switch.git
   cd PaymentSwitch
   ```

2. **Create your `.env`** (copy `.env.example`; every secret is required —
   compose fails fast on missing values)

3. **Start the whole stack**

   ```bash
   docker compose up -d --build
   docker compose ps          # wait until all services report healthy
   ```

   For production TLS termination, overlay the TLS config (see `docs/tls.md`):

   ```bash
   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

4. **Verify**

   ```bash
   curl -fsS http://localhost/identity/health/live     # via nginx
   bash infra/smoke/compose-smoke.sh                   # per-API health + metrics matrix
   ```

Services and ports (host → container):

| Service | URL / port |
|---|---|
| Nginx (only public ingress) | `http://localhost` (`:80`, `:443` in prod overlay) |
| Merchant portal | `http://localhost/` |
| Admin portal | `http://localhost/admin/login` |
| APIs (loopback only) | `127.0.0.1:5146` (identity), `:5237` (merchant), `:5118` (payment), `:5320` (ledger), `:5281` (notification), `:5392` (settlement) → container `:8080` |
| RabbitMQ mgmt / metrics | `127.0.0.1:15672` / `:15692` (loopback only) |
| Postgres | `127.0.0.1:5432` (loopback only) |

Swagger UI is served in Development only (`/swagger` on each API's direct port);
it is disabled in Production and denied at the edge — see `docs/prod-exposure.md`.

### Running tests

```bash
# Unit tests (one project at a time)
dotnet test tests/Unit/Payment.Application.Tests/Payment.Application.Tests.csproj -c Release

# Integration tests (need Docker Desktop running; run projects one at a time)
dotnet test tests/Integration/Ledger.API.IntegrationTests/Ledger.API.IntegrationTests.csproj -c Release

# Cross-service E2E
dotnet test tests/Integration/E2E.IntegrationTests/E2E.IntegrationTests.csproj -c Release

# Frontend (per app in apps/merchant, apps/admin)
npm run typecheck && npm run lint && npm run test && npm run build

# Load (k6 must be installed; staging only)
k6 run tests/load/create-intent-capture.js
```

### Kubernetes Deployment

Ensure Kubernetes is running (Docker Desktop / minikube / kind).

**Create the namespace and secrets**

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
DB_PASSWORD=$(openssl rand -base64 24)
JWT_SECRET=$(openssl rand -base64 32)
SERVICE_TOKEN_SECRET=$(openssl rand -base64 32)
RABBITMQ_PASS=$(openssl rand -base64 24)
SEED_ADMIN_PASSWORD=$(openssl rand -base64 18)
WEBHOOK_KEY=$(openssl rand -base64 32)
GRAFANA_PASSWORD=$(openssl rand -base64 18)
SMTP_PASS='<smtp-password>'
kubectl create secret generic payment-switch-secret \
  --namespace payment-switch \
  --from-literal=Postgres__Password="$DB_PASSWORD" \
  --from-literal=Jwt__Secret="$JWT_SECRET" \
  --from-literal=ServiceToken__Secret="$SERVICE_TOKEN_SECRET" \
  --from-literal=RabbitMQ__UserName="paymentswitch" \
  --from-literal=RabbitMQ__Password="$RABBITMQ_PASS" \
  --from-literal=Seed__AdminPassword="$SEED_ADMIN_PASSWORD" \
  --from-literal=WebhookSecretEncryption__Key="$WEBHOOK_KEY" \
  --from-literal=GrafanaAdminUser="admin" \
  --from-literal=GrafanaAdminPassword="$GRAFANA_PASSWORD" \
  --from-literal=Smtp__Password="$SMTP_PASS" \
  --from-literal=IdentityDb__ConnectionString="Host=postgres;Database=IdentityDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=MerchantDb__ConnectionString="Host=postgres;Database=MerchantDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=PaymentDb__ConnectionString="Host=postgres;Database=PaymentDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=LedgerDb__ConnectionString="Host=postgres;Database=LedgerDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=NotificationDb__ConnectionString="Host=postgres;Database=NotificationDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=SettlementDb__ConnectionString="Host=postgres;Database=SettlementDb;Username=paymentswitch;Password=$DB_PASSWORD"
```

Every key above is required at pod start (`Jwt__PreviousSecret` excepted — rotation windows only). The full key set also lives in `k8s/secret.example.yaml`.
```

**Deploy all services**

```bash
kubectl apply -f k8s/
```

**Access via Ingress** (paths mirror the nginx compose routes; Swagger is
dev-only, so these are health and app routes)

- Merchant portal: http://localhost/ (admin portal: http://localhost/admin/login)
- Identity health: http://localhost/identity/health/live
- Merchant health: http://localhost/merchant/health/ready

### Observability

| Tool       | Access | Purpose                             |
|------------|-----------------------------------|-------------------------------------|
| Jaeger     | cluster-internal `:16686` (use `kubectl port-forward`) | Distributed traces across services  |
| Prometheus | cluster-internal `:9090` (use `kubectl port-forward`) | Metrics scraping |
| Grafana    | cluster-internal `:3000` (use `kubectl port-forward`) | Dashboards (see secrets for admin password) |

*(Grafana datasource + dashboards are provisioned from `infra/grafana/provisioning`; alert rules live in `infra/prometheus/alerts.yml`.)*

CI/CD Pipeline
The project uses GitHub Actions (`.github/workflows/`):

Triggers: push to main and pull requests (plus nightly E2E and load schedules).

- Build & Test: restore, `dotnet format` gate, Release build, NuGet vulnerability audit, unit tests with coverage.
- Integration Tests: Testcontainers suites (real Postgres + RabbitMQ), run sequentially per service.
- Frontend Build & Lint: typecheck, lint, unit/component tests, and production builds for both portals.
- Manifest Validation: `helm lint`, `helm template`, and kubeconform schema validation of `k8s/` and the rendered chart.
- Docker Build & Push: builds all six API images plus both frontend images with immutable `:<sha>` (and `:latest`) tags, Trivy-scanned, pushed to GHCR (only on push to main).
- Security Scan: CodeQL (C# + JS) and Gitleaks secret scanning on push, PR, and weekly schedule.
- Deploy to Kubernetes: gated on the `production` environment; pins immutable image SHAs via `kubectl set image`, verifies rollouts, auto-reverts on failure.
- E2E Nightly: full cross-service flow (register → authorize → capture → ledger → notification → settlement) plus k6 load/soak thresholds.

Required GitHub Secrets / Variables
Secret Name	Description
KUBE_CONFIG	Base64‑encoded kubeconfig for the target cluster
GITHUB_TOKEN	Automatically provided by GitHub Actions
FRONTEND_API_URL (variable)	Public API origin baked into the frontend images at build time
Project Structure
```
PaymentSwitch/
├── src/
│   ├── BuildingBlocks/
│   │   └── BuildingBlocks.Shared/         # Shared kernel (Result, auth, rate limiting, OTel, etc.)
│   ├── Protos/                            # gRPC contracts
│   └── Services/
│       ├── Identity/                      # Identity microservice
│       ├── Merchant/                      # Merchant microservice
│       ├── Payment/                       # Payment microservice
│       ├── Ledger/                        # Ledger microservice
│       ├── Notification/                  # Notification microservice
│       └── Settlement/                    # Settlement microservice
├── apps/
│   ├── merchant/                          # Merchant portal (Next.js, served at /)
│   └── admin/                             # Admin portal (Next.js, served at /admin via basePath)
├── packages/
│   ├── shared/                            # Shared TypeScript types & utils
│   └── ui/                                # Shared React components
├── tests/
│   ├── Unit/                              # Unit tests per service (+ API/middleware suites)
│   ├── Integration/                       # Testcontainers suites per service + cross-service E2E
│   ├── load/                              # k6 load/soak scripts (create-intent → capture)
│   └── Performance/                       # k6 nightly smoke
├── e2e/                                   # Playwright end-to-end tests
├── k8s/                                   # Kubernetes manifests (Deployments, Services, Ingress, PDBs, NetworkPolicies)
├── helm/
│   └── payment-switch/                    # Helm chart mirroring k8s/
├── infra/
│   ├── nginx/{http,tls}/                  # Edge configs (dev HTTP, prod TLS termination)
│   ├── prometheus/                        # Scrape config + alert rules
│   ├── grafana/provisioning/              # Datasources + dashboards
│   ├── alertmanager/                      # Alert routing
│   ├── postgres/                          # Multi-database init scripts
│   ├── backup/                            # Nightly pg-backup job + restore drill
│   └── smoke/                             # Compose smoke test (health + metrics matrix)
├── docker-compose.yml                     # Live runtime (all services, health-gated startup)
├── docker-compose.prod.yml                # Prod overlay (TLS :443)
├── Directory.Build.props                  # Solution-wide build policy (incl. NuGet audit gate)
├── .github/workflows/                     # ci-cd, e2e-nightly, k6-nightly, security pipelines
├── docs/                                  # runbook, deployment, tls, secrets, webhooks, api-reference, ...
└── PaymentSwitch.slnx
```
Further documentation: `docs/runbook.md` (operations), `docs/deployment.md` (k8s/Helm/CI-CD),
`docs/tls.md`, `docs/secrets.md`, `docs/api-reference.md`, `docs/webhooks.md`,
`docs/prod-exposure.md`, `docs/roles.md`, `docs/branch-protection.md`,
`docs/load-testing.md`, `docs/frontend-tests.md`, `docs/messaging-registry.md`,
`docs/api-versioning.md`, `docs/RETENTION.md`.
Delivered recently (previously listed here as future work)
- Real‑time webhooks with SignalR *(notification hub + both portals live)*
- Merchant Portal *(Next.js, live)* and Admin Portal *(Next.js, live, role-gated)*
- Helm charts for Kubernetes deployment *(chart mirrors k8s/, lint/template/kubeconform-gated in CI)*
- Public Payments API with secret‑key auth (Stripe/Paystack‑style, idempotency keys enforced)
- Card tokenization vault, hosted Checkout page, payment links
- Subscriptions, plans, invoices & recurring billing worker
- Multi‑currency balances with exponent-aware money handling *(FX conversion pending)*
- Production TLS termination at nginx/Ingress plus default-deny NetworkPolicies

Still ahead
- Full OAuth2 / OpenID Connect flows
- FX conversion for multi‑currency settlement
- Disputes / chargebacks
- Admin-portal support for the read-only Support role (API already grants it)