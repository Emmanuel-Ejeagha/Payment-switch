"use client"

import { useEffect, useState, useCallback } from "react"
import { RefreshCw, X, Ban, Repeat } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { SubscriptionDto, CustomerDto, PlanDto } from "@paymentswitch/shared"

function extractError(body: unknown): string {
  if (!body || typeof body !== "object") return "Request failed"
  const b = body as Record<string, unknown>
  if (typeof b.message === "string") return b.message
  if (typeof b.detail === "string") return b.detail
  if (b.errors && typeof b.errors === "object") {
    const parts = Object.values(b.errors as Record<string, unknown>).flatMap((v) =>
      Array.isArray(v) ? v.map(String) : [String(v)]
    )
    if (parts.length) return parts.join(" ")
  }
  if (typeof b.title === "string") return b.title
  return "Request failed"
}

export default function SubscriptionsPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const [subscriptions, setSubscriptions] = useState<SubscriptionDto[]>([])
  const [customers, setCustomers] = useState<CustomerDto[]>([])
  const [plans, setPlans] = useState<PlanDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const [showForm, setShowForm] = useState(false)
  const [customerId, setCustomerId] = useState("")
  const [planId, setPlanId] = useState("")
  const [cardToken, setCardToken] = useState("")

  const loadSubscriptions = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/subscriptions?merchantId=${merchantId}&skip=0&take=100`)
    if (res.ok) setSubscriptions(await res.json())
  }, [])

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    let cancelled = false
    async function load() {
      try {
        const [subsRes, custRes, plansRes] = await Promise.all([
          fetch(`/api/proxy/payment/api/v1/subscriptions?merchantId=${merchantId}&skip=0&take=100`),
          fetch(`/api/proxy/payment/api/v1/customers?merchantId=${merchantId}&skip=0&take=100`),
          fetch(`/api/proxy/payment/api/v1/plans?merchantId=${merchantId}&skip=0&take=100`),
        ])
        if (cancelled) return
        if (subsRes.ok) setSubscriptions(await subsRes.json())
        if (custRes.ok) setCustomers(await custRes.json())
        if (plansRes.ok) setPlans(await plansRes.json().then((p) => p.filter((x: PlanDto) => x.active)))
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load subscriptions")
      } finally {
        if (!cancelled) setDataReady(true)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [merchantLoading, merchantId])

  const createSubscription = async () => {
    if (!merchantId || !customerId || !planId || !cardToken) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch("/api/proxy/payment/api/v1/subscriptions", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ merchantId, customerId, planId, cardToken }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => undefined)
        setError(extractError(body))
        return
      }
      await loadSubscriptions(merchantId)
      setShowForm(false)
      setCustomerId("")
      setPlanId("")
      setCardToken("")
    } finally {
      setWorking(false)
    }
  }

  const cancelSubscription = async (id: string, atPeriodEnd: boolean) => {
    if (!merchantId || !confirm(atPeriodEnd ? "Cancel at period end?" : "Cancel immediately?")) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/subscriptions/${id}/cancel?atPeriodEnd=${atPeriodEnd}`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => undefined)
        setError(extractError(body))
        return
      }
      await loadSubscriptions(merchantId)
    } finally {
      setWorking(false)
    }
  }

  const collectNow = async (id: string) => {
    if (!merchantId) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/subscriptions/${id}/collect`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => undefined)
        setError(extractError(body))
        return
      }
      await loadSubscriptions(merchantId)
    } finally {
      setWorking(false)
    }
  }

  if (loading) {
    return <div className="h-64 animate-pulse rounded-xl bg-muted" />
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold">Subscriptions</h1>
          <p className="text-sm text-muted-foreground">Recurring billing subscriptions for your customers.</p>
        </div>
        <button
          onClick={() => setShowForm((v) => !v)}
          disabled={!merchantId}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
        >
          {showForm ? <X className="h-4 w-4" /> : <RefreshCw className="h-4 w-4" />}
          {showForm ? "Cancel" : "New subscription"}
        </button>
      </div>

      {(error || merchantError) && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error || merchantError}</div>}

      {showForm && (
        <div className="rounded-xl border bg-card p-6">
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Customer <span className="text-destructive">*</span></label>
              {customers.length === 0 ? (
                <div className="rounded-lg border border-dashed bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                  No customers yet.{" "}
                  <a href="/customers" className="font-medium text-primary underline">Create one</a> before adding a subscription.
                </div>
              ) : (
                <select
                  value={customerId}
                  onChange={(e) => setCustomerId(e.target.value)}
                  className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                >
                  <option value="">Select customer</option>
                  {customers.map((c) => (
                    <option key={c.id} value={c.id}>{c.email} ({c.code})</option>
                  ))}
                </select>
              )}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Plan <span className="text-destructive">*</span></label>
              {plans.length === 0 ? (
                <div className="rounded-lg border border-dashed bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                  No active plans yet.{" "}
                  <a href="/plans" className="font-medium text-primary underline">Create one</a> before adding a subscription.
                </div>
              ) : (
                <select
                  value={planId}
                  onChange={(e) => setPlanId(e.target.value)}
                  className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                >
                  <option value="">Select plan</option>
                  {plans.map((p) => (
                    <option key={p.id} value={p.id}>{p.name} — {(p.amount / 100).toFixed(2)} {p.currency}</option>
                  ))}
                </select>
              )}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Card token <span className="text-destructive">*</span></label>
              <input
                type="text"
                value={cardToken}
                onChange={(e) => setCardToken(e.target.value)}
                placeholder="tok_..."
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm font-mono outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>
          <div className="mt-4">
            <button
              onClick={createSubscription}
              disabled={working || !customerId || !planId || !cardToken}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {working ? "Creating..." : "Create subscription"}
            </button>
          </div>
        </div>
      )}

      <div className="rounded-xl border bg-card">
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold">All subscriptions</h2>
        </div>
        {subscriptions.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
            <RefreshCw className="h-8 w-8" />
            <p className="text-sm">No subscriptions yet</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="px-6 py-3 text-left font-medium">Code</th>
                  <th className="px-6 py-3 text-left font-medium">Status</th>
                  <th className="px-6 py-3 text-left font-medium">Customer</th>
                  <th className="px-6 py-3 text-left font-medium">Plan</th>
                  <th className="px-6 py-3 text-left font-medium">Current period</th>
                  <th className="px-6 py-3 text-left font-medium">Next billing</th>
                  <th className="px-6 py-3 text-left font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {subscriptions.map((s) => {
                  const customer = customers.find((c) => c.id === s.customerId)
                  const plan = plans.find((p) => p.id === s.planId)
                  return (
                    <tr key={s.id} className="border-b last:border-0 hover:bg-muted/50">
                      <td className="px-6 py-3 font-mono text-xs">{s.code}</td>
                      <td className="px-6 py-3">
                        <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${s.status === "Active" ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}>
                          {s.status}
                        </span>
                      </td>
                      <td className="px-6 py-3">{customer?.email || "—"}</td>
                      <td className="px-6 py-3">{plan?.name || "—"}</td>
                      <td className="px-6 py-3">{new Date(s.currentPeriodStart).toLocaleDateString()} — {new Date(s.currentPeriodEnd).toLocaleDateString()}</td>
                      <td className="px-6 py-3">{s.nextBillingAt ? new Date(s.nextBillingAt).toLocaleDateString() : "—"}</td>
                      <td className="px-6 py-3">
                        <div className="flex items-center gap-2">
                          {s.status === "Active" && !s.cancelAtPeriodEnd && (
                            <>
                              <button
                                onClick={() => collectNow(s.id)}
                                disabled={working}
                                className="inline-flex items-center gap-1 rounded-md border px-2 py-1 text-xs hover:bg-accent disabled:opacity-50"
                                title="Collect now"
                              >
                                <Repeat className="h-3 w-3" />
                              </button>
                              <button
                                onClick={() => cancelSubscription(s.id, false)}
                                disabled={working}
                                className="inline-flex items-center gap-1 rounded-md border border-destructive/50 px-2 py-1 text-xs text-destructive hover:bg-destructive/10 disabled:opacity-50"
                                title="Cancel now"
                              >
                                <Ban className="h-3 w-3" />
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
