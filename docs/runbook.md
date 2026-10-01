# Operations Runbook

The live production runtime is **Docker Compose on an EC2 host** (see
`docker-compose.yml`). The Kubernetes manifests (`k8s/`) and Helm chart
(`helm/payment-switch`) are implemented and CI-tested but are **not** the
live runtime — treat them as the target platform for a future migration. This
runbook covers the compose deployment, TLS, backups, and common incident
responses. See `docs/deployment.md` (k8s/CI-CD), `docs/tls.md` (TLS), and
`docs/secrets.md` (secret management + rotation) for the deep dives.

## 1. Deploying the stack

### Prerequisites (one-time)

- EC2 host with Docker Engine + Compose v2 installed.
- DNS A record pointing the domain at the host's public IP (a raw IP cannot get
  a public certificate).
- A `.env` file at the repo root populated from the managed secret store
  (see `docs/secrets.md`). Missing required values cause `docker compose up`
  to fail fast, which is intentional.

### First deployment

```bash
git clone <repo> /opt/paymentswitch && cd /opt/paymentswitch
# Materialize .env from the secret store (AWS SSM/Secrets Manager)
docker compose up -d --build
docker compose ps          # wait until all services are healthy
```

Compose runs migrations on startup (`RunMigrations=true` on every API).

### Updating (normal release)

```bash
cd /opt/paymentswitch
git pull origin main
docker compose up -d --build --remove-orphans
docker compose ps
```

Each API uses a rolling restart by default (`restart: always`; nginx
`depends_on` all APIs). Brief overlaps are fine; the message bus and DB keep
the stack consistent. For zero-downtime deployment of a single service,
`docker compose up -d <service>`.

### Verifying a deploy

```bash
curl -fsS http://localhost/identity/api/v1/health/live
curl -fsS http://localhost/merchant/api/v1/health/ready
curl -I http://localhost/            # merchant portal
curl -I http://localhost/admin/login # admin portal
```

## 2. TLS (production)

TLS terminates at nginx (see `docs/tls.md`):

1. Obtain certs with certbot: `sudo certbot certonly --standalone -d <domain>`.
2. Copy `fullchain.pem` + `privkey.pem` into `infra/nginx/tls/certs/`
   (gitignored).
3. Switch nginx to TLS mode in `docker-compose.yml` (mount
   `./infra/nginx/tls:/etc/nginx/tls:ro`) and `docker compose up -d nginx`.
4. Confirm the redirect + HSTS:
   ```bash
   curl -I http://<domain>/identity/api/v1/health   # 301 -> https
   curl -I https://<domain>/                        # 200
   ```

### Renewal

certbot installs a systemd timer. Wire a `--deploy-hook` that copies the fresh
certs into `infra/nginx/tls/certs` and reloads nginx (see `docs/tls.md`).
Test renewal with `sudo certbot renew --dry-run`.

## 3. Backups

All state lives in the named volumes (`postgres_data`, `rabbitmq_data`,
`prometheus_data`, `grafana_data`; `redis_data` is cache).

### Postgres (critical)

```bash
# Full logical dump of every paymentswitch database, hourly/daily:
docker compose exec -T postgres pg_dump -U paymentswitch -F c -f - -C IdentityDb \
  | gzip > /var/backups/paymentswitch/identitydb-$(date +%F_%H%M).dump.gz
# ... repeat for MerchantDb, PaymentDb, LedgerDb, NotificationDb, SettlementDb
```

`pg_dumpall` is a simpler one-shot for the whole instance (superuser):

```bash
docker compose exec -T postgres pg_dumpall -U paymentswitch \
  | gzip > /var/backups/paymentswitch/pg-dumpall-$(date +%F_%H%M).sql.gz
```

Retention: keep 14 daily + 12 monthly copies off-host (S3/object storage).
Restore:

```bash
gunzip -c backup.sql.gz | docker compose exec -T postgres psql -U paymentswitch -d postgres
```

### RabbitMQ / Prometheus / Grafana

These are recreatable from config (`infra/`) and re-populate from the DB / live
traffic. A nightly snapshot of the volumes is optional:

```bash
docker run --rm -v paymentswitch_prometheus_data:/data -v /var/backups:/backup \
  alpine tar czf /backup/prometheus-$(date +%F).tgz -C /data .
```

## 4. Health, metrics & logs

- **Health endpoints:** each API exposes `/health/live` and `/health/ready`
  (nginx proxies them; used by the Docker healthchecks).
- **Metrics:** Prometheus scrapes `/metrics` from each API (port 8080) and
  RabbitMQ; Grafana dashboards are provisioned from `infra/grafana`.
  Alert rules live in `infra/prometheus/alerts.yml` (fired via Alertmanager).
- **Traces:** OpenTelemetry OTLP -> Jaeger (`http://jaeger:4317`).
- **Logs:** `docker compose logs -f <service>` (structured JSON via Serilog).
  For a tail of everything: `docker compose logs -f --tail=200`.

Public exposure of these surfaces (Swagger/metrics/Hangfire and the infra UIs)
is deliberately limited — see `docs/prod-exposure.md` (only nginx :80/:443 is
public; Swagger is disabled in Production; Grafana/Jaeger/Prometheus are
internal-only).

## 5. Common incidents

### A service keeps restarting

```bash
docker compose ps                 # which one is unhealthy/restarting
docker compose logs <service> --tail=100
docker compose exec <service> sh  # inspect if needed
```

Most common causes:

- **Migrations not run / DB unreachable** — check `ConnectionStrings__*`
  in compose and Postgres health.
- **Secret validation failure at startup** (intentional fail-fast) — the API
  exits immediately; re-check `.env` against `docs/secrets.md`. After rotating
  a JWT key, confirm both `JWT_SECRET` and `JWT_PREVIOUS_SECRET` are set during
  the window.
