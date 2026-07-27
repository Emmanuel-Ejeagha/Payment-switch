"use client"

import { useEffect, useState } from "react"
import { User, Store, Mail, Shield, Check } from "lucide-react"
import type { UserDto, MerchantDto } from "@paymentswitch/shared"
import { StatusBadge } from "@paymentswitch/ui"

export default function ProfilePage() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) { setError("Failed to load user"); setLoading(false); return }
        const userData: UserDto = await userRes.json()
        setUser(userData)

        const merchantRes = await fetch(`/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(userData.email)}`)
        if (!merchantRes.ok) { setError("Failed to load merchant profile"); setLoading(false); return }
        setMerchant(await merchantRes.json())
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load profile")
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-semibold">Profile</h1>
        <div className="grid gap-6 md:grid-cols-2">
          <div className="h-56 animate-pulse rounded-xl bg-muted" />
          <div className="h-56 animate-pulse rounded-xl bg-muted" />
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-semibold">Profile</h1>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">Failed to load profile</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Profile</h1>
        <p className="text-sm text-muted-foreground">
          Your account and merchant information
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        {user && (
          <div className="rounded-xl border p-6">
            <div className="mb-4 flex items-center gap-2">
              <User className="h-5 w-5 text-muted-foreground" />
              <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                Account
              </h2>
            </div>
            <dl className="space-y-4 text-sm">
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Name</dt>
                <dd className="font-medium">{user.fullName}</dd>
              </div>
              <div className="flex justify-between items-center">
                <dt className="text-muted-foreground">Email</dt>
                <dd className="flex items-center gap-1.5 font-medium">
                  <Mail className="h-3.5 w-3.5 text-muted-foreground" />
                  {user.email}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Roles</dt>
                <dd className="font-medium">{user.roles.join(", ") || "—"}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Active</dt>
                <dd>
                  {user.isActive ? (
                    <Check className="h-4 w-4 text-emerald-500" />
                  ) : (
                    <span className="text-muted-foreground">No</span>
                  )}
                </dd>
              </div>
            </dl>
          </div>
        )}

        {merchant && (
          <div className="rounded-xl border p-6">
            <div className="mb-4 flex items-center gap-2">
              <Store className="h-5 w-5 text-muted-foreground" />
              <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                Merchant
              </h2>
            </div>
            <dl className="space-y-4 text-sm">
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
                <dt className="text-muted-foreground">Member Since</dt>
                <dd className="font-medium">
                  {merchant.createdAt ? new Date(merchant.createdAt).toLocaleDateString() : "—"}
                </dd>
              </div>
            </dl>
          </div>
        )}
      </div>
    </div>
  )
}
