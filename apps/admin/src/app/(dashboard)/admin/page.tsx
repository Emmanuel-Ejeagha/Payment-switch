"use client"

import { useEffect, useState } from "react"
import { Shield, User, Key, Check, Trash2, Plus, Copy } from "lucide-react"
import type { UserDto, ApiKeyDto } from "@paymentswitch/shared"

export default function AdminPage() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [loading, setLoading] = useState(true)

  const [targetUserId, setTargetUserId] = useState("")
  const [role, setRole] = useState("Admin")
  const [assigning, setAssigning] = useState(false)
  const [assignResult, setAssignResult] = useState<string | null>(null)

  const [apiKeys, setApiKeys] = useState<ApiKeyDto[]>([])
  const [keysLoading, setKeysLoading] = useState(true)
  const [environment, setEnvironment] = useState("Development")
  const [generating, setGenerating] = useState(false)
  const [newKey, setNewKey] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const res = await fetch("/api/proxy/identity/api/v1/users/me")
        if (res.ok) setUser(await res.json())
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  useEffect(() => {
    async function loadKeys() {
      const res = await fetch("/api/proxy/identity/api/v1/apikeys")
      if (res.ok) setApiKeys(await res.json())
      setKeysLoading(false)
    }
    loadKeys()
  }, [])

  const handleAssignRole = async () => {
    if (!targetUserId.trim()) return
    setAssigning(true)
    setAssignResult(null)
    try {
      const res = await fetch("/api/proxy/identity/api/v1/admin/roles", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ targetUserId, role }),
      })
      const data = res.ok ? "Role assigned successfully" : await res.text()
      setAssignResult(res.ok ? "Role assigned successfully" : data || `Failed (${res.status})`)
    } catch {
      setAssignResult("Failed to assign role")
    } finally {
      setAssigning(false)
    }
  }

  const handleGenerateKey = async () => {
    setGenerating(true)
    setNewKey(null)
    try {
      const res = await fetch("/api/proxy/identity/api/v1/apikeys", {
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
      }
    } finally {
      setGenerating(false)
    }
  }

  const handleRevokeKey = async (keyId: string) => {
    const res = await fetch(`/api/proxy/identity/api/v1/apikeys/${keyId}`, {
      method: "DELETE",
    })
    if (res.ok) {
      setApiKeys((prev) =>
        prev.map((k) => (k.keyId === keyId ? { ...k, revokedAt: new Date().toISOString() } : k)),
      )
    }
  }

  const activeKeys = apiKeys.filter((k) => !k.revokedAt)
  const revokedKeys = apiKeys.filter((k) => k.revokedAt)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Admin</h1>
        <p className="text-sm text-muted-foreground">
          User management and API keys
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="rounded-xl border p-6">
          <div className="flex items-center gap-2 mb-4">
            <User className="h-5 w-5 text-muted-foreground" />
            <h2 className="font-semibold">Your Profile</h2>
          </div>

          {loading ? (
            <div className="h-24 animate-pulse rounded bg-muted" />
          ) : user ? (
            <dl className="space-y-3 text-sm">
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Name</dt>
                <dd className="font-medium">{user.fullName}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Email</dt>
                <dd className="font-medium">{user.email}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Roles</dt>
                <dd className="font-medium">{user.roles.join(", ") || "—"}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Active</dt>
                <dd>{user.isActive ? <Check className="h-4 w-4 text-emerald-500" /> : "—"}</dd>
              </div>
            </dl>
          ) : (
            <p className="text-sm text-muted-foreground">Could not load profile.</p>
          )}
        </div>

        <div className="rounded-xl border p-6">
          <div className="flex items-center gap-2 mb-4">
            <Shield className="h-5 w-5 text-muted-foreground" />
            <h2 className="font-semibold">Assign Role</h2>
          </div>

          <div className="space-y-4">
            <div className="space-y-2">
              <label htmlFor="targetUserId" className="text-sm font-medium">Target User ID</label>
              <input
                id="targetUserId"
                value={targetUserId}
                onChange={(e) => setTargetUserId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
                className="flex h-10 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              />
            </div>
            <div className="space-y-2">
              <label htmlFor="role" className="text-sm font-medium">Role</label>
              <select
                id="role"
                value={role}
                onChange={(e) => setRole(e.target.value)}
                className="flex h-10 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              >
                <option value="Admin">Admin</option>
                <option value="Merchant">Merchant</option>
                <option value="Support">Support</option>
              </select>
            </div>
            <button
              onClick={handleAssignRole}
              disabled={assigning || !targetUserId.trim()}
              className="inline-flex h-10 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:pointer-events-none disabled:opacity-50"
            >
              {assigning ? "Assigning..." : "Assign Role"}
            </button>
            {assignResult && (
              <p className={`text-sm ${assignResult === "Role assigned successfully" ? "text-emerald-600" : "text-destructive"}`}>
                {assignResult}
              </p>
            )}
          </div>
        </div>

        <div className="rounded-xl border p-6">
          <div className="flex items-center gap-2 mb-4">
            <Key className="h-5 w-5 text-muted-foreground" />
            <h2 className="font-semibold">API Keys</h2>
          </div>

          <div className="space-y-4">
            <div className="flex gap-2">
              <select
                value={environment}
                onChange={(e) => setEnvironment(e.target.value)}
                className="flex h-10 flex-1 rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              >
                <option value="Development">Development</option>
                <option value="Staging">Staging</option>
                <option value="Production">Production</option>
              </select>
              <button
                onClick={handleGenerateKey}
                disabled={generating}
                className="inline-flex h-10 items-center gap-1 rounded-lg bg-primary px-3 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                <Plus className="h-4 w-4" /> Generate
              </button>
            </div>

            {newKey && (
              <div className="rounded-lg border border-emerald-500/50 bg-emerald-500/10 p-3">
                <p className="text-xs font-medium text-emerald-600 mb-1">Copy your new API key now — it won't be shown again:</p>
                <div className="flex items-center gap-2">
                  <code className="flex-1 truncate rounded bg-background px-2 py-1 text-xs font-mono">{newKey}</code>
                  <button
                    onClick={() => { navigator.clipboard.writeText(newKey); setNewKey(null) }}
                    className="shrink-0 rounded p-1 hover:bg-emerald-500/20"
                  >
                    <Copy className="h-4 w-4 text-emerald-600" />
                  </button>
                </div>
              </div>
            )}

            {keysLoading ? (
              <div className="h-20 animate-pulse rounded bg-muted" />
            ) : activeKeys.length === 0 && revokedKeys.length === 0 ? (
              <p className="text-sm text-muted-foreground">No API keys yet.</p>
            ) : (
              <div className="space-y-3">
                {activeKeys.map((k) => (
                  <div key={k.keyId} className="flex items-center justify-between rounded-lg border p-3">
                    <div>
                      <p className="text-sm font-medium">{k.environment}</p>
                      <p className="text-xs text-muted-foreground">
                        Created {new Date(k.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                    <button
                      onClick={() => handleRevokeKey(k.keyId)}
                      className="rounded p-1.5 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                ))}
                {revokedKeys.length > 0 && (
                  <details className="text-xs text-muted-foreground">
                    <summary className="cursor-pointer py-1">
                      {revokedKeys.length} revoked key{revokedKeys.length !== 1 ? "s" : ""}
                    </summary>
                    <div className="mt-2 space-y-2">
                      {revokedKeys.map((k) => (
                        <div key={k.keyId} className="flex items-center justify-between rounded-lg border border-dashed p-2 opacity-60">
                          <div>
                            <p className="text-sm font-medium">{k.environment}</p>
                            <p className="text-xs">Revoked {k.revokedAt ? new Date(k.revokedAt).toLocaleDateString() : ""}</p>
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
      </div>
    </div>
  )
}