- **RabbitMQ credentials mismatch** — rotate `RABBITMQ_DEFAULT_USER/PASS`
  together across `.env` and the RabbitMQ container.

### Outbox/inbox backlog (consumers not draining)

```bash
docker compose logs <consumer-service> --tail=200
# RabbitMQ management UI on :15672 -> check queue depth (payment.events, DLQ)
```

The inbox/outbox are resilient by design (idempotent processing, DLX/DLQ). If
a consumer is down, restart it; messages accumulate in the queue and drain on
recovery.

### Disk filling up

```bash
docker system df                       # images, containers, volumes
docker compose logs --tail=200 -f      # large or log-spinning service?
docker image prune -af                 # prune dangling images (keep :latest tags)
docker volume ls                       # identify big named volumes
```

Postgres WAL growth is normal; if `postgres_data` balloons, check that
`wal_level`/archiving is configured for the backup approach in use.

### Rollback a bad release (compose)

```bash
cd /opt/paymentswitch
git checkout <previous-sha> -- docker-compose.yml infra/
docker compose up -d --build
```

(For the k8s/CI-CD target the pipeline records previous images and reverts
automatically — see `docs/deployment.md`.)

## 6. k8s / Helm — implemented, not live

The `k8s/` and `helm/` artefacts are validated by CI (`helm lint`, `helm
template`, YAML parse) but the production runtime is compose. Before relying
on k8s in production:

- Apply against a staging cluster and validate rollouts/HPA/NetworkPolicies.
- Provision the `payment-switch-secret` and configmap, wire External Secrets
  Operator, and confirm the Ingress + cert-manager issuance.
- Confirm the frontend deployments (`merchant-web`, `admin-web`) route through
  the frontend Ingress (`/` and `/admin`).

## 7. Transactional email (Resend verification mail)

Registration and resend publish `EmailVerificationRequestedDomainEvent` to the
Identity outbox; the Notification service consumes it from `identity.events`
and delivers via Resend (SMTP keeps serving every other mail channel).

### Resend account and sender setup (one-time, Resend dashboard)

1. Create an API key at `https://resend.com/api-keys` (starts with `re_`) and
   store it in the managed secret store as `RESEND_API_KEY` (compose `.env`),
   `Resend__ApiKey` (k8s `payment-switch-secret`, Helm `--set
   config.resend.apiKey=...`). Never commit it; never log it.
2. Add and verify the sender domain at `https://resend.com/domains` (SPF/DKIM
   DNS records Resend shows you; delivery from unverified domains is rejected).
   Set `RESEND_FROM_EMAIL` to an address on that domain
   (e.g. `noreply@paymentswitch.example.com`).
3. Set `FRONTEND_BASE_URL` to the trusted https frontend origin
   (production; this is the same origin the portals and password-reset links
   use). Verification links are built from this value only — never from request
   Host headers.

### Behavior and troubleshooting

- Leave `RESEND_API_KEY` empty in Development for simulated-email mode (logged
  without the token). In Production an empty key fails startup loudly — the
  service never silently drops verification mail.
- Resend throttling: `EMAIL_VERIFICATION_RESEND_COOLDOWN_SECONDS` (default 60)
  per address (429 inside the window, nothing sent/rotated); endpoint rate
  limiting (`Strict`) applies on top.
- Delivery failures ride the notification retry budget (exponential backoff,
  then DLQ recorded by the dead-letter consumer); provider outages never report
  success. Check `docker compose logs notification-api` (masked recipients,
  provider message id, no tokens) and the RabbitMQ management UI queue depth.
- Processed Identity outbox rows older than 7 days are hard-deleted by the
  retention sweep (transport envelopes; business facts live in `Users`).

### Migrations, existing accounts, and smoke test

- The feature adds three additive, nullable migrations (no backfill, no
  downtime): Identity `AddVerificationResendTracking`
  (`Users.LastVerificationEmailSentAtUtc`), Notification
  `AddNotificationProviderHint` (`Notifications.Provider`), plus the standard
  model snapshots. Compose runs them on startup (`RunMigrations=true`).
  Rollback drops only send-timestamps/provider hints; verification state
  (`EmailConfirmed`, token hashes) is untouched.
- Existing accounts are never mass-verified or locked out: unverified users
  keep logging in (gated only on key issuance/creation paths) and verify
  through the normal resend flow; the seeded admin stays pre-confirmed.
- After deploy, smoke the flow against the local stack:
  ```bash
  # 1. Register (creates user + outbox verification event)
  curl -fsS -X POST http://localhost/identity/api/v1/auth/register \
    -H 'Content-Type: application/json' \
    -d '{"email":"smoke@example.com","password":"SmokePass123!","fullName":"Smoke"}'
  # 2. Immediate resend must be throttled (proves the cooldown wiring)
  curl -s -o /dev/null -w "%{http_code}\n" -X POST \
    http://localhost/identity/api/v1/auth/resend-verification \
    -H 'Content-Type: application/json' -d '{"email":"smoke@example.com"}'  # expect 429
  # 3. Unknown addresses stay neutral (no enumeration oracle)
  curl -s -o /dev/null -w "%{http_code}\n" -X POST \
    http://localhost/identity/api/v1/auth/resend-verification \
    -H 'Content-Type: application/json' -d '{"email":"nobody@example.com"}'  # expect 200
  # 4. Notification consumed the event (Resend/simulated delivery afterwards)
  docker compose exec -T postgres psql -U paymentswitch -d NotificationDb \
    -c "SELECT \"Recipient\",\"Provider\",\"Status\" FROM \"Notifications\" WHERE \"Recipient\"='smoke@example.com';"
  ```
