"use client"

import { useEffect, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import { ArrowLeft, CheckCircle, XCircle, Ban, RotateCcw } from "lucide-react"
import type { PaymentIntentDto, TransactionDto } from "@paymentswitch/shared"

export default function PaymentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const [payment, setPayment] = useState<PaymentIntentDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [pendingAction, setPendingAction] = useState<string | null>(null)

  const fetchPayment = async () => {
    setLoading(true)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/payments/${id}`)
      if (!res.ok) { setError("Payment not found"); return }
      setPayment(await res.json())
    } catch {
      setError("Failed to load payment")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchPayment() }, [id])

  const doAction = async (action: string, extra?: Record<string, unknown>) => {
    // These endpoints move money. Without the in-flight guard a double-click sends two
    // refunds, and without the idempotency key a retried request is a second refund too.
    if (pendingAction) return
    setPendingAction(action)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/payments/${id}/${action}`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Idempotency-Key": crypto.randomUUID(),
        },
        body: extra ? JSON.stringify(extra) : undefined,
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? `${action} failed`)
        return
      }
      await fetchPayment()
    } catch {
      setError(`${action} failed`)
    } finally {
      setPendingAction(null)
    }
  }

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-48 animate-pulse rounded bg-muted" />
        <div className="h-64 animate-pulse rounded-xl bg-muted" />
      </div>
    )
  }

  if (error && !payment) {
    return (
      <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
        <p className="font-medium">{error}</p>
      </div>
    )
  }

  if (!payment) return null

  const canAuthorize = payment.status === "Pending"
  const canCapture = payment.status === "Authorized"
  const canVoid = payment.status === "Authorized"
  const canRefund = payment.status === "Captured" || payment.status === "Settled"

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <button onClick={() => router.push("/payments")} className="rounded-lg p-1 hover:bg-accent">
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-3xl font-semibold">Payment detail</h1>
          <p className="font-mono text-sm text-muted-foreground">{payment.intentId}</p>
        </div>
      </div>

      {error && (
        <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
      )}

      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-xl border bg-card p-6">
          <p className="text-sm text-muted-foreground">Amount</p>
          <p className="text-2xl font-semibold">{(payment.amount / 100).toFixed(2)} {payment.currency}</p>
        </div>
        <div className="rounded-xl border bg-card p-6">
          <p className="text-sm text-muted-foreground">Status</p>
          <span className="inline-flex rounded-full bg-primary/10 px-3 py-1 text-sm font-medium text-primary">
            {payment.status}
          </span>
        </div>
        <div className="rounded-xl border bg-card p-6">
          <p className="text-sm text-muted-foreground">Transactions</p>
          <p className="text-2xl font-semibold">{payment.transactions?.length ?? 0}</p>
        </div>
      </div>

      <div className="flex flex-wrap gap-3">
        {canAuthorize && (
          <ActionButton
            icon={CheckCircle}
            label="Authorize"
            pending={pendingAction === "authorize"}
            disabled={pendingAction !== null}
            onClick={() => doAction("authorize", { cardLastFour: payment.cardLastFour, cardBrand: payment.cardBrand })}
          />
        )}
        {canCapture && (
          <ActionButton
            icon={CheckCircle}
            label="Capture"
            pending={pendingAction === "capture"}
            disabled={pendingAction !== null}
            onClick={() => doAction("capture", { amount: payment.amount })}
          />
        )}
        {canVoid && (
          <ActionButton
            icon={Ban}
            label="Void"
            variant="destructive"
            pending={pendingAction === "void"}
            disabled={pendingAction !== null}
            onClick={() => doAction("void")}
          />
        )}
        {canRefund && (
          <ActionButton
            icon={RotateCcw}
            label="Refund"
            variant="destructive"
            pending={pendingAction === "refund"}
            disabled={pendingAction !== null}
            onClick={() => doAction("refund", { amount: payment.amount })}
          />
        )}
      </div>

      <div className="rounded-xl border bg-card">
        <h2 className="border-b px-6 py-4 font-semibold">Transaction history</h2>
        {(!payment.transactions || payment.transactions.length === 0) ? (
          <p className="px-6 py-8 text-sm text-muted-foreground">No transactions yet</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="px-6 py-3 text-left font-medium">Type</th>
                  <th className="px-6 py-3 text-left font-medium">Amount</th>
                  <th className="px-6 py-3 text-left font-medium">Currency</th>
                  <th className="px-6 py-3 text-left font-medium">Timestamp</th>
                </tr>
              </thead>
              <tbody>
                {payment.transactions.map((t: TransactionDto) => (
                  <tr key={t.id} className="border-b last:border-0 hover:bg-muted/50">
                    <td className="px-6 py-3">{t.type}</td>
                    <td className="px-6 py-3">{(t.amount / 100).toFixed(2)}</td>
                    <td className="px-6 py-3">{t.currency}</td>
                    <td className="px-6 py-3">{new Date(t.timestamp).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

function ActionButton({
  icon: Icon,
  label,
  variant,
  pending,
  disabled,
  onClick,
}: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  variant?: "destructive"
  pending?: boolean
  disabled?: boolean
  onClick: () => void
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      aria-busy={pending}
      className={`inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-colors hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:bg-transparent ${
        variant === "destructive" ? "border-destructive/50 text-destructive hover:bg-destructive/10" : ""
      }`}
    >
      <Icon className={`h-4 w-4 ${pending ? "animate-spin" : ""}`} />
      {pending ? `${label}...` : label}
    </button>
  )
}
