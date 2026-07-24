"use client"

import { useEffect, useState } from "react"
import { Store, CreditCard, BookOpen, Banknote } from "lucide-react"
import { StatsCard } from "@paymentswitch/ui"
import { RecentMerchants } from "@paymentswitch/ui"
import type { MerchantDto, UserDto } from "@paymentswitch/shared"

interface Stats {
  total: number
  active: number
  pending: number
  suspended: number
}

export default function DashboardPage() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [merchants, setMerchants] = useState<MerchantDto[]>([])
  const [stats, setStats] = useState<Stats | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function load() {
      try {
        const [userRes, merchantsRes] = await Promise.all([
          fetch("/api/proxy/identity/api/v1/users/me"),
          fetch("/api/proxy/merchant/api/v1/merchants?skip=0&take=5"),
        ])

        if (userRes.ok) {
          setUser(await userRes.json())
        }

        if (merchantsRes.ok) {
          const list: MerchantDto[] = await merchantsRes.json()
          setMerchants(list)

          const active = list.filter((m) => m.status === "Active").length
          const pending = list.filter((m) => m.status === "Pending").length
          const suspended = list.filter((m) => m.status === "Suspended").length

          setStats({
            total: list.length,
            active,
            pending,
            suspended,
          })
        } else if (merchantsRes.status === 403) {
          setMerchants([])
          setStats({ total: 0, active: 0, pending: 0, suspended: 0 })
        } else {
          const body = await merchantsRes.text()
          setError(body || `Request failed (${merchantsRes.status})`)
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load dashboard")
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [])

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-semibold">Dashboard</h1>
          <p className="text-sm text-muted-foreground">Loading...</p>
        </div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-28 animate-pulse rounded-xl bg-muted" />
          ))}
        </div>
        <div className="h-64 animate-pulse rounded-xl bg-muted" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-semibold">Dashboard</h1>
        </div>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">Failed to load dashboard</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          Welcome back{user ? `, ${user.fullName}` : ""}
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatsCard
          title="Total Merchants"
          value={stats?.total ?? 0}
          icon={Store}
        />
        <StatsCard
          title="Active"
          value={stats?.active ?? 0}
          icon={CreditCard}
          description="Merchants currently processing"
        />
        <StatsCard
          title="Pending"
          value={stats?.pending ?? 0}
          icon={BookOpen}
          description="Awaiting activation"
        />
        <StatsCard
          title="Suspended"
          value={stats?.suspended ?? 0}
          icon={Banknote}
          description="Temporarily disabled"
        />
      </div>

      <RecentMerchants merchants={merchants} />
    </div>
  )
}
