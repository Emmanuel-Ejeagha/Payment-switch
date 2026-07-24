"use client"

import { useEffect, useState } from "react"
import { useParams } from "next/navigation"
import { ArrowLeft, CreditCard, Check, X, RefreshCw, Ban } from "lucide-react"
import Link from "next/link"
import type { PaymentIntentDto, TransactionDto } from "@paymentswitch/shared"

const statusColors: Record<string, string> = {
  Pending: "bg-slate-500/10 text-slate-600",
  Authorized: "bg-blue-500/10 text-blue-600",
  Captured: "bg-emerald-500/10 text-emerald-600",
  PartiallyCaptured: "bg-teal-500/10 text-teal-600",
  Voided: "bg-orange-500/10 text-orange-600",
  Failed: "bg-red-500/10 text-red-600",
  PartiallyRefunded: "bg-amber-500/10 text-amber-600",
  FullyRefunded: "bg-purple-500/10 text-purple-600",
}

const timelineIcons: Record<string, typeof Check> = {
  Created: CreditCard,
  Authorize: Check,
  Capture: RefreshCw,
  Refund: RefreshCw,
  Void: Ban,
  Fail: X,
}

const timelineColors: Record<string, string> = {
  Created: "bg-slate-500",
  Authorize: "bg-blue-500",
  Capture: "bg-emerald-500",
  Refund: "bg-amber-500",
  Void: "bg-orange-500",
  Fail: "bg-red-500",
}

function formatAmount(amount: number, currency: string) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: currency || "USD",
  }).format(amount)
}

export default function PaymentDetailPage() {
  const params = useParams()
  const id = params.id as string

  const [payment, setPayment] = useState<PaymentIntentDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const res = await fetch(`/api/proxy/payment/api/v1/payments/${id}`)
        if (!res.ok) {
          setError(`Request failed (${res.status})`)
          return
        }
        setPayment(await res.json())
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load payment")
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [id])

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-48 animate-pulse rounded bg-muted" />
        <div className="h-64 animate-pulse rounded-xl bg-muted" />
      </div>
    )
  }

  if (error || !payment) {
    return (
      <div className="space-y-6">
        <Link href="/payments" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" /> Back to payments
        </Link>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">{error || "Payment not found"}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <Link href="/payments" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="h-4 w-4" /> Back to payments
      </Link>

      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-semibold">
            {formatAmount(payment.amount, payment.currency)}
          </h1>
          <p className="text-sm text-muted-foreground">
            ID: {payment.intentId}
          </p>
        </div>
        <span
          className={`rounded-full px-3 py-1 text-sm font-medium ${
            statusColors[payment.status] || "bg-muted text-muted-foreground"
          }`}
        >
          {payment.status}
        </span>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <div className="rounded-xl border p-6">
          <h2 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wider">
            Details
          </h2>
          <dl className="space-y-3 text-sm">
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Merchant ID</dt>
              <dd className="font-medium">{payment.merchantId}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Currency</dt>
              <dd className="font-medium">{payment.currency}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Amount</dt>
              <dd className="font-medium">
                {formatAmount(payment.amount, payment.currency)}
              </dd>
            </div>
          </dl>
        </div>

        <div className="rounded-xl border p-6">
          <h2 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wider">
            Timeline
          </h2>
          <div className="space-y-0">
            {(payment.transactions || []).map((t, i) => {
              const Icon = timelineIcons[t.type] || CreditCard
              const color = timelineColors[t.type] || "bg-slate-500"
              return (
                <div key={t.id} className="relative flex gap-4 pb-6 last:pb-0">
                  {i < (payment.transactions?.length || 0) - 1 && (
                    <div className="absolute left-[15px] top-8 bottom-0 w-px bg-border" />
                  )}
                  <div className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${color}`}>
                    <Icon className="h-4 w-4 text-white" />
                  </div>
                  <div className="flex-1 pt-1">
                    <p className="text-sm font-medium">{t.type}</p>
                    <p className="text-xs text-muted-foreground">
                      {formatAmount(t.amount, t.currency)} &middot;{" "}
                      {new Date(t.timestamp).toLocaleString()}
                    </p>
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </div>
    </div>
  )
}
