# TLS / HTTPS

TASK-004 — no plaintext HTTP in production. TLS is terminated at nginx (Docker
Compose) and at the cluster Ingress (k8s). This document covers cert
provisioning, the Compose dev/prod switch, and frontend base URLs.

## Docker Compose (nginx)

The nginx container loads its server blocks from `/etc/nginx/tls/*.conf`. Two
mode folders are committed:

| Mode | Mount (`nginx.volumes`) | Behaviour |
|---|---|---|
| Dev (default) | `./infra/nginx/http:/etc/nginx/tls:ro` | Plain HTTP on `:80` |
| Prod | `./infra/nginx/tls:/etc/nginx/tls:ro` | HTTP→HTTPS redirect on `:80`, HTTPS on `:443` |

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

4. Switch nginx to TLS mode in `docker-compose.yml` and restart:

   ```yaml
   volumes:
     - ./infra/nginx/tls:/etc/nginx/tls:ro
   ```

   ```bash
   docker compose up -d nginx
   ```

5. Verify:

   ```bash
   curl -I https://paymentswitch.example.com/merchant/api/v1/health
   curl -I http://paymentswitch.example.com/merchant/api/v1/health   # expect 301
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

See `apps/<app>/.env.production.example`.
