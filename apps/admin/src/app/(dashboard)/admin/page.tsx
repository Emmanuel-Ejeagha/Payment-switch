"use client"

import { useEffect, useState } from "react"
import { Shield, User, Check } from "lucide-react"
import type { UserDto } from "@paymentswitch/shared"
import { apiUrl } from "@/lib/api"

export default function AdminPage() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [loading, setLoading] = useState(true)

  const [targetUserId, setTargetUserId] = useState("")
  const [role, setRole] = useState("Admin")
  const [assigning, setAssigning] = useState(false)
  const [assignResult, setAssignResult] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const res = await fetch(apiUrl("/api/proxy/identity/api/v1/users/me"))
        if (res.ok) setUser(await res.json())
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const handleAssignRole = async () => {
    if (!targetUserId.trim()) return
    setAssigning(true)
    setAssignResult(null)
    try {
      const res = await fetch(apiUrl("/api/proxy/identity/api/v1/admin/roles"), {
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

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Admin</h1>
        <p className="text-sm text-muted-foreground">
          User management
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
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
      </div>
    </div>
  )
}
