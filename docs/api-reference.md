# API Reference

All endpoints return JSON. Authentication uses JWT Bearer tokens obtained from the Identity service.

## Identity Service

| Method | Endpoint                            | Auth     | Description                  |
|--------|-------------------------------------|----------|------------------------------|
| POST   | `/api/v1/auth/register`             | None     | Register a new user          |
| POST   | `/api/v1/auth/login`                | None     | Login, returns tokens        |
| POST   | `/api/v1/auth/refresh`              | None     | Refresh access token         |
| POST   | `/api/v1/auth/revoke`               | None     | Revoke a refresh token       |
| GET    | `/api/v1/users/me`                  | User     | Get current user profile     |
| POST   | `/api/v1/api-keys`                  | User     | Generate an API key          |
| GET    | `/api/v1/api-keys`                  | User     | List API keys                |
| DELETE | `/api/v1/api-keys/{id}`             | User     | Revoke an API key            |
| POST   | `/api/v1/admin/roles`               | Admin    | Assign a role to a user      |

## Merchant Service

| Method | Endpoint                              | Auth   | Description                    |
|--------|---------------------------------------|--------|--------------------------------|
| POST   | `/api/v1/merchants`                   | None   | Onboard a new merchant         |
| GET    | `/api/v1/merchants/{id}`              | User   | Get merchant details           |
| GET    | `/api/v1/merchants/by-email/{email}`  | User   | Get merchant by email          |
| POST   | `/api/v1/merchants/{id}/activate`     | Admin  | Activate a pending merchant    |
| POST   | `/api/v1/merchants/{id}/suspend`      | Admin  | Suspend an active merchant     |
| PUT    | `/api/v1/merchants/{id}/configuration`| User   | Update webhook/payment methods |
| GET    | `/api/v1/merchants`                   | Admin  | List merchants (paginated)     |
| POST   | `/api/v1/merchants/{id}/apikeys`      | User   | Generate a secret key (`sk_live_...` / `sk_test_...`) |
| GET    | `/api/v1/merchants/{id}/apikeys`      | User   | List secret keys (never returns the secret) |
| DELETE | `/api/v1/merchants/{id}/apikeys/{keyId}` | User | Revoke a secret key          |

## Payment Service

| Method | Endpoint                                | Auth | Description                     |
|--------|-----------------------------------------|------|---------------------------------|
| POST   | `/api/v1/payments`                      | User | Create a payment intent         |
| POST   | `/api/v1/payments/{id}/authorize`       | User | Authorize a payment             |
| POST   | `/api/v1/payments/{id}/capture`         | User | Capture an authorized payment   |
| POST   | `/api/v1/payments/{id}/void`            | User | Void an authorized payment      |
| POST   | `/api/v1/payments/{id}/refund`          | User | Refund a captured payment       |
| GET    | `/api/v1/payments/{id}`                 | User | Get payment intent details      |
| GET    | `/api/v1/payments`                      | User | List payments for a merchant    |

### Public Payments API (secret-key authentication)

Authenticate with `Authorization: Bearer sk_live_...` or `sk_test_...`. The merchant is resolved from the key — never pass `merchantId`. Keys are validated against the Merchant service and cached for 5 minutes.

| Method | Endpoint                  | Auth          | Description                              |
|--------|---------------------------|---------------|------------------------------------------|
| POST   | `/v1/payments/intents`    | Secret key    | Create a payment intent                  |
| GET    | `/v1/payments/{id}`       | Secret key    | Get payment intent details (own merchant only) |

`POST /v1/payments/intents` request body:

```json
{
  "amount": 100.00,
  "currency": "USD",
  "paymentMethod": "Card",
  "cardLastFour": "4242",
  "cardBrand": "Visa"
}
```

Pass an idempotency key via the `Idempotency-Key` header to prevent duplicate intents.

## Ledger Service

| Method | Endpoint                                      | Auth | Description                     |
|--------|-----------------------------------------------|------|---------------------------------|
| GET    | `/api/v1/ledger/balance?merchantId={id}`       | User | Get merchant balances           |
| GET    | `/api/v1/ledger/balances?merchantId={id}`      | User | Get multi-currency balances     |
| GET    | `/api/v1/ledger/transactions?merchantId={id}`  | User | Get transaction history (paged) |

## Notification Service

| Method | Endpoint                                   | Auth  | Description                     |
|--------|--------------------------------------------|-------|---------------------------------|
| GET    | `/api/v1/notifications/{id}`               | Admin | Get a single notification       |
| GET    | `/api/v1/notifications`                    | Admin | List notifications (filtered)   |

## Settlement Service

| Method | Endpoint                          | Auth  | Description                     |
|--------|-----------------------------------|-------|---------------------------------|
| POST   | `/api/v1/settlement/trigger`      | Admin | Trigger a settlement batch      |
| GET    | `/api/v1/settlement/{id}`         | Admin | Get settlement batch details    |
| GET    | `/api/v1/settlement`              | Admin | List settlement batches         |
