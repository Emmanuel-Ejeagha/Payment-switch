export interface MerchantDto {
  id: string
  businessName: string
  email: string
  status: "Active" | "Pending" | "Suspended"
  webhookUrl?: string | null
  enabledPaymentMethods?: string[]
  autoCapture?: boolean
  createdAt?: string
}

export interface PaymentIntentDto {
  intentId: string
  merchantId: string
  amount: number
  currency: string
  status: string
  cardLastFour?: string | null
  cardBrand?: string | null
  createdAt?: string
  transactions: TransactionDto[]
}

export interface TransactionDto {
  id: string
  type: string
  amount: number
  currency: string
  timestamp: string
}

export interface BalanceDto {
  merchantId: string
  available: number
  pending: number
  reserved: number
  currency: string
}

export interface LedgerTransactionDto {
  id: string
  type: string
  amount: number
  currency: string
  description: string
  timestamp: string
}

export interface PayoutDto {
  merchantId: string
  grossVolume: number
  fees: number
  netAmount: number
  currency: string
}

export interface SettlementBatchDto {
  id: string
  batchDate: string
  status: string
  totalAmount: number
  payouts: PayoutDto[]
}

export interface NotificationDto {
  id: string
  recipient: string
  channel: string
  subject?: string
  body?: string
  webhookUrl?: string
  status: string
  retryCount: number
  nextRetryAt?: string
  createdAt: string
  processedAt?: string
}

export interface ApiKeyDto {
  keyId: string
  environment: string
  createdAt: string
  revokedAt?: string | null
}

export interface UserDto {
  id: string
  email: string
  fullName: string
  isActive: boolean
  roles: string[]
}

export interface PaymentLinkDto {
  id: string
  amount: number
  currency: string
  code: string
  description?: string
  active: boolean
  createdAt?: string
}

export interface CheckoutPaymentDto {
  intentId: string
  status: string
  clientSecret?: string | null
}

export interface CustomerDto {
  id: string
  merchantId: string
  code: string
  email: string
  name?: string | null
  phone?: string | null
  description?: string | null
  createdAt: string
  updatedAt?: string | null
}

export interface PlanDto {
  id: string
  merchantId: string
  code: string
  name: string
  amount: number
  currency: string
  intervalUnit: string
  intervalCount: number
  description?: string | null
  active: boolean
  createdAt: string
}

export interface SubscriptionDto {
  id: string
  merchantId: string
  customerId: string
  planId: string
  code: string
  status: string
  currentPeriodStart: string
  currentPeriodEnd: string
  nextBillingAt?: string | null
  cancelAtPeriodEnd: boolean
  canceledAt?: string | null
  createdAt: string
}

export interface InvoiceDto {
  id: string
  merchantId: string
  customerId: string
  subscriptionId: string
  code: string
  amount: number
  currency: string
  status: string
  periodStart: string
  periodEnd: string
  paymentIntentId?: string | null
  attemptCount: number
  lastError?: string | null
  paidAt?: string | null
  createdAt: string
}

export interface WebhookEventDto {
  id: string
  merchantId: string
  eventType: string
  payload: string
  status: string
  deliveredAt?: string | null
  failureReason?: string | null
  attemptCount: number
  nextRetryAt?: string | null
  createdAt: string
}
