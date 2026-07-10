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

The system follows **Domain‑Driven Design (DDD)**, **CQRS**, **Event Sourcing**, and **Event‑Driven Architecture** with **Saga** patterns for distributed transactions.
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
│ (Event Src) │ │ (Retry) │ │ (Hangfire) │ │ Idempotency)│
└─────────────┘ └────────────┘ └─────────────────┘ └─────────────┘

```

---

## Technology Stack

| Category               | Technologies                                                                 |
|------------------------|------------------------------------------------------------------------------|
| **Backend**            | .NET 10, ASP.NET Core, PostgreSQL 16, Redis 7, RabbitMQ 3, Hangfire          |
| **Testing**            | xUnit, Moq, EF Core InMemory                                                 |
| **Communication**      | REST (OpenAPI), RabbitMQ (AMQP), gRPC (internal sync calls)                 |
| **Authentication**     | JWT, OAuth2, API Keys                                                        |
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
- **Saga Pattern** – Payment lifecycle (authorize → capture → refund) coordinated via process manager
- **Outbox Pattern** – Reliable message publishing (captured events → database → background worker → RabbitMQ)
- **Inbox Pattern** – Idempotent message consumption with deduplication
- **Event Sourcing** – Ledger stores all financial movements as an immutable event stream
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
Start infrastructure services

```bash
docker-compose -f infra/docker-compose.yml up -d
Run each microservice (each in its own terminal)

bash
dotnet run --project src/Services/Identity/Identity.API
dotnet run --project src/Services/Merchant/Merchant.API
dotnet run --project src/Services/Payment/Payment.API
dotnet run --project src/Services/Ledger/Ledger.API
dotnet run --project src/Services/Notification/Notification.API
dotnet run --project src/Services/Settlement/Settlement.API
Access APIs at http://localhost:5xxx/swagger (ports are configured in launchSettings.json).
```
Kubernetes Deployment
Ensure Kubernetes is running (Docker Desktop / minikube / kind).

Create the namespace and secrets

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl create secret generic payment-switch-secret \
  --namespace payment-switch \
  --from-literal=Jwt__Secret="your-super-secret-key-minimum-32-bytes!" \
  --from-literal=Postgres__Password="paymentswitch" \
  --from-literal=IdentityDb__ConnectionString="Host=postgres;Database=IdentityDb;Username=paymentswitch;Password=paymentswitch" \
  # ... add all connection strings (see docs/deployment.md)
```
Deploy all services

```bash
kubectl apply -f k8s/
Access via Ingress

Identity: http://localhost/identity/swagger

Merchant: http://localhost/merchant/swagger

Payment: http://localhost/payment/swagger

Ledger: http://localhost/ledger/swagger

Notification: http://localhost/notification/swagger

Settlement: http://localhost/settlement/swagger
```
Observability
Tool	Access URL / Port	Purpose
Jaeger	http://localhost:16686	Distributed traces across all services
Prometheus	http://localhost:9090	Metrics scraping
Grafana	http://localhost:3000	Dashboards (admin / admin)
A pre‑configured ASP.NET Core HTTP Overview dashboard is available in Grafana showing request rate, latency percentiles, and active connections.

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
│   ├── Services/
│   │   ├── Identity/                      # Identity microservice
│   │   ├── Merchant/                      # Merchant microservice
│   │   ├── Payment/                       # Payment microservice
│   │   ├── Ledger/                        # Ledger microservice
│   │   ├── Notification/                  # Notification microservice
│   │   └── Settlement/                    # Settlement microservice
│   └── Frontend/                          # Future SPA
├── tests/
│   └── Unit/                              # Unit tests per service
├── k8s/                                   # Kubernetes manifests
├── infra/                                 # Docker Compose & Nginx config
├── .github/workflows/                     # CI/CD pipeline
├── docs/                                  # Detailed documentation
└── PaymentSwitch.slnx
```
Future Enhancements
Real‑time webhooks with SignalR

Full OAuth2 / OpenID Connect flows

Multi‑currency support with FX conversion

Merchant Portal (React SPA)

Admin Portal

Helm charts for Kubernetes deployment

Production‑ready TLS & network policies