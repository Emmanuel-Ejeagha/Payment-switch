"use client"

import { useEffect, useState } from "react"
import { Plus, CreditCard } from "lucide-react"
import type { PaymentIntentDto, MerchantDto, UserDto } from "@paymentswitch/shared"
import { useToast } from "@/components/toast"

export default function PaymentsPage() {
  const { showToast } = useToast()
  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [payments, setPayments] = useState<PaymentIntentDto[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)

  useEffect(() => {
    async function load() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) return
        const userData: UserDto = await userRes.json()

        const merchantRes = await fetch(`/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(userData.email)}`)
        if (!merchantRes.ok) return
        const merchantData: MerchantDto = await merchantRes.json()
        setMerchant(merchantData)

        const paymentsRes = await fetch(`/api/proxy/payment/api/v1/payments?merchantId=${merchantData.id}&skip=0&take=10`)
        if (paymentsRes.ok) setPayments(await paymentsRes.json())
      } catch {
        /* ignore */
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [])

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold">Payments</h1>
          <p className="text-sm text-muted-foreground">View and manage payment intents</p>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
        >
          <Plus className="h-4 w-4" />
          Create payment
        </button>
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-14 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : payments.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-xl border py-16 text-muted-foreground">
          <CreditCard className="h-8 w-8" />
          <p className="text-sm">No payments found</p>
        </div>
      ) : (
        <div className="overflow-x-auto rounded-xl border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <th className="px-6 py-3 text-left font-medium">Intent ID</th>
                <th className="px-6 py-3 text-left font-medium">Amount</th>
                <th className="px-6 py-3 text-left font-medium">Currency</th>
                <th className="px-6 py-3 text-left font-medium">Status</th>
                <th className="px-6 py-3 text-left font-medium">Transactions</th>
              </tr>
            </thead>
            <tbody>
              {payments.map((p) => (
                <tr key={p.intentId} className="border-b last:border-0 hover:bg-muted/50">
                  <td className="px-6 py-3 font-mono text-xs">{p.intentId.slice(0, 12)}...</td>
                  <td className="px-6 py-3">{(p.amount / 100).toFixed(2)}</td>
                  <td className="px-6 py-3">{p.currency}</td>
                  <td className="px-6 py-3">
                    <span className="inline-flex rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary">
                      {p.status}
                    </span>
                  </td>
                  <td className="px-6 py-3">{p.transactions?.length ?? 0}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showCreate && merchant && (
        <CreatePaymentModal
          merchantId={merchant.id}
          onClose={() => setShowCreate(false)}
          onCreated={async (intentId) => {
            const res = await fetch(`/api/proxy/payment/api/v1/payments/${intentId}`)
            if (res.ok) {
              const p = await res.json()
              setPayments((prev) => [p, ...prev])
              showToast("success", "Payment created successfully")
            }
            setShowCreate(false)
          }}
        />
      )}
    </div>
  )
}

function CreatePaymentModal({
  merchantId,
  onClose,
  onCreated,
}: {
  merchantId: string
  onClose: () => void
  onCreated: (intentId: string) => void
}) {
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("USD")
  const [paymentMethod, setPaymentMethod] = useState("Card")
  const [cardLastFour, setCardLastFour] = useState("")
  const [cardBrand, setCardBrand] = useState("")
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      const res = await fetch("/api/proxy/payment/api/v1/payments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId,
          amount: Math.round(parseFloat(amount) * 100),
          currency,
          paymentMethod,
          ...(paymentMethod === "Card" ? { cardLastFour, cardBrand } : {}),
          idempotencyKey: crypto.randomUUID(),
        }),
      })

      if (!res.ok) {
        const body = await res.json()
        setError(body.title ?? body.detail ?? "Failed to create payment")
        return
      }

      const { intentId } = await res.json()
      onCreated(intentId)
    } catch {
      setError("Failed to create payment")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={onClose}>
      <div className="w-full max-w-md rounded-xl bg-card p-6 shadow-lg" onClick={(e) => e.stopPropagation()}>
        <h2 className="text-lg font-semibold mb-4">Create payment</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Amount</label>
            <input
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              required
              className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">Currency</label>
            <select
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            >
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
              <option value="GBP">GBP</option>
              <option value="NGN">NGN</option>
            </select>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">Payment Method</label>
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            >
              <option value="Card">Card</option>
              <option value="Bank">Bank Transfer</option>
              <option value="MobileMoney">Mobile Money</option>
            </select>
          </div>
          {paymentMethod === "Card" && (
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <label className="text-sm font-medium">Card (last 4)</label>
                <input
                  type="text"
                  maxLength={4}
                  pattern="[0-9]{4}"
                  placeholder="1234"
                  value={cardLastFour}
                  onChange={(e) => setCardLastFour(e.target.value.replace(/\D/g, "").slice(0, 4))}
                  required
                  className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Card Brand</label>
                <select
                  value={cardBrand}
                  onChange={(e) => setCardBrand(e.target.value)}
                  className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
                >
                  <option value="">Select</option>
                  <option value="Visa">Visa</option>
                  <option value="Mastercard">Mastercard</option>
                  <option value="Amex">Amex</option>
                </select>
              </div>
            </div>
          )}
          {error && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}
          <div className="flex gap-3">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-lg border border-input px-4 py-2 text-sm font-medium hover:bg-accent"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              {submitting ? "Creating..." : "Create"}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
