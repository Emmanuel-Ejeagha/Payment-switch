# Deployment Guide

## Runtime Note

The **current production runtime is Docker Compose on EC2** (see
`infra/` + `docker-compose.yml`). The Kubernetes manifests (`k8s/`) and Helm
chart (`helm/payment-switch`) are **implemented and CI-tested but are not the
live production runtime**. Treat them as the target platform for a future
migration; verify them against a staging cluster before relying on them.

## Prerequisites

- Kubernetes cluster (v1.29+)
- `kubectl` configured to access the cluster
- Docker images built and pushed to a registry (or available locally for Docker Desktop)

## Step 1 – Create Namespace & Configuration

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
```

## Step 2 – Create Secrets

Create the secret with all connection strings and the JWT secret.
**Do not commit these values to source control.** The repository ships a template at
`k8s/secret.example.yaml`; copy it to `k8s/secret.yaml` (gitignored) and replace the
`REPLACE_ME` values, or generate a fresh secret with:

```bash
DB_PASSWORD=$(openssl rand -base64 24)
JWT_SECRET=$(openssl rand -base64 32)

kubectl create secret generic payment-switch-secret \
  --namespace payment-switch \
  --from-literal=Postgres__Password="$DB_PASSWORD" \
  --from-literal=Jwt__Secret="$JWT_SECRET" \
  --from-literal=IdentityDb__ConnectionString="Host=postgres;Database=IdentityDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=MerchantDb__ConnectionString="Host=postgres;Database=MerchantDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=PaymentDb__ConnectionString="Host=postgres;Database=PaymentDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=LedgerDb__ConnectionString="Host=postgres;Database=LedgerDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=NotificationDb__ConnectionString="Host=postgres;Database=NotificationDb;Username=paymentswitch;Password=$DB_PASSWORD" \
  --from-literal=SettlementDb__ConnectionString="Host=postgres;Database=SettlementDb;Username=paymentswitch;Password=$DB_PASSWORD"
```

## Step 3 – Deploy Infrastructure & Services

```bash
kubectl apply -f k8s/
```

This deploys PostgreSQL, Redis, RabbitMQ, Jaeger, Prometheus, Grafana, all six microservices, the two
frontend portals (`merchant-web`, `admin-web`), and the Ingress.

## Step 4 – Verify

```bash
kubectl get pods -n payment-switch
kubectl get ingress -n payment-switch
```

Access the APIs via `http://localhost/<service>/swagger`, the merchant portal at `/`, and the admin
portal at `/admin` (the admin app uses `basePath: /admin`; see `apps/admin/next.config.js`).

> **Note on the frontends:** `NEXT_PUBLIC_API_URL` is inlined at build time, so the web images are
> built with the HTTPS origin (see `.github/workflows/ci-cd.yml`, `docker-frontend` job and the
> `FRONTEND_API_URL` repository variable). The value is also injected at runtime from the
> `payment-switch-config` ConfigMap so the Next.js server-side API proxy resolves the same origin.
> No secrets are baked into the images.

## Optional – Port‑Forward Observability Tools

```bash
kubectl port-forward -n payment-switch svc/jaeger 16686:16686
kubectl port-forward -n payment-switch svc/prometheus 9090:9090
kubectl port-forward -n payment-switch svc/grafana 3000:3000
```

## CI/CD Automation

The GitHub Actions workflow (`.github/workflows/ci-cd.yml`) automatically builds, tests, and deploys
the services when changes are pushed to the `main` branch. Ensure the `KUBE_CONFIG` secret is set in
your repository.

The pipeline gates deploys per environment:

- **Pull requests** run build, unit, integration, and frontend checks, plus static analysis
  (`dotnet format --verify-no-changes`), dependency audits (`npm audit`, Dependabot), and security
  scans (CodeQL, Trivy, Gitleaks). No deploy happens from a PR.
- **Pushes to `main`** build and push images (immutable `:<sha>` plus `:latest`), scan them with
  Trivy (HIGH/CRITICAL fail the build), then deploy to the `production` environment, which is
  subject to **environment protection rules** (reviewers/approvals) in the repository settings.

### Rollback

Deploys are immutable-sha based, so rollback is a one-line image pin:

```bash
# Revert all services to a previous build (replace <previous-sha>):
for t in identity-api merchant-api payment-api ledger-api notification-api settlement-api \
         merchant-web admin-web; do
  kubectl set image deployment/$t $t=ghcr.io/<owner>/<repo>/$t:<previous-sha> \
    --namespace payment-switch
done
kubectl rollout status deployment/identity-api --namespace payment-switch
```

If a rollout fails, the pipeline records the previous image per service and automatically reverts
to it. For manual intervention, `kubectl rollout undo deployment/<svc>-api --namespace payment-switch`
returns to the last good revision.

### Environment protection

Enable **branch protection** on `main` (require `build-test`, `integration`, `frontend`, and the
security checks to pass before merging) and add required reviewers to the `production` environment
(Settings → Environments → `production` → Required reviewers). Keep `latest` tags for development
convenience; production always deploys by immutable `:<sha>`.
