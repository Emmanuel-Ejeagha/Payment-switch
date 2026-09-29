# Webhook Contract — Payment → Merchant

Payment Switch delivers **outbound** webhooks to a merchant's configured
endpoint whenever a payment intent transitions through its lifecycle. This
document is the contract for those deliveries and for the merchant-portal
replay/test workflow. See `src/Services/Payment/Payment.Infrastructure/Services/WebhookDispatcher.cs`
for the producer side.

## Delivery

Payment `POST`s the event payload as JSON to the merchant's configured
`WebhookUrl` (set via
`PUT /api/v1/merchants/{id}/configuration`).

### Headers

| Header | Example | Meaning |
|---|---|---|
| `Content-Type` | `application/json` | Payload MIME type |
| `X-PaymentSwitch-Signature` | `sha256=1c2b…` | HMAC-SHA256 signature (see below) |
| `X-PaymentSwitch-Timestamp` | `1755500000` | Unix seconds at signing time |
| `X-PaymentSwitch-Event` | `PaymentCapturedDomainEvent` | Event type (also the payload `type`) |
| `X-PaymentSwitch-Delivery` | `<webhook_event_uuid>` | Delivery id (idempotency key) |

### Signature scheme

The signature is an HMAC-SHA256 computed over the string
`"<timestamp>.<base64(payload)>"` using the merchant's webhook signing secret,
then hex-encoded (lowercase) and prefixed with `sha256=`:

```
signature = "sha256=" + hex( HMAC-SHA256( secret, timestamp + "." + base64(payload) ) )
```

- `timestamp` is the value of `X-PaymentSwitch-Timestamp` (Unix seconds).
- `payload` is the exact raw HTTP body bytes.
- The signing secret is the merchant's webhook secret. After a secret rotation
  (see `docs/secrets.md`, TASK-006) deliveries are signed with the **previous**
  secret during the rotation grace window, so consumers must keep verifying
  against both the current and previous secret until the old one expires.

### Retry policy

Deliveries are retried by `WebhookDispatchWorker` up to **8 attempts** with
exponential backoff: `delay = min(2^attempt, 300)` seconds (attempts 1-indexed,
capped at 5 minutes). A delivery is marked `Failed` and rescheduled while
`attempts < 8`; after the 8th failure it stays failed until manually replayed
from the merchant dashboard. Any HTTP status outside 2xx counts as a failure;
connection errors count too.

### Idempotency

Deliveries carry `X-PaymentSwitch-Delivery` (the webhook event id). Consumers
should dedupe on this value — the same logical event is re-delivered verbatim
on retry and on replay (payload byte-identical, same delivery id).

## Event types

| `X-PaymentSwitch-Event` | When |
|---|---|
| `PaymentIntentCreatedDomainEvent` | Intent created |
| `PaymentAuthorizedDomainEvent` | Authorization approved |
| `PaymentRequiresActionDomainEvent` | 3DS / action required |
| `PaymentProcessingDomainEvent` | Processing |
| `PaymentCapturedDomainEvent` | Capture succeeded |
| `PaymentRefundedDomainEvent` | Refund applied |
| `PaymentVoidedDomainEvent` | Void succeeded |

## Payload schema

The body is the JSON-serialized domain event. All events share a shape:

```json
{
  "intentId": "…",
  "merchantId": "…",
  "amount": 1000,
  "currency": "USD",
  "status": "Captured",
  "occurredAtUtc": "2026-08-17T12:00:00Z",
  "id": "…"
}
```

Fields vary by event; treat unknown fields as forward-compatible. `amount` is
always an integer in minor units (never a float).

## Verification sample (C#)

Merchants verify a delivery by recomputing the signature **over the
sender-supplied timestamp** with their secret and comparing in constant time.
Reference implementation (mirrors `WebhookSignature.VerifyWithRotation` +
`WebhookReceiver.Validate` in `Payment.Infrastructure.Services`):

```csharp
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

static string Sign(string secret, byte[] payload, string timestamp)
{
    var body = Encoding.UTF8.GetBytes($"{timestamp}.{Convert.ToBase64String(payload)}");
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    return "sha256=" + Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();
}

static bool Verify(string secret, byte[] payload, string timestamp, string signature,
    TimeSpan maxAge)
{
    // 1. timestamp freshness: reject if |now - timestamp| > tolerance (5 min).
    if (!long.TryParse(timestamp, out var ts)) return false;
    if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > maxAge.TotalSeconds)
        return false;
    // 2. signature match (constant time) over the SUPPLIED timestamp.
    var expected = Sign(secret, payload, timestamp);
    if (expected.Length != signature.Length || !signature.StartsWith("sha256="))
        return false;
    try
    {
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(expected[7..]),
            Convert.FromHexString(signature[7..]));
    }
    catch (FormatException) { return false; }
}

// Recommended checks, in order:
// 1. freshness: reject if |now - timestamp| > 5 min (both past and future).
// 2. signature against current secret, then previous secret while inside the
//    rotation grace window (deliveries are signed with the previous secret
//    during the window — see docs/secrets.md / TASK-006).
// 3. idempotency: skip already-processed X-PaymentSwitch-Delivery ids. Claim
//    the id ONLY after the signature verifies, and persist it (DB/Redis) so a
//    restart cannot reprocess. The in-memory reference is
//    `InMemoryDeliveryIdStore` (72h TTL, covers retry + manual replay).
```

The repository exposes `WebhookSignature.Verify` (single secret, 5-minute
default window), `WebhookSignature.VerifyWithRotation` (current + previous
with grace), and `WebhookReceiver.Validate` (freshness + dual-secret +
delivery-id claim in one call), all exercised by unit tests. `Compute` stamps
"now"; verification always recomputes over the supplied timestamp via
`ComputeWithTimestamp` — never compare against a freshly-stamped signature.

## Replay and test (merchant dashboard)

- `GET /api/v1/webhookevents?merchantId=…` — list delivery events (paginated).
- `POST /api/v1/webhookevents/{id}/replay?merchantId=…` — reset a delivery to
  `Pending` for redelivery (attempts reset to 0).
- `POST /api/v1/webhookevents/test?merchantId=…` — enqueue a synthetic
  `test.event` delivery to the configured endpoint.

All three endpoints are **owner-scoped**: the caller must be the merchant's
owner or an admin (verified against the Identity `owner_id` via the Merchant
gRPC `GetMerchantContact`). Replay and test are **audited**: each action emits
a structured warning log carrying the delivery id, merchant id, and the acting
caller's email and user id.

## Delivery-status visibility

The merchant dashboard (`apps/merchant/.../webhooks/page.tsx`) lists deliveries
with status (`Pending`/`Succeeded`/`Failed`), attempt count, last error, and
timestamps, plus replay and test actions.