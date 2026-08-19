# Production Exposure & Surface Hardening

This document defines what is reachable from the public internet in the
production runtime (Docker Compose on EC2) and how the discovery/observability
surfaces are kept private. It is the deliverable for TASK-046 (ROADMAP H45).

## The rule: nginx is the only public ingress

```
Internet ──> EC2 security group (80/443 only)
                │
                ▼
             nginx (:80/:443)  ──> merchant-web, admin-web, /identity/, /merchant/,
                                   /payment/, /ledger/, /notification/, /settlement/
```

Everything else is bound to **loopback (`127.0.0.1`)** in `docker-compose.yml`:

| Component | Container port | Published on | Reachable publicly? |
|---|---|---|---|
| nginx | 80/443 | `0.0.0.0` | **yes — the only ingress** |
| identity-api / merchant-api / payment-api / ledger-api / notification-api / settlement-api | 8080 | `127.0.0.1:5146/5237/5118/5320/5281/5392` | no (host-only) |
| postgres | 5432 | `127.0.0.1` | no |
| rabbitmq (AMQP + mgmt) | 5672/15672/15692 | `127.0.0.1` | no |
| jaeger | 16686/4317 | not published (Docker network only) | no |
| prometheus / alertmanager | 9090/9093 | not published | no |
| grafana | 3000 | not published | no |

**Operational note:** in the EC2 security group only open TCP 80 and 443. The
loopback bindings are defense-in-depth for the case where the group is later
loosened. If you must reach Grafana/Jaeger/RabbitMQ UI from your laptop, use an
SSH tunnel to `127.0.0.1:<hostport>` — never open the port.

## Surfaces handled individually

| Surface | Where it lives | Protection |
|---|---|---|
| Swagger/OpenAPI (`/swagger`, `/v1/swagger.json`) | each API | Disabled when `ASPNETCORE_ENVIRONMENT=Production` (all 6 `Program.cs`); additionally denied at nginx for dev/prod |
| Prometheus `/metrics` | each API | Must stay on for scraping, so it is **not** disabled; only reachable on the Docker network (Prometheus scrapes `service:8080/metrics`) and host loopback. Public access denied at nginx |
| Hangfire dashboard (`/hangfire`) | settlement-api | Admin-only JWT gate (TASK-013) + denied at nginx as defense-in-depth |
| Grafana | compose | No published port + required `GRAFANA_ADMIN_PASSWORD` |
| Jaeger UI | compose | No published port |
| RabbitMQ management | compose | Loopback-only binding |

## Enabling the production posture

1. Set `ASPNETCORE_ENVIRONMENT=Production` in the host `.env` (compose now
   defaults to `Development` but honors `ASPNETCORE_ENVIRONMENT` from `.env`).
2. Keep the nginx TLS server block mounted (see `docs/tls.md`); the deny
   location for discovery surfaces is already in both `infra/nginx/http` and
   `infra/nginx/tls` server configs.
3. Ensure the EC2 security group allows only 80/443 inbound.

## Verification

After `docker compose up -d` on the host:

```bash
# From an external vantage point (or after confirming the SG only allows 80/443):
curl -s -o /dev/null -w "%{http_code}\n" https://host/identity/swagger   # expect 404
curl -s -o /dev/null -w "%{http_code}\n" https://host/payment/metrics     # expect 404
curl -s -o /dev/null -w "%{http_code}\n" https://host/settlement/hangfire # expect 404
curl -s -o /dev/null -w "%{http_code}\n" https://host/identity/api/v1/... # expect real API behavior

# On the host itself, the loopback-bound services are still reachable:
curl -s http://127.0.0.1:5146/health/live                                # expect 200
```

Swagger in Production is covered by `SwaggerExposureTests`
(`tests/Integration/Payment.API.IntegrationTests/SwaggerExposureTests.cs`):
`/swagger` and `/v1/swagger.json` return 200 in Development/Test and 404 in
Production, while `/metrics` stays 200 in Production.