"use client"

import { useEffect, useState } from "react"
import { useParams } from "next/navigation"
import { ArrowLeft, Store, Check, X } from "lucide-react"
import Link from "next/link"
import type { MerchantDto } from "@paymentswitch/shared"
import { StatusBadge } from "@paymentswitch/ui"

const paymentMethods = ["card", "bank_transfer", "wallet", "ussd"]

export default function MerchantDetailPage() {
  const params = useParams()
  const id = params.id as string

  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [webhookUrl, setWebhookUrl] = useState("")
  const [enabledMethods, setEnabledMethods] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [saveMessage, setSaveMessage] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${id}`)
        if (!res.ok) {
          setError(`Request failed (${res.status})`)
          return
        }
        const data: MerchantDto = await res.json()
        setMerchant(data)
        setWebhookUrl(data.webhookUrl || "")
        setEnabledMethods(data.enabledPaymentMethods || [])
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load merchant")
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [id])

  const toggleMethod = (method: string) => {
    setEnabledMethods((prev) =>
      prev.includes(method) ? prev.filter((m) => m !== method) : [...prev, method],
    )
  }

  const handleSaveConfig = async () => {
    setSaving(true)
    setSaveMessage(null)
    try {
      const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${id}/configuration`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId: id,
          webhookUrl: webhookUrl || null,
          paymentMethods: enabledMethods,
        }),
      })
      if (res.ok) {
        setSaveMessage("Configuration updated")
      } else {
        const body = await res.text()
        setSaveMessage(body || `Failed (${res.status})`)
      }
    } catch {
      setSaveMessage("Failed to save configuration")
    } finally {
      setSaving(false)
    }
  }

  const handleActivate = async () => {
    const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${id}/activate`, {
      method: "POST",
    })
    if (res.ok) {
      setMerchant((prev) => prev ? { ...prev, status: "Active" as const } : null)
    }
  }

  const handleSuspend = async () => {
    const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${id}/suspend`, {
      method: "POST",
    })
    if (res.ok) {
      setMerchant((prev) => prev ? { ...prev, status: "Suspended" as const } : null)
    }
  }

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-48 animate-pulse rounded bg-muted" />
        <div className="h-64 animate-pulse rounded-xl bg-muted" />
      </div>
    )
  }

  if (error || !merchant) {
    return (
      <div className="space-y-6">
        <Link href="/merchants" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" /> Back to merchants
        </Link>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">{error || "Merchant not found"}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <Link href="/merchants" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="h-4 w-4" /> Back to merchants
      </Link>

      <div className="flex items-start justify-between">
        <div className="flex items-center gap-4">
          <div className="rounded-lg bg-primary/10 p-3">
            <Store className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-semibold">{merchant.businessName}</h1>
            <p className="text-sm text-muted-foreground">{merchant.email}</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <StatusBadge status={merchant.status} />
          {merchant.status === "Pending" && (
            <button
              onClick={handleActivate}
              className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-700"
            >
              <Check className="h-3.5 w-3.5" /> Activate
            </button>
          )}
          {merchant.status === "Active" && (
            <button
              onClick={handleSuspend}
              className="inline-flex items-center gap-1 rounded-lg bg-orange-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-orange-700"
            >
              <X className="h-3.5 w-3.5" /> Suspend
            </button>
          )}
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <div className="rounded-xl border p-6">
          <h2 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wider">
            Configuration
          </h2>

          <div className="space-y-5">
            <div className="space-y-2">
              <label htmlFor="webhook" className="text-sm font-medium">
                Webhook URL
              </label>
              <input
                id="webhook"
                type="url"
                value={webhookUrl}
                onChange={(e) => setWebhookUrl(e.target.value)}
                placeholder="https://example.com/webhook"
                className="flex h-10 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Payment Methods</label>
              <div className="flex flex-wrap gap-2">
                {paymentMethods.map((method) => (
                  <button
                    key={method}
                    onClick={() => toggleMethod(method)}
                    className={`rounded-lg border px-3 py-1.5 text-xs font-medium transition-colors ${
                      enabledMethods.includes(method)
                        ? "border-primary bg-primary/10 text-primary"
                        : "border-input text-muted-foreground hover:bg-accent"
                    }`}
                  >
                    {method === "bank_transfer" ? "Bank Transfer" : method.charAt(0).toUpperCase() + method.slice(1)}
                  </button>
                ))}
              </div>
            </div>

            <button
              onClick={handleSaveConfig}
              disabled={saving}
              className="inline-flex h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:pointer-events-none disabled:opacity-50"
            >
              {saving ? "Saving..." : "Save Configuration"}
            </button>

            {saveMessage && (
              <p className={`text-sm ${saveMessage === "Configuration updated" ? "text-emerald-600" : "text-destructive"}`}>
                {saveMessage}
              </p>
            )}
          </div>
        </div>

        <div className="rounded-xl border p-6">
          <h2 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wider">
            Details
          </h2>
          <dl className="space-y-3 text-sm">
            <div className="flex justify-between">
              <dt className="text-muted-foreground">ID</dt>
              <dd className="font-medium font-mono text-xs">{merchant.id}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Business Name</dt>
              <dd className="font-medium">{merchant.businessName}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Email</dt>
              <dd className="font-medium">{merchant.email}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Status</dt>
              <dd><StatusBadge status={merchant.status} /></dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Webhook URL</dt>
              <dd className="font-medium text-xs truncate max-w-[200px]">
                {merchant.webhookUrl || "—"}
              </dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Payment Methods</dt>
              <dd className="font-medium text-xs text-right">
                {merchant.enabledPaymentMethods?.join(", ") || "—"}
              </dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  )
}
