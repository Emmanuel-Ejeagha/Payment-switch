# Real-Time Payment Switch

A production-grade, cloud‑native payment switch built with .NET 10, microservices, event‑driven architecture, and Kubernetes.  
The system simulates the core backend of payment processors like Stripe, Flutterwave, and Paystack.

> **Designed for education, portfolio quality, and deep understanding of distributed systems.**

## Live Deployment

The entire stack is deployed on **AWS EC2** (t3.small) using Docker Compose.  
You can explore the live APIs here:

| Service        | Swagger UI                                         |
|----------------|----------------------------------------------------|
| Identity       | http://16.171.58.173/identity/swagger              |
| Merchant       | http://16.171.58.173/merchant/swagger              |
| Payment        | http://16.171.58.173/payment/swagger               |
| Ledger         | http://16.171.58.173/ledger/swagger                |
| Notification   | http://16.171.58.173/notification/swagger          |
| Settlement     | http://16.171.58.173/settlement/swagger            |

*(The instance stops automatically when credits run out, but you can always restart it.)*

---

## Architecture

The system follows **Domain‑Driven Design (DDD)**, **CQRS**, and **Event‑Driven Architecture**.
```
┌─────────────┐
│ Clients │
│ SPA / MAPI │
└──────┬──────┘
│ HTTPS
┌──────▼──────┐
│ Nginx │ (Reverse Proxy, TLS, Routing)
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
│ (Broker) │ │ (per service) │
└──────┬──────┘ └────────┬────────┘
│ │
┌──────────────┼────────────────────┼──────────────┐
│ │ │ │
┌──────▼──────┐ ┌─────▼──────┐ ┌────────▼────────┐ ┌──▼──────────┐
│ Ledger │ │ Notification│ │ Settlement │ │ Redis │
│ Service │ │ Service │ │ Service │ │ (Cache/ │
│ (Double-entry) │ │ (Retry) │ │ (Hangfire) │ │ Idempotency)│
└─────────────┘ └────────────┘ └─────────────────┘ └─────────────┘

```

---

## Technology Stack

| Category               | Technologies                                                                 |
|------------------------|------------------------------------------------------------------------------|
| **Backend**            | .NET 10, ASP.NET Core, PostgreSQL 16, Redis 7, RabbitMQ 3, Hangfire          |
| **Testing**            | xUnit, Moq, EF Core InMemory                                                 |
| **Communication**      | REST (OpenAPI), RabbitMQ (AMQP), gRPC (internal sync calls)                 |
| **Authentication**     | JWT, API Keys, service-to-service tokens (gRPC)                             |
| **Validation**         | FluentValidation                                                             |
| **Observability**      | Serilog (structured logging), OpenTelemetry, Jaeger (tracing), Prometheus (metrics), Grafana (dashboards) |
| **Containerization**   | Docker, Docker Compose (local dev)                                           |
| **Orchestration**      | Kubernetes (Deployments, Services, Ingress, ConfigMaps, Secrets)             |
| **CI/CD**              | GitHub Actions (build, test, Docker build & push, deploy to Kubernetes)      |
| **Scheduling**         | Hangfire (nightly settlement batch)                                          |

---

## Microservices

| Service        | Database           | Responsibilities                                                                                     |
|----------------|--------------------|------------------------------------------------------------------------------------------------------|
| **Identity**   | `IdentityDb`       | User registration, login, JWT issuance, API key management, role‑based access control                |
| **Merchant**   | `MerchantDb`       | Merchant onboarding, activation/suspension, webhook & payment method configuration                   |
| **Payment**    | `PaymentDb`        | Payment intent creation, authorization, capture, void, refund, idempotency, routing (simulated)      |
| **Ledger**     | `LedgerDb`         | Double‑entry ledger, merchant balances (available/pending/reserved), immutable journal entries       |
| **Notification**| `NotificationDb`  | Email / SMS / Webhook dispatch with retry & exponential backoff, driven by payment events            |
| **Settlement** | `SettlementDb`     | End‑of‑day settlement batch calculation, merchant payouts, scheduled via Hangfire                    |

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

### Local Development (Docker Compose)

1. **Clone the repository**

   ```bash
   git clone https://github.com/Emmanuel-Ejeagha/Payment-switch.git
   cd PaymentSwitch
   ```

2. **Start infrastructure services**

   ```bash
   docker-compose up -d
   ```

