"use client"

import { useEffect, useState } from "react"
import { Key, Plus, Copy, Trash2, Check, AlertCircle } from "lucide-react"
import type { ApiKeyDto, MerchantDto, UserDto } from "@paymentswitch/shared"

export default function ApiKeysPage() {
  const [apiKeys, setApiKeys] = useState<ApiKeyDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [environment, setEnvironment] = useState("test")
  const [generating, setGenerating] = useState(false)
  const [newKey, setNewKey] = useState<string | null>(null)
  const [copiedId, setCopiedId] = useState<string | null>(null)
  const [merchantId, setMerchantId] = useState<string | null>(null)

  useEffect(() => {
    async function loadKeys() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) { setError("Failed to load user"); setLoading(false); return }
        const userData: UserDto = await userRes.json()

        const merchantRes = await fetch(`/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(userData.email)}`)
        if (!merchantRes.ok) { setError("Failed to load merchant"); setLoading(false); return }
        const merchantData: MerchantDto = await merchantRes.json()
        setMerchantId(merchantData.id)

        const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${merchantData.id}/apikeys`)
        if (!res.ok) { setError("Failed to load API keys"); return }
        setApiKeys(await res.json())
      } catch {
        setError("Failed to load API keys")
      } finally {
        setLoading(false)
      }
    }
    loadKeys()
  }, [])

  const handleGenerateKey = async () => {
    if (!merchantId) return
    setGenerating(true)
    setNewKey(null)
    try {
      const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${merchantId}/apikeys`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ environment }),
      })
      if (res.ok) {
        const data = await res.json()
        setNewKey(data.plainTextKey)
        setApiKeys((prev) => [
          { keyId: data.keyId, environment: data.environment, createdAt: data.createdAt, revokedAt: null },
          ...prev,
        ])
      } else {
        const body = await res.json()
        setError(body.detail || "Failed to generate key")
      }
    } catch {
      setError("Failed to generate key")
    } finally {
      setGenerating(false)
    }
  }

  const handleRevokeKey = async (keyId: string) => {
    if (!merchantId) return
    const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${merchantId}/apikeys/${keyId}`, {
      method: "DELETE",
    })
    if (res.ok) {
      setApiKeys((prev) =>
        prev.map((k) => (k.keyId === keyId ? { ...k, revokedAt: new Date().toISOString() } : k)),
      )
    }
  }

  const handleCopy = (key: string) => {
    navigator.clipboard.writeText(key)
    setCopiedId(key)
    setTimeout(() => setCopiedId(null), 2000)
  }

  const activeKeys = apiKeys.filter((k) => !k.revokedAt)
  const revokedKeys = apiKeys.filter((k) => k.revokedAt)

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-semibold">API Keys</h1>
        <div className="h-48 animate-pulse rounded-xl bg-muted" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">API Keys</h1>
        <p className="text-sm text-muted-foreground">
          Secret keys authenticate your requests to the public Payments API (Authorization: Bearer sk_live_...)
        </p>
      </div>

      <div className="rounded-xl border p-6">
        <div className="mb-4 flex items-center gap-2">
          <Key className="h-5 w-5 text-muted-foreground" />
          <h2 className="font-semibold">Generate New Key</h2>
        </div>

        <div className="flex gap-2">
          <select
            value={environment}
            onChange={(e) => setEnvironment(e.target.value)}
            className="flex h-10 flex-1 rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          >
            <option value="test">Test</option>
            <option value="live">Live</option>
          </select>
          <button
            onClick={handleGenerateKey}
            disabled={generating}
            className="inline-flex h-10 items-center gap-1 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            <Plus className="h-4 w-4" /> {generating ? "Generating..." : "Generate"}
          </button>
        </div>

        {newKey && (
          <div className="mt-4 rounded-lg border border-emerald-500/50 bg-emerald-500/10 p-3">
            <p className="mb-1 text-xs font-medium text-emerald-600">
              Copy your new API key now — it won't be shown again:
            </p>
            <div className="flex items-center gap-2">
              <code className="flex-1 truncate rounded bg-background px-2 py-1 text-xs font-mono">
                {newKey}
              </code>
              <button
                onClick={() => handleCopy(newKey)}
                className="shrink-0 rounded p-1 hover:bg-emerald-500/20"
              >
                {copiedId === newKey ? (
                  <Check className="h-4 w-4 text-emerald-600" />
                ) : (
                  <Copy className="h-4 w-4 text-emerald-600" />
                )}
              </button>
            </div>
          </div>
        )}

        {error && (
          <div className="mt-4 flex items-center gap-2 rounded-lg border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
            <AlertCircle className="h-4 w-4 shrink-0" />
            {error}
          </div>
        )}
      </div>

      <div className="rounded-xl border">
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold">Your API Keys</h2>
        </div>

        {activeKeys.length === 0 && revokedKeys.length === 0 ? (
          <div className="flex flex-col items-center py-12 text-center text-muted-foreground">
            <Key className="mb-2 h-6 w-6" />
            <p className="text-sm">No API keys yet</p>
          </div>
        ) : (
          <div className="divide-y">
            {activeKeys.map((k) => (
              <div key={k.keyId} className="flex items-center justify-between px-6 py-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">{k.environment === "live" ? "Live" : "Test"}</span>
                    <span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                      Active
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Created {new Date(k.createdAt).toLocaleDateString()}
                  </p>
                </div>
                <button
                  onClick={() => handleRevokeKey(k.keyId)}
                  className="rounded p-1.5 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                  title="Revoke key"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            ))}

            {revokedKeys.length > 0 && (
              <details className="px-6 py-3">
                <summary className="cursor-pointer text-xs text-muted-foreground hover:text-foreground">
                  {revokedKeys.length} revoked key{revokedKeys.length !== 1 ? "s" : ""}
                </summary>
                <div className="mt-2 space-y-2">
                  {revokedKeys.map((k) => (
                    <div key={k.keyId} className="flex items-center justify-between rounded-lg border border-dashed p-3 opacity-60">
                      <div className="space-y-1">
                        <p className="text-sm font-medium">{k.environment === "live" ? "Live" : "Test"}</p>
                        <p className="text-xs text-muted-foreground">
                          Revoked {k.revokedAt ? new Date(k.revokedAt).toLocaleDateString() : ""}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </details>
            )}
          </div>
        )}
      </div>
    </div>
  )
}


