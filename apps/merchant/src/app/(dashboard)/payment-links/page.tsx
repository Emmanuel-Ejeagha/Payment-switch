"use client"

import { useEffect, useState, useCallback } from "react"
import { Link2, Copy, Plus, X } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { PaymentLinkDto } from "@paymentswitch/shared"

const SUPPORTED_CURRENCIES = ["USD", "EUR", "GBP", "NGN"]

export default function PaymentLinksPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const [links, setLinks] = useState<PaymentLinkDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [copied, setCopied] = useState<string | null>(null)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const [showForm, setShowForm] = useState(false)
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("USD")
  const [description, setDescription] = useState("")

  const loadLinks = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/payment-links?merchantId=${merchantId}&skip=0&take=50`)
    if (res.ok) setLinks(await res.json())
  }, [])

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    const mid = merchantId
    async function load() {
      try {
        await loadLinks(mid)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load payment links")
      } finally {
        setDataReady(true)
      }
    }
    load()
  }, [merchantLoading, merchantId, loadLinks])

  const createLink = async () => {
    if (!merchantId) return
    setCreating(true)
    setError(null)
    try {
      const res = await fetch("/api/proxy/payment/api/v1/payment-links", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId,
          amount: Math.round(parseFloat(amount) * 100),
          currency,
          description,
        }),
      })
      if (!res.ok) {
        const body = await res.json()
        setError(body.message ?? body.detail ?? "Failed to create payment link")
        return
      }
      await loadLinks(merchantId)
      setShowForm(false)
      setAmount("")
      setDescription("")
    } finally {
      setCreating(false)
    }
  }

  const copyLink = async (code: string) => {
    const url = `${window.location.origin}/checkout/${code}`
    await navigator.clipboard.writeText(url)
    setCopied(code)
    setTimeout(() => setCopied(null), 1500)
  }

  if (loading) {
    return <div className="h-64 animate-pulse rounded-xl bg-muted" />
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold">Payment links</h1>
          <p className="text-sm text-muted-foreground">Create shareable checkout links for your customers.</p>
        </div>
        <button
          onClick={() => setShowForm((v) => !v)}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90"
        >
          {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
          {showForm ? "Cancel" : "New link"}
        </button>
      </div>

      {(error || merchantError) && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error || merchantError}</div>}

      {showForm && (
        <div className="rounded-xl border bg-card p-6">
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Amount</label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="100.00"
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
              <label className="mb-1 block text-sm font-medium">Description</label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Pro plan subscription"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>
          <div className="mt-4">
            <button
              onClick={createLink}
              disabled={creating || !amount}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              <Link2 className="h-4 w-4" />
              {creating ? "Creating..." : "Create link"}
            </button>
          </div>
        </div>
      )}

      <div className="rounded-xl border bg-card">
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold">Your links</h2>
        </div>
        {links.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
            <Link2 className="h-8 w-8" />
            <p className="text-sm">No payment links yet</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="px-6 py-3 text-left font-medium">Code</th>
                  <th className="px-6 py-3 text-left font-medium">Amount</th>
                  <th className="px-6 py-3 text-left font-medium">Description</th>
                  <th className="px-6 py-3 text-left font-medium">Status</th>
                  <th className="px-6 py-3 text-left font-medium">Checkout URL</th>
                </tr>
              </thead>
              <tbody>
                {links.map((link) => (
                  <tr key={link.id} className="border-b last:border-0 hover:bg-muted/50">
                    <td className="px-6 py-3 font-mono text-xs">{link.code}</td>
                    <td className="px-6 py-3">{(link.amount / 100).toFixed(2)} {link.currency}</td>
                    <td className="px-6 py-3">{link.description || "—"}</td>
                    <td className="px-6 py-3">
                      <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${link.active ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}>
                        {link.active ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-6 py-3">
                      <button
                        onClick={() => copyLink(link.code)}
                        className="inline-flex items-center gap-1 rounded-md border px-2 py-1 text-xs text-muted-foreground hover:bg-accent"
                      >
                        <Copy className="h-3 w-3" />
                        {copied === link.code ? "Copied!" : "Copy"}
                      </button>
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
