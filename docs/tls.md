# TLS / HTTPS

TASK-004 — no plaintext HTTP in production. TLS is terminated at nginx (Docker
Compose) and at the cluster Ingress (k8s). This document covers cert
provisioning, the Compose dev/prod switch, and frontend base URLs.

## Docker Compose (nginx)

The nginx container loads its server blocks from `/etc/nginx/tls/*.conf`. Two
mode folders are committed:

| Mode | How | Behaviour |
|---|---|---|
| Dev (default, `docker compose up -d`) | `./infra/nginx/http:/etc/nginx/tls:ro`, port `:80` only | Plain HTTP on `:80` |
| Prod (`docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d`) | `./infra/nginx/tls:/etc/nginx/tls:ro`, ports `:80` + `:443` | HTTP→HTTPS redirect on `:80`, HTTPS on `:443` |

### Provisioning (Let's Encrypt / certbot)

1. Point a DNS A record at the EC2 host (a raw IP cannot get a public cert).
2. Install certbot and obtain the certificate:

   ```bash
   sudo certbot certonly --standalone -d paymentswitch.example.com \
     --email ops@example.com --agree-tos --no-eff-email
   ```

3. Copy the certs into the (gitignored) certs folder:

   ```bash
   sudo cp /etc/letsencrypt/live/paymentswitch.example.com/fullchain.pem \
     infra/nginx/tls/certs/fullchain.pem
   sudo cp /etc/letsencrypt/live/paymentswitch.example.com/privkey.pem \
     infra/nginx/tls/certs/privkey.pem
   ```

4. Bring up the production overlay (Step 9.6) instead of editing the base file:

   ```bash
   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

   The overlay publishes `:443` and mounts `./infra/nginx/tls`; the base
   file stays HTTP-only on `:80`.

5. Verify:

    ```bash
    curl -I https://paymentswitch.example.com/merchant/health/live
    curl -I http://paymentswitch.example.com/merchant/health/live   # expect 301
    curl -I https://paymentswitch.example.com/                         # merchant portal
    curl -I https://paymentswitch.example.com/admin/login              # admin portal
    ```

The TLS server sets HSTS (`max-age=31536000; includeSubDomains`), forwards
`X-Forwarded-Proto: https`, and sets nosniff/SAMEORIGIN/referrer headers.

### Renewal

certbot installs a systemd timer (`systemctl list-timers | grep certbot`).
Add a post-hook to refresh the mounted certs and reload nginx:

```bash
certbot renew --deploy-hook "cp /etc/letsencrypt/live/<domain>/fullchain.pem infra/nginx/tls/certs/fullchain.pem && cp /etc/letsencrypt/live/<domain>/privkey.pem infra/nginx/tls/certs/privkey.pem && docker compose exec nginx nginx -s reload"
```

Or use an ACME-syncing sidecar (e.g. `certbot`/`cert-manager` dns01) that writes
directly to `infra/nginx/tls/certs`.

### Local development

Dev mode keeps serving plain HTTP on `:80`, so `docker compose up` works without
certs. Frontends use `NEXT_PUBLIC_API_URL=http://localhost`. If you want TLS in
dev, mount the tls mode and add a self-signed cert (browser trust it as a CA).

## Kubernetes Ingress

`k8s/ingress.yaml` declares a `tls` block for the host and references the
`payment-switch-tls` secret (created by cert-manager — see
`k8s/cert-manager-cluster-issuer.yaml`), with `ssl-redirect: true`. Replace the
placeholder host with the real domain. The Helm chart ingress mirrors this.

## Frontend base URLs (apps/merchant, apps/admin)

`NEXT_PUBLIC_API_URL` is inlined at build time. Committed `.env.production`
files are removed (TASK-004). Provide the value at build/run time via CI/CD
secrets or a gitignored `apps/<app>/.env.production`:

```bash
NEXT_PUBLIC_API_URL=https://paymentswitch.example.com npm run build
```

The apps are served through nginx/Ingress on the same origin: the merchant
portal at `/` and the admin portal at `/admin` (admin sets `basePath: "/admin"`
in `next.config.js`; nginx and the Ingress route `/admin` to `admin-web` and
`/` to `merchant-web` — no rewrite on those locations so the admin basePath
paths reach the app unchanged).

See `apps/<app>/.env.production.example`.

## gRPC service-to-service (Step 7.3)

Three internal gRPC channels cross service boundaries:

| Caller | Callee | Address key | Sensitive payload |
|---|---|---|---|
| payment-api | merchant-api (`GetMerchantConfig`, `ResolveApiKey`) | `Grpc:Merchant:Address` | webhook secrets (current + previous), API-key material |
| notification-api | merchant-api (`GetMerchantContact`) | `Grpc:Merchant:Address` | merchant contact email |
| settlement-api | ledger-api (`GetDailyPayoutData`, `GetBalances`) | `Grpc:Ledger:Address` | payout aggregates, balances |

Servers listen on `:5001` (HTTP/2 cleartext) alongside `:8080`. Port `5001`
is **never published** (no compose `ports`, no k8s Service port, no Ingress
route) and every gRPC method is gated by the `ServiceOnly` policy
(short-lived service JWT with `client_type=service`, 10-min expiry). That is
the documented in-network exception to end-to-end TLS.

### Production posture

- Default (fail-closed): `RequireGrpcTls` throws at startup in Production
  unless the address uses `https://`. Set e.g.
  `Grpc__Merchant__Address=https://merchant-api:5001` (compose env) or
  `Grpc.Merchant.Address` (appsettings) and mount a server cert via Kestrel
  (`ASPNETCORE_Kestrel__Certificates__Default__Path/KeyPath`) — clients
  automatically use TLS credentials for `https://` addresses (insecure call
  credentials are only applied to `http://`).
- Acknowledged exception: where mTLS is not yet provisioned, set
  `Grpc__Merchant__AllowInsecure=true` (or `Grpc__Ledger__AllowInsecure=true`).
  This is only valid while ALL of the following hold: the channel stays on the
  isolated compose/k8s network, port 5001 remains unpublished, the
  `ServiceOnly` gate stays enforced, and service-token secrets come from the
  managed store (see `docs/secrets.md`). The exception is logged as a
  deployment decision, not silent — startup still fails without the explicit
  flag.
- Encrypted payloads: merchant webhook secrets are AES-GCM encrypted at rest
  (TASK-006) and decrypted only inside the Merchant value-conversion and the
  Payment signing path; they traverse gRPC only inside the isolated network
  with `ServiceOnly` auth. API-key resolution sends the presented key over the
  same channel for BCrypt verification server-side (the stored hash never
  leaves Merchant).

### mTLS roadmap

Terminate per-service TLS with a shared internal CA (or a service mesh with
automatic mTLS, e.g. Linkerd/Istio) and require client certificates on `:5001`.
Until then, the controls above + NetworkPolicy restriction of `:5001` to the
three caller pods are the compensating controls.
