"use client"

import { useEffect, useState } from "react"
import { Banknote, ChevronLeft, ChevronRight, Play, Calendar } from "lucide-react"
import Link from "next/link"
import type { SettlementBatchDto } from "@paymentswitch/shared"
import { StatusBadge } from "@paymentswitch/ui"

function formatAmount(amount: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(amount)
}

export default function SettlementsPage() {
  const [batches, setBatches] = useState<SettlementBatchDto[]>([])
  const [loading, setLoading] = useState(true)
  const [skip, setSkip] = useState(0)
  const take = 10
  const [triggering, setTriggering] = useState(false)
  const [triggerResult, setTriggerResult] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      const params = new URLSearchParams({ skip: String(skip), take: String(take) })
      const res = await fetch(`/api/proxy/settlement/api/v1/settlement?${params}`)
      if (res.ok) setBatches(await res.json())
      setLoading(false)
    }
    load()
  }, [skip])

  const handleTrigger = async () => {
    setTriggering(true)
    setTriggerResult(null)
    try {
      const res = await fetch("/api/proxy/settlement/api/v1/settlement/trigger", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ batchDate: new Date().toISOString().split("T")[0] }),
      })
      if (res.ok) {
        const batchId = await res.json()
        setTriggerResult(`Settlement triggered — Batch ID: ${batchId}`)
        setSkip(0)
      } else {
        setTriggerResult(`Failed (${res.status})`)
      }
    } catch {
      setTriggerResult("Failed to trigger settlement")
    } finally {
      setTriggering(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-semibold">Settlements</h1>
          <p className="text-sm text-muted-foreground">
            Settlement batches and payouts
          </p>
        </div>
        <button
          onClick={handleTrigger}
          disabled={triggering}
          className="inline-flex h-10 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
        >
          <Play className="h-4 w-4" /> {triggering ? "Triggering..." : "Trigger Settlement"}
        </button>
      </div>

      {triggerResult && (
        <div className={`rounded-lg border p-3 text-sm ${
          triggerResult.startsWith("Settlement triggered")
            ? "border-emerald-500/50 bg-emerald-500/10 text-emerald-600"
            : "border-destructive/50 bg-destructive/10 text-destructive"
        }`}>
          {triggerResult}
        </div>
      )}

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-20 animate-pulse rounded-xl bg-muted" />
          ))}
        </div>
      ) : batches.length === 0 ? (
        <div className="flex flex-col items-center rounded-xl border border-dashed py-16 text-center">
          <Banknote className="mb-3 h-8 w-8 text-muted-foreground" />
          <p className="font-medium">No settlement batches</p>
          <p className="text-sm text-muted-foreground">Trigger a settlement to get started</p>
        </div>
      ) : (
        <>
          <div className="overflow-hidden rounded-xl border">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50 text-left text-sm text-muted-foreground">
                  <th className="px-6 py-3 font-medium">Batch Date</th>
                  <th className="px-6 py-3 font-medium">Status</th>
                  <th className="px-6 py-3 font-medium">Total Amount</th>
                  <th className="px-6 py-3 font-medium">Payouts</th>
                  <th className="px-6 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {batches.map((b) => (
                  <tr key={b.id} className="text-sm hover:bg-muted/30">
                    <td className="px-6 py-4 font-medium">
                      <div className="flex items-center gap-2">
                        <Calendar className="h-4 w-4 text-muted-foreground" />
                        {new Date(b.batchDate).toLocaleDateString()}
                      </div>
                    </td>
                    <td className="px-6 py-4"><StatusBadge status={b.status} /></td>
                    <td className="px-6 py-4 font-medium">{formatAmount(b.totalAmount)}</td>
                    <td className="px-6 py-4 text-muted-foreground">{b.payouts?.length || 0}</td>
                    <td className="px-6 py-4">
                      <Link href={`/settlements/${b.id}`} className="text-primary hover:underline text-xs font-medium">
                        View details
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <p>Showing {skip + 1}–{skip + batches.length}</p>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setSkip(Math.max(0, skip - take))}
                disabled={skip === 0}
                className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-accent disabled:opacity-50"
              >
                <ChevronLeft className="h-4 w-4" /> Previous
              </button>
              <button
                onClick={() => setSkip(skip + take)}
                disabled={batches.length < take}
                className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-accent disabled:opacity-50"
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