3. **Run each microservice** (each in its own terminal)

   ```bash
   dotnet run --project src/Services/Identity/Identity.API
   dotnet run --project src/Services/Merchant/Merchant.API
   dotnet run --project src/Services/Payment/Payment.API
   dotnet run --project src/Services/Ledger/Ledger.API
   dotnet run --project src/Services/Notification/Notification.API
   dotnet run --project src/Services/Settlement/Settlement.API
   ```

   Access APIs at `http://localhost:5xxx/swagger` (ports are configured in launchSettings.json).

### Kubernetes Deployment

Ensure Kubernetes is running (Docker Desktop / minikube / kind).

**Create the namespace and secrets**

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
DB_PASSWORD=$(openssl rand -base64 24)
JWT_SECRET=$(openssl rand -base64 32)
kubectl create secret generic payment-switch-secret \
  --namespace payment-switch \
  --from-literal=Jwt__Secret="$JWT_SECRET" \
  --from-literal=Postgres__Password="$DB_PASSWORD" \
  --from-literal=IdentityDb__ConnectionString="Host=postgres;Database=IdentityDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  # ... add all connection strings (see docs/deployment.md)
```

**Deploy all services**

```bash
kubectl apply -f k8s/
```

**Access via Ingress**

- Identity: http://localhost/identity/swagger
- Merchant: http://localhost/merchant/swagger
- Payment: http://localhost/payment/swagger
- Ledger: http://localhost/ledger/swagger
- Notification: http://localhost/notification/swagger
- Settlement: http://localhost/settlement/swagger

### Observability

| Tool       | Access URL / Port          | Purpose                             |
|------------|----------------------------|-------------------------------------|
| Jaeger     | http://localhost:16686     | Distributed traces across services  |
| Prometheus | http://localhost:9090      | Metrics scraping                    |
| Grafana    | http://localhost:3000      | Dashboards (admin / `GRAFANA_ADMIN_PASSWORD`) |

*(Grafana datasource + dashboards are provisioned from `infra/grafana/provisioning`; alert rules live in `infra/prometheus/alerts.yml`.)*

CI/CD Pipeline
The project uses GitHub Actions:

Triggers: push to main and pull requests.

Build & Test: Restores, builds, and runs all unit tests.

Docker Build & Push: Builds all six Docker images and pushes them to GitHub Container Registry (only on push to main).

Deploy to Kubernetes: Updates the Kubernetes deployments with the new image tags and verifies rollouts.

Required GitHub Secrets
Secret Name	Description
KUBE_CONFIG	Base64‑encoded kubeconfig for the target cluster
GITHUB_TOKEN	Automatically provided by GitHub Actions
Project Structure
```
PaymentSwitch/
├── src/
│   ├── BuildingBlocks/
│   │   └── BuildingBlocks.Shared/         # Shared kernel (Result, AggregateRoot, etc.)
│   ├── Protos/                            # gRPC contracts
│   └── Services/
│       ├── Identity/                      # Identity microservice
│       ├── Merchant/                      # Merchant microservice
│       ├── Payment/                       # Payment microservice
│       ├── Ledger/                        # Ledger microservice
│       ├── Notification/                  # Notification microservice
│       └── Settlement/                    # Settlement microservice
├── apps/
│   ├── merchant/                          # Merchant portal (Next.js SPA)
│   └── admin/                             # Admin portal (Next.js SPA)
├── packages/
│   ├── shared/                            # Shared TypeScript types & utils
│   └── ui/                                # Shared React components
├── tests/
│   ├── Unit/                              # Unit tests per service
│   └── Integration/                       # Integration tests per service
├── k8s/                                   # Kubernetes manifests
├── helm/                                  # Helm chart
├── infra/                                 # Nginx, Prometheus, Postgres configs
├── .github/workflows/                     # CI/CD pipeline
├── docs/                                  # Detailed documentation
└── PaymentSwitch.slnx
```
Future Enhancements
- Real‑time webhooks with SignalR *(implemented — notification hub + portal UI)*
- Full OAuth2 / OpenID Connect flows
- Multi‑currency support with FX conversion *(multi‑currency balances implemented; FX pending)*
- Merchant Portal *(implemented — Next.js)*
- Admin Portal *(implemented — Next.js)*
- Helm charts for Kubernetes deployment *(scaffolded)*
- Public Payments API with secret‑key auth (Stripe/Paystack‑style)
- Card tokenization, 3DS, and a hosted Checkout page
- Subscriptions & recurring billing
- Disputes / chargebacks
- Production‑ready TLS & network policies