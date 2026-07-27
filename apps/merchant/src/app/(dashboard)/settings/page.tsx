"use client"

import { useEffect, useState } from "react"
import { Store, Globe, CreditCard, Check, AlertCircle } from "lucide-react"
import type { MerchantDto, UserDto } from "@paymentswitch/shared"
import { StatusBadge } from "@paymentswitch/ui"

const paymentMethods = ["card", "bank_transfer", "wallet", "ussd"]

export default function SettingsPage() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [webhookUrl, setWebhookUrl] = useState("")
  const [enabledMethods, setEnabledMethods] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [saveMessage, setSaveMessage] = useState<string | null>(null)
  const [saveError, setSaveError] = useState(false)

  useEffect(() => {
    async function load() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) { setError("Failed to load user"); setLoading(false); return }
        const userData: UserDto = await userRes.json()
        setUser(userData)

        const merchantRes = await fetch(`/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(userData.email)}`)
        if (!merchantRes.ok) { setError("Failed to load merchant profile"); setLoading(false); return }
        const merchantData: MerchantDto = await merchantRes.json()
        setMerchant(merchantData)
        setWebhookUrl(merchantData.webhookUrl || "")
        setEnabledMethods(merchantData.enabledPaymentMethods || [])
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load settings")
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const toggleMethod = (method: string) => {
    setEnabledMethods((prev) =>
      prev.includes(method) ? prev.filter((m) => m !== method) : [...prev, method],
    )
  }

  const handleSaveConfig = async () => {
    if (!merchant) return
    setSaving(true)
    setSaveMessage(null)
    setSaveError(false)
    try {
      const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${merchant.id}/configuration`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId: merchant.id,
          webhookUrl: webhookUrl || null,
          paymentMethods: enabledMethods,
        }),
      })
      if (res.ok) {
        setSaveMessage("Configuration updated")
        setSaveError(false)
      } else {
        const body = await res.json()
        setSaveMessage(body.detail || body.title || `Failed (${res.status})`)
        setSaveError(true)
      }
    } catch {
      setSaveMessage("Failed to save configuration")
      setSaveError(true)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-semibold">Settings</h1>
        <div className="grid gap-6 md:grid-cols-2">
          <div className="h-64 animate-pulse rounded-xl bg-muted" />
          <div className="h-64 animate-pulse rounded-xl bg-muted" />
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-semibold">Settings</h1>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">Failed to load settings</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Settings</h1>
        <p className="text-sm text-muted-foreground">
          Manage your merchant profile and integration settings
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <div className="rounded-xl border p-6">
          <div className="mb-4 flex items-center gap-2">
            <Store className="h-5 w-5 text-muted-foreground" />
            <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
              Profile
            </h2>
          </div>

          {merchant && (
            <dl className="space-y-3 text-sm">
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
                <dt className="text-muted-foreground">Name</dt>
                <dd className="font-medium">{user?.fullName || "—"}</dd>
              </div>
            </dl>
          )}
        </div>

        <div className="rounded-xl border p-6">
          <div className="mb-4 flex items-center gap-2">
            <Globe className="h-5 w-5 text-muted-foreground" />
            <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
              Configuration
            </h2>
          </div>

          {merchant?.status !== "Active" && (
            <div className="mb-4 flex items-center gap-2 rounded-lg border border-amber-500/50 bg-amber-500/10 p-3 text-sm text-amber-700">
              <AlertCircle className="h-4 w-4 shrink-0" />
              Configuration can only be updated when the merchant is active.
            </div>
          )}

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
                disabled={merchant?.status !== "Active"}
                className="flex h-10 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Payment Methods</label>
              <div className="flex flex-wrap gap-2">
                {paymentMethods.map((method) => (
                  <button
                    key={method}
                    onClick={() => toggleMethod(method)}
                    disabled={merchant?.status !== "Active"}
                    className={`rounded-lg border px-3 py-1.5 text-xs font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
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
              disabled={saving || merchant?.status !== "Active"}
              className="inline-flex h-10 items-center justify-center gap-1 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:pointer-events-none disabled:opacity-50"
            >
              {saving ? "Saving..." : (
                <>
                  <Check className="h-4 w-4" /> Save Configuration
                </>
              )}
            </button>

            {saveMessage && (
              <div className={`flex items-center gap-2 rounded-lg border p-3 text-sm ${
                saveError
                  ? "border-destructive/50 bg-destructive/10 text-destructive"
                  : "border-emerald-500/50 bg-emerald-500/10 text-emerald-700"
              }`}>
                {saveError ? <AlertCircle className="h-4 w-4 shrink-0" /> : <Check className="h-4 w-4 shrink-0" />}
                {saveMessage}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
