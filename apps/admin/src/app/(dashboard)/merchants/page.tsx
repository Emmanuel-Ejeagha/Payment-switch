"use client"

import { useEffect, useState } from "react"
import { Search, ChevronLeft, ChevronRight, Store } from "lucide-react"
import Link from "next/link"
import type { MerchantDto } from "@paymentswitch/shared"
import { StatusBadge } from "@paymentswitch/ui"

export default function MerchantsPage() {
  const [merchants, setMerchants] = useState<MerchantDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState<string>("All")
  const [skip, setSkip] = useState(0)
  const take = 10

  useEffect(() => {
    async function load() {
      setLoading(true)
      setError(null)
      try {
        const params = new URLSearchParams({ skip: String(skip), take: String(take) })
        if (search) params.set("search", search)

        const res = await fetch(`/api/proxy/merchant/api/v1/merchants?${params}`)

        if (res.status === 403) {
          setError("You need admin access to view merchants.")
          setMerchants([])
          return
        }
        if (!res.ok) {
          setError(`Request failed (${res.status})`)
          setMerchants([])
          return
        }

        const data: MerchantDto[] = await res.json()
        setMerchants(data)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load merchants")
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [skip, search])

  const filtered = merchants.filter((m) =>
    statusFilter === "All" ? true : m.status === statusFilter,
  )

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Merchants</h1>
        <p className="text-sm text-muted-foreground">
          Manage all merchants on the platform
        </p>
      </div>

      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            placeholder="Search merchants..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setSkip(0) }}
            className="flex h-10 w-full rounded-lg border border-input bg-background pl-10 pr-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          />
        </div>

        <div className="flex gap-2">
          {["All", "Active", "Pending", "Suspended"].map((s) => (
            <button
              key={s}
              onClick={() => { setStatusFilter(s); setSkip(0) }}
              className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
                statusFilter === s
                  ? "bg-primary text-primary-foreground"
                  : "bg-muted text-muted-foreground hover:bg-accent"
              }`}
            >
              {s}
            </button>
          ))}
        </div>
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : error ? (
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">{error}</p>
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-16 text-center">
          <Store className="mb-3 h-8 w-8 text-muted-foreground" />
          <p className="font-medium">No merchants found</p>
          <p className="text-sm text-muted-foreground">
            {search
              ? "Try a different search term"
              : "No merchants match the selected filter"}
          </p>
        </div>
      ) : (
        <>
          <div className="overflow-hidden rounded-xl border">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50 text-left text-sm text-muted-foreground">
                  <th className="px-6 py-3 font-medium">Business Name</th>
                  <th className="px-6 py-3 font-medium">Email</th>
                  <th className="px-6 py-3 font-medium">Status</th>
                  <th className="px-6 py-3 font-medium">Created</th>
                  <th className="px-6 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {filtered.map((m) => (
                  <tr key={m.id} className="text-sm transition-colors hover:bg-muted/30">
                    <td className="px-6 py-4 font-medium">{m.businessName}</td>
                    <td className="px-6 py-4 text-muted-foreground">{m.email}</td>
                    <td className="px-6 py-4">
                      <StatusBadge status={m.status} />
                    </td>
                    <td className="px-6 py-4 text-muted-foreground">
                      {m.createdAt ? new Date(m.createdAt).toLocaleDateString() : "-"}
                    </td>
                    <td className="px-6 py-4">
                      <Link href={`/merchants/${m.id}`} className="text-primary hover:underline text-xs font-medium">
                        View details
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <p>
              Showing {skip + 1}–{skip + filtered.length}
            </p>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setSkip(Math.max(0, skip - take))}
                disabled={skip === 0}
                className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors hover:bg-accent disabled:pointer-events-none disabled:opacity-50"
              >
                <ChevronLeft className="h-4 w-4" /> Previous
              </button>
              <button
                onClick={() => setSkip(skip + take)}
                disabled={filtered.length < take}
                className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors hover:bg-accent disabled:pointer-events-none disabled:opacity-50"
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
