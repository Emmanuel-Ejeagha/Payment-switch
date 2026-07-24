"use client"

import { useEffect, useState } from "react"
import { ChevronLeft, ChevronRight, CreditCard } from "lucide-react"
import Link from "next/link"
import type { MerchantDto, PaymentIntentDto } from "@paymentswitch/shared"
import { StatusBadge } from "@paymentswitch/ui"

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

const statuses = [
  "All",
  "Pending",
  "Authorized",
  "Captured",
  "PartiallyCaptured",
  "Voided",
  "Failed",
  "PartiallyRefunded",
  "FullyRefunded",
]

function formatAmount(amount: number, currency: string) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: currency || "USD",
  }).format(amount)
}

export default function PaymentsPage() {
  const [merchants, setMerchants] = useState<MerchantDto[]>([])
  const [payments, setPayments] = useState<PaymentIntentDto[]>([])
  const [selectedMerchantId, setSelectedMerchantId] = useState<string>("")
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState("All")
  const [skip, setSkip] = useState(0)
  const take = 10

  useEffect(() => {
    async function loadMerchants() {
      const res = await fetch("/api/proxy/merchant/api/v1/merchants?skip=0&take=100")
      if (res.ok) {
        const list: MerchantDto[] = await res.json()
        setMerchants(list)
        if (list.length > 0 && !selectedMerchantId) {
          setSelectedMerchantId(list[0].id)
        }
      }
    }
    loadMerchants()
  }, [])

  useEffect(() => {
    if (!selectedMerchantId) return

    async function loadPayments() {
      setLoading(true)
      setError(null)
      try {
        const params = new URLSearchParams({
          merchantId: selectedMerchantId,
          skip: String(skip),
          take: String(take),
        })

        const res = await fetch(`/api/proxy/payment/api/v1/payments?${params}`)

        if (res.status === 403) {
          setError("You need admin access to view payments.")
          setPayments([])
          return
        }
        if (!res.ok) {
          setError(`Request failed (${res.status})`)
          setPayments([])
          return
        }

        const data: PaymentIntentDto[] = await res.json()
        setPayments(data)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load payments")
      } finally {
        setLoading(false)
      }
    }

    loadPayments()
  }, [selectedMerchantId, skip])

  const filtered = payments.filter((p) =>
    statusFilter === "All" ? true : p.status === statusFilter,
  )

  const selectedMerchant = merchants.find((m) => m.id === selectedMerchantId)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Payments</h1>
        <p className="text-sm text-muted-foreground">
          View and manage payment intents
        </p>
      </div>

      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <select
          value={selectedMerchantId}
          onChange={(e) => { setSelectedMerchantId(e.target.value); setSkip(0); setStatusFilter("All") }}
          className="flex h-10 max-w-sm rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          {merchants.length === 0 && (
            <option value="">No merchants available</option>
          )}
          {merchants.map((m) => (
            <option key={m.id} value={m.id}>
              {m.businessName}
            </option>
          ))}
        </select>

        <div className="flex flex-wrap gap-2">
          {["All", "Pending", "Authorized", "Captured"].map((s) => (
            <button
              key={s}
              onClick={() => { setStatusFilter(s); setSkip(0) }}
              className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
                statusFilter === s
                  ? "bg-primary text-primary-foreground"
                  : "bg-muted text-muted-foreground hover:bg-accent"
              }`}
            >
              {s}
            </button>
          ))}
        </div>
      </div>

      {!selectedMerchantId && merchants.length === 0 && !loading ? (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-16 text-center">
          <CreditCard className="mb-3 h-8 w-8 text-muted-foreground" />
          <p className="font-medium">No merchants found</p>
          <p className="text-sm text-muted-foreground">
            Create a merchant to view payments
          </p>
        </div>
      ) : loading ? (
        <div className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : error ? (
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">{error}</p>
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-16 text-center">
          <CreditCard className="mb-3 h-8 w-8 text-muted-foreground" />
          <p className="font-medium">No payments found</p>
          <p className="text-sm text-muted-foreground">
            {selectedMerchant?.businessName
              ? `No payments for ${selectedMerchant.businessName}`
              : "Select a merchant to view payments"}
          </p>
        </div>
      ) : (
        <>
          <div className="overflow-hidden rounded-xl border">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50 text-left text-sm text-muted-foreground">
                  <th className="px-6 py-3 font-medium">Merchant</th>
                  <th className="px-6 py-3 font-medium">Amount</th>
                  <th className="px-6 py-3 font-medium">Status</th>
                  <th className="px-6 py-3 font-medium">Date</th>
                  <th className="px-6 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                  {filtered.map((p) => {
                    const created = p.transactions?.find((t) => t.type === "Created")?.timestamp
                    return (
                      <tr key={p.intentId} className="text-sm transition-colors hover:bg-muted/30">
                        <td className="px-6 py-4 font-medium">
                          {selectedMerchant?.businessName || p.merchantId.slice(0, 8)}
                        </td>
                        <td className="px-6 py-4">
                          {formatAmount(p.amount, p.currency)}
                        </td>
                        <td className="px-6 py-4">
                          <span
                            className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                              statusColors[p.status] || "bg-muted text-muted-foreground"
                            }`}
                          >
                            {p.status}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-muted-foreground">
                          {created ? new Date(created).toLocaleDateString() : "-"}
                        </td>
                        <td className="px-6 py-4">
                          <Link
                            href={`/payments/${p.intentId}`}
                            className="text-primary hover:underline text-xs font-medium"
                          >
                            View details
                          </Link>
                        </td>
                      </tr>
                    )
                  })}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <p>
              Showing {skip + 1}–{skip + filtered.length}
            </p>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setSkip(Math.max(0, skip - take))}
                disabled={skip === 0}
                className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors hover:bg-accent disabled:pointer-events-none disabled:opacity-50"
              >
                <ChevronLeft className="h-4 w-4" /> Previous
              </button>
              <button
                onClick={() => setSkip(skip + take)}
                disabled={filtered.length < take}
                className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors hover:bg-accent disabled:pointer-events-none disabled:opacity-50"
              >
                Next <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
