# Messaging Registry — Event → Producer → Consumer

The single source of truth for every domain event routed through the RabbitMQ
outbox. Each event's fate is decided by TASK-027: **an event is published only
if a consumer delivers business value, otherwise it is removed from the bus**
(no dead messages).

## How the machine works

1. An EF Core `SaveChangesInterceptor` per service captures aggregate domain
   events during `SaveChanges` and writes transactional **outbox** rows
   (`OutboxMessage`). The C# class name **is** the RabbitMQ routing key.
2. `OutboxPublisherService` (polls every 2s) leases unprocessed rows and
   publishes to the service's topic exchange.
3. Consumers bind queues to exchanges, dedupe via an **inbox** table
   (`InboxMessage`), and on failure republish to a retry exchange (TTL 30s,
   max 3) then a DLX/DLQ. DLQ consumers persist every exhausted message.

## Exchanges

| Exchange | Declared by | Producer(s) | Consumers |
|---|---|---:|---|
| `payment.events` | Payment | Payment | Ledger, Notification |
| `merchant.events` | Merchant + Ledger consumer | Merchant | Ledger |
| `identity.events` | Identity | — (nothing published) | — |
| `ledger.events` | Ledger | — (nothing published) | — |
| `notification.events` | Notification | — (nothing published) | — |
| `settlement.events` | Settlement | — (nothing published) | — |

## Event matrix

Fate: **consumed** (has a live RabbitMQ consumer) · **removed** (no consumer;
no longer written to the outbox — either webhook-delivered in-process or of no
cross-service value).

### Payment → `payment.events`

| EventType (routing key) | Fate | Consumers | Notes |
|---|---|---|---|
| `PaymentIntentCreatedDomainEvent` | consumed | Notification | Merchant notified a payment started; webhook delivered too |
| `PaymentAuthorizedDomainEvent` | consumed | Ledger, Notification | Ledger creates account + reserves; webhook |
| `PaymentRequiresActionDomainEvent` | removed | — | Webhook-only; no cross-service consumer |
| `PaymentProcessingDomainEvent` | removed | — | Webhook-only; no cross-service consumer |
| `PaymentCapturedDomainEvent` | consumed | Ledger, Notification | Ledger captures; webhook |
| `PaymentVoidedDomainEvent` | consumed | Ledger, Notification | Ledger releases the reservation; merchant notified; webhook |
| `PaymentFailedDomainEvent` | consumed | Notification | Merchant notified a payment failed; webhook |
| `PaymentRefundedDomainEvent` | consumed | Ledger, Notification | Ledger refunds; webhook |
| `PaymentExpiredDomainEvent` | consumed | Notification | Merchant notified a payment expired; webhook |
| `SubscriptionCreatedDomainEvent` | removed | — | No consumer |
| `SubscriptionRenewedDomainEvent` | removed | — | No consumer |
| `SubscriptionPastDueDomainEvent` | removed | — | No consumer |
| `SubscriptionCanceledDomainEvent` | removed | — | No consumer |
| `InvoiceIssuedDomainEvent` | removed | — | No consumer |
| `InvoicePaidDomainEvent` | removed | — | No consumer |
| `InvoiceUncollectibleDomainEvent` | removed | — | No consumer |

### Merchant → `merchant.events`

| EventType | Fate | Consumers | Notes |
|---|---|---|---|
| `MerchantOnboardedEvent` | consumed | Ledger | Ledger provisions a default (USD) ledger account |
| `MerchantApprovedEvent` | removed | — | No consumer |
| `MerchantRejectedEvent` | removed | — | No consumer |
| `MerchantActivatedEvent` | removed | — | No consumer |
| `MerchantSuspendedEvent` | removed | — | No consumer |
| `MerchantConfigurationUpdatedEvent` | removed | — | No consumer |

### Identity → `identity.events`

| EventType | Fate | Consumers | Notes |
|---|---|---|---|
| `UserRegisteredDomainEvent` | removed | — | No consumer |
| `ApiKeyGeneratedDomainEvent` | removed | — | No consumer |
| `ApiKeyRevokedDomainEvent` | removed | — | No consumer |

### Ledger → `ledger.events`

| EventType | Fate | Consumers | Notes |
|---|---|---|---|
| `FundsReservedEvent` | removed | — | No consumer |
| `FundsCapturedEvent` | removed | — | No consumer |
| `FundsRefundedEvent` | removed | — | No consumer |
| `FeesChargedEvent` | removed | — | No consumer |

### Settlement → `settlement.events`

| EventType | Fate | Consumers | Notes |
|---|---|---|---|
| `SettlementBatchCreatedEvent` | removed | — | No consumer |
| `SettlementBatchCompletedEvent` | removed | — | Payload carries no merchant targeting (`BatchId, BatchDate, TotalAmount, Currency`); merchant notification deferred until the payload is enriched per-merchant |

### Notification → `notification.events`

| EventType | Fate | Consumers | Notes |
|---|---|---|---|
| `NotificationSentEvent` | removed | — | No consumer |
| `NotificationPermanentlyFailedEvent` | removed | — | No consumer |

## Consumer bindings (live)

| Queue | Source exchange | Routing keys | Consumer |
|---|---|---|---|
| `ledger.payment.events` | `payment.events` | `PaymentAuthorizedDomainEvent`, `PaymentCapturedDomainEvent`, `PaymentRefundedDomainEvent`, `PaymentVoidedDomainEvent` | Ledger `RabbitMQConsumerService` |
| `ledger.merchant.events` | `merchant.events` | `MerchantOnboardedEvent` | Ledger `MerchantEventConsumerService` |
| `notification.events` | `payment.events` | `PaymentAuthorizedDomainEvent`, `PaymentCapturedDomainEvent`, `PaymentRefundedDomainEvent`, `PaymentIntentCreatedDomainEvent`, `PaymentVoidedDomainEvent`, `PaymentFailedDomainEvent`, `PaymentExpiredDomainEvent` | Notification `RabbitMQConsumerService` |

## Implementation touchpoints

- Published-set filters: `src/Services/*/*/Outbox/OutboxInterceptor.cs`
  (`PublishedEventTypes`).
- New consumers: `Ledger.Infrastructure/Messaging/MerchantEventConsumerService.cs`,
  bindings added to `Notification.Infrastructure/Messaging/RabbitMQConsumerService.cs`.
- Merchant-visible notification event types:
  `Notification.Domain/NotificationEventTypes.cs`.

## Deferred (documented for future work)

- `SettlementBatchCompletedEvent` merchant notification — needs per-merchant
  payout detail in the event payload.
- `MerchantOnboardedEvent` → merchant "welcome" email (Notification), not yet
  implemented.
