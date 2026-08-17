# Secret Management

TASK-035 — production secrets come from a managed store, and rotation is
automated with dual-write windows.

## Current state

| Runtime | Secret source |
|---|---|
| Docker Compose (EC2, current production) | `.env` (gitignored), fail-fast on missing vars |
| Kubernetes (target platform) | `payment-switch-secret` k8s Secret (from `k8s/secret.yaml`, gitignored, or `kubectl create secret`) |
| Helm | `payment-switch-secret` Secret rendered from values (secrets via `--set` / a secrets values file) |

Nothing is committed to source control. `k8s/secret.example.yaml` and `.env.example`
are REPLACE_ME templates.

## Adopting a managed store

### Option A — AWS (matches the EC2 compose runtime)

Store every secret in AWS Secrets Manager (or SSM Parameter Store with
`SecureString`), one parameter per value, then materialize `.env` at deploy time:

```bash
aws ssm get-parameters --names \
  /paymentswitch/prod/JWT_SECRET \
  /paymentswitch/prod/POSTGRES_PASSWORD \
  /paymentswitch/prod/RABBITMQ_DEFAULT_PASS \
  --with-decryption --query "Parameters[*].{Name,Value}" \
  --output text | tee .env
```

Wire this into the deploy step so the compose stack always starts with the
values the store currently holds. Restrict access with an instance role or a
narrow IAM policy; do not store long-lived credentials on the host.

### Option B — Kubernetes (target platform)

- **External Secrets Operator**: define a `ClusterSecretStore` pointing at AWS
  Secrets Manager and a `ExternalSecret` that syncs each key into the
  `payment-switch-secret` Secret. Rotation then happens in the store; ESO
  propagates changes without a manual `kubectl patch`.
- **Sealed Secrets**: encrypt a manifest with `kubeseal` and commit it (it is
  decryptable only by the cluster's sealed-secrets controller).

## JWT secret rotation (dual-write window)

The shared `AddPaymentSwitchJwtBearer` extension
(`src/BuildingBlocks/BuildingBlocks.Shared/Auth/JwtBearerExtensions.cs`)
accepts `Jwt:Secret` (new) and `Jwt:PreviousSecret` (old) simultaneously, so
tokens signed with either key validate. This gives a zero-downtime rotation:

1. **Set new key + old key, redeploy.** `Jwt:Secret` = new secret;
   `Jwt:PreviousSecret` = old secret. Services start signing new tokens with
   the new key while still validating tokens from the old key.
2. **Wait out the window.** Longer than the maximum access-token lifetime (see
   `Jwt:AccessTokenExpirationMinutes`; default 60 min in Identity) so no issued
   token is orphaned.
3. **Clear the old key, redeploy.** Remove `Jwt:PreviousSecret` (or set it
   empty). Only the new key is accepted from then on.

Use the helper script for each runtime:

```bash
# Compose: moves the current JWT_SECRET to JWT_PREVIOUS_SECRET and sets the new one
./infra/secrets/rotate-jwt.sh '<new-32+-char-secret>' compose

# Kubernetes: patches the Secret with Jwt__PreviousSecret then Jwt__Secret
./infra/secrets/rotate-jwt.sh '<new-32+-char-secret>' k8s

# Helm: prints the --set flags for a helm upgrade
./infra/secrets/rotate-jwt.sh '<new-32+-char-secret>' helm
```

For Helm, set `config.jwt.previousSecret` on the first rotation step and clear
it on the second.

## Access-token `jti` and revocation

Access tokens (Identity `TokenService`) carry a unique `jti` (JWT ID) claim,
plus `iat`/`nbf`, and are validated with a strict HS256 algorithm binding
(`ValidAlgorithms`, shared `AddPaymentSwitchJwtBearer` extension). The `jti`
is the hook for server-side access-token revocation.

Revocation story:

- **Refresh tokens are the revocation channel.** Access tokens are short-lived
  (`Jwt:AccessTokenExpirationMinutes`, default 60) and are bound to a hashed
  refresh token; revoking the refresh token (logout / `RevokeRefreshToken`)
  terminates the session, so a leaked access token is only usable until expiry.
- **Immediate access-token kill** is a deliberate non-goal. There is no
  persisted token-version/blacklist table, so a compromised access token cannot
  be revoked before its natural expiry. If the risk profile demands it later,
  add a token-version claim (`tv`), bump it on revoke, and have
  `AddPaymentSwitchJwtBearer` reject tokens whose version is stale (requires
  a Redis/DB lookup in the JwtBearer events).
- **`jti` + logging.** Identity and other services can log `jti` (and
  `NameIdentifier`) at the auth boundary for audit; `jti` can later back a
  denylist without changing the token shape.

Rotation and revocation both live in the shared extension, so all six services
stay in lockstep.

## Postgres password rotation

1. Rotate the password in the managed store / parameter.
2. Run `ALTER USER paymentswitch WITH PASSWORD '<new>';` against Postgres.
3. Update all six connection strings (compose `POSTGRES_PASSWORD` or the k8s
   Secret `*Db__ConnectionString` entries) and redeploy.
4. Restart each service in a rolling fashion; the fail-fast secret validation
   (`ValidateSecuritySecrets`) ensures a stale/mismatched secret is caught at
   startup rather than mid-flight.

## Rotation runbook checklist

- [ ] Generate a new secret (`openssl rand -base64 32`).
- [ ] Rotate the value in the managed store first.
- [ ] Deploy with the dual-write window populated (JWT only).
- [ ] Verify `GET /identity/api/v1/health` and a real login succeed.
- [ ] After the window, clear the previous value and redeploy.
- [ ] Record the rotation date + reason in the ops log.
