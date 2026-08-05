"use client"

import { useEffect, useState, useCallback } from "react"
import { Plus, X, Archive, Package } from "lucide-react"
import type { PlanDto } from "@paymentswitch/shared"

const SUPPORTED_CURRENCIES = ["USD", "EUR", "GBP", "NGN"]
const INTERVAL_UNITS = ["Day", "Week", "Month", "Year"]

export default function PlansPage() {
  const [merchantId, setMerchantId] = useState<string | null>(null)
  const [plans, setPlans] = useState<PlanDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)

  const [showForm, setShowForm] = useState(false)
  const [name, setName] = useState("")
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("USD")
  const [intervalUnit, setIntervalUnit] = useState("Month")
  const [intervalCount, setIntervalCount] = useState("1")
  const [description, setDescription] = useState("")

  const loadPlans = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/plans?merchantId=${merchantId}&skip=0&take=100`)
    if (res.ok) setPlans(await res.json())
  }, [])

  useEffect(() => {
    async function load() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) { setError("Failed to load user"); setLoading(false); return }
        const user = await userRes.json()
        const merchantRes = await fetch(`/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(user.email)}`)
        if (!merchantRes.ok) { setError("Failed to load merchant"); setLoading(false); return }
        const merchant = await merchantRes.json()
        setMerchantId(merchant.id)
        await loadPlans(merchant.id)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load plans")
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [loadPlans])

  const createPlan = async () => {
    if (!merchantId || !name || !amount) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch("/api/proxy/payment/api/v1/plans", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId,
          name,
          amount: Math.round(parseFloat(amount) * 100),
          currency,
          intervalUnit,
          intervalCount: parseInt(intervalCount, 10) || 1,
          description: description || null,
        }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Failed to create plan")
        return
      }
      await loadPlans(merchantId)
      setShowForm(false)
      setName("")
      setAmount("")
      setDescription("")
      setIntervalCount("1")
    } finally {
      setWorking(false)
    }
  }

  const archivePlan = async (id: string) => {
    if (!merchantId || !confirm("Archive this plan? Existing subscriptions keep billing.")) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/plans/${id}/archive`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Failed to archive plan")
        return
      }
      await loadPlans(merchantId)
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
          <h1 className="text-3xl font-semibold">Plans</h1>
          <p className="text-sm text-muted-foreground">Recurring billing plans customers can subscribe to.</p>
        </div>
        <button
          onClick={() => setShowForm((v) => !v)}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90"
        >
          {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
          {showForm ? "Cancel" : "New plan"}
        </button>
      </div>

      {error && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}

      {showForm && (
        <div className="rounded-xl border bg-card p-6">
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Name <span className="text-destructive">*</span></label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Pro monthly"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Amount <span className="text-destructive">*</span></label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="29.99"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Currency</label>
              <select
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              >
                {SUPPORTED_CURRENCIES.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Bills every</label>
              <input
                type="number"
                min="1"
                value={intervalCount}
                onChange={(e) => setIntervalCount(e.target.value)}
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Interval</label>
              <select
                value={intervalUnit}
                onChange={(e) => setIntervalUnit(e.target.value)}
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              >
                {INTERVAL_UNITS.map((u) => (
                  <option key={u} value={u}>{u}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Description</label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Full access"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>
          <div className="mt-4">
            <button
              onClick={createPlan}
              disabled={working || !name || !amount}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              <Package className="h-4 w-4" />
              {working ? "Creating..." : "Create plan"}
            </button>
          </div>
        </div>
      )}

      <div className="rounded-xl border bg-card">
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold">Your plans</h2>
        </div>
        {plans.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
            <Package className="h-8 w-8" />
            <p className="text-sm">No plans yet</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="px-6 py-3 text-left font-medium">Code</th>
                  <th className="px-6 py-3 text-left font-medium">Name</th>
                  <th className="px-6 py-3 text-left font-medium">Amount</th>
                  <th className="px-6 py-3 text-left font-medium">Interval</th>
                  <th className="px-6 py-3 text-left font-medium">Status</th>
                  <th className="px-6 py-3 text-left font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {plans.map((p) => (
                  <tr key={p.id} className="border-b last:border-0 hover:bg-muted/50">
                    <td className="px-6 py-3 font-mono text-xs">{p.code}</td>
                    <td className="px-6 py-3">{p.name}</td>
                    <td className="px-6 py-3">{(p.amount / 100).toFixed(2)} {p.currency}</td>
                    <td className="px-6 py-3">
                      Every {p.intervalCount} {p.intervalUnit.toLowerCase()}{p.intervalCount > 1 ? "s" : ""}
                    </td>
                    <td className="px-6 py-3">
                      <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${p.active ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}>
                        {p.active ? "Active" : "Archived"}
                      </span>
                    </td>
                    <td className="px-6 py-3">
                      {p.active && (
                        <button
                          onClick={() => archivePlan(p.id)}
                          disabled={working}
                          className="inline-flex items-center gap-1 rounded-md border px-2 py-1 text-xs text-muted-foreground hover:bg-accent disabled:opacity-50"
                        >
                          <Archive className="h-3 w-3" />
                          Archive
                        </button>
                      )}
                    </td>
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
