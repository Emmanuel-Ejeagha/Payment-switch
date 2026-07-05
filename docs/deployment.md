# Deployment Guide

## Prerequisites

- Kubernetes cluster (v1.29+)
- `kubectl` configured to access the cluster
- Docker images built and pushed to a registry (or available locally for Docker Desktop)

## Step 1 – Create Namespace & Configuration

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
Step 2 – Create Secrets
Create the secret with all connection strings and the JWT secret.
Do not commit these values to source control.

```
```bash
kubectl create secret generic payment-switch-secret \
  --namespace payment-switch \
  --from-literal=Jwt__Secret="your-super-secret-key-minimum-32-bytes!" \
  --from-literal=Postgres__Password="paymentswitch" \
  --from-literal=IdentityDb__ConnectionString="Host=postgres;Database=IdentityDb;Username=paymentswitch;Password=paymentswitch" \
  --from-literal=MerchantDb__ConnectionString="Host=postgres;Database=MerchantDb;Username=paymentswitch;Password=paymentswitch" \
  --from-literal=PaymentDb__ConnectionString="Host=postgres;Database=PaymentDb;Username=paymentswitch;Password=paymentswitch" \
  --from-literal=LedgerDb__ConnectionString="Host=postgres;Database=LedgerDb;Username=paymentswitch;Password=paymentswitch" \
  --from-literal=NotificationDb__ConnectionString="Host=postgres;Database=NotificationDb;Username=paymentswitch;Password=paymentswitch" \
  --from-literal=SettlementDb__ConnectionString="Host=postgres;Database=SettlementDb;Username=paymentswitch;Password=paymentswitch"
  ```
Step 3 – Deploy Infrastructure & Services
```bash
kubectl apply -f k8s/
This deploys PostgreSQL, Redis, RabbitMQ, Jaeger, Prometheus, Grafana, all six microservices, and the Ingress.
```
Step 4 – Verify
```bash
kubectl get pods -n payment-switch
kubectl get ingress -n payment-switch
Access the APIs via http://localhost/<service>/swagger.
```
Optional – Port‑Forward Observability Tools
```bash
kubectl port-forward -n payment-switch svc/jaeger 16686:16686
kubectl port-forward -n payment-switch svc/prometheus 9090:9090
kubectl port-forward -n payment-switch svc/grafana 3000:3000
CI/CD Automation
The GitHub Actions workflow (.github/workflows/ci-cd.yml) automatically builds, tests, and deploys the services when changes are pushed to the main branch. Ensure the KUBE_CONFIG secret is set in your repository.

```
---