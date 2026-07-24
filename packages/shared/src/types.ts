export interface MerchantDto {
  id: string
  businessName: string
  email: string
  status: "Active" | "Pending" | "Suspended"
  webhookUrl?: string | null
  enabledPaymentMethods?: string[]
  createdAt?: string
}

export interface PaymentIntentDto {
  intentId: string
  merchantId: string
  amount: number
  currency: string
  status: string
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
