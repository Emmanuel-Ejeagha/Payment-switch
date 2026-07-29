"use client"

import { useEffect, useState } from "react"
import { BookOpen, ChevronLeft, ChevronRight, Wallet, Clock, Lock } from "lucide-react"
import type { UserDto, MerchantDto, BalanceDto, LedgerTransactionDto } from "@paymentswitch/shared"

const txTypeColors: Record<string, string> = {
  Credit: "bg-emerald-500/10 text-emerald-600",
  Debit: "bg-red-500/10 text-red-600",
  Reserve: "bg-amber-500/10 text-amber-600",
  Release: "bg-blue-500/10 text-blue-600",
}

export default function MerchantLedgerPage() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [balances, setBalances] = useState<BalanceDto[]>([])
  const [transactions, setTransactions] = useState<LedgerTransactionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [skip, setSkip] = useState(0)
  const take = 10

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

        const [balRes, txRes] = await Promise.all([
          fetch(`/api/proxy/ledger/api/v1/ledger/balances?merchantId=${merchantData.id}`),
          fetch(`/api/proxy/ledger/api/v1/ledger/transactions?merchantId=${merchantData.id}&skip=0&take=${take}`),
        ])
        if (balRes.ok) setBalances(await balRes.json())
        if (txRes.ok) setTransactions(await txRes.json())
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load ledger")
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [])

  useEffect(() => {
    if (!merchant) return
    const mId = merchant.id
    async function loadTx() {
      const txRes = await fetch(
        `/api/proxy/ledger/api/v1/ledger/transactions?merchantId=${mId}&skip=${skip}&take=${take}`
      )
      if (txRes.ok) setTransactions(await txRes.json())
    }
    loadTx()
  }, [skip])

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-semibold">Ledger</h1>
        <div className="grid gap-4 md:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
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
        <h1 className="text-3xl font-semibold">Ledger</h1>
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">Failed to load ledger</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Ledger</h1>
        <p className="text-sm text-muted-foreground">
          {merchant ? `${merchant.businessName} — balance and transactions` : "Balance and transactions"}
        </p>
      </div>

      {balances.length > 0 && (
        <div className="space-y-6">
          {balances.map((b) => (
            <div key={b.currency}>
              <p className="mb-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">{b.currency}</p>
              <div className="grid gap-4 md:grid-cols-3">
                <div className="rounded-xl border p-6">
                  <div className="mb-3 flex items-center gap-3">
                    <div className="rounded-lg bg-emerald-500/10 p-2">
                      <Wallet className="h-5 w-5 text-emerald-600" />
                    </div>
                    <p className="text-sm text-muted-foreground">Available</p>
                  </div>
                  <p className="text-2xl font-semibold">{(b.available / 100).toFixed(2)} {b.currency}</p>
                </div>
                <div className="rounded-xl border p-6">
                  <div className="mb-3 flex items-center gap-3">
                    <div className="rounded-lg bg-amber-500/10 p-2">
                      <Clock className="h-5 w-5 text-amber-600" />
                    </div>
                    <p className="text-sm text-muted-foreground">Pending</p>
                  </div>
                  <p className="text-2xl font-semibold">{(b.pending / 100).toFixed(2)} {b.currency}</p>
                </div>
                <div className="rounded-xl border p-6">
                  <div className="mb-3 flex items-center gap-3">
                    <div className="rounded-lg bg-blue-500/10 p-2">
                      <Lock className="h-5 w-5 text-blue-600" />
                    </div>
                    <p className="text-sm text-muted-foreground">Reserved</p>
                  </div>
                  <p className="text-2xl font-semibold">{(b.reserved / 100).toFixed(2)} {b.currency}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="rounded-xl border">
        <div className="flex items-center gap-2 border-b px-6 py-4">
          <BookOpen className="h-5 w-5 text-muted-foreground" />
          <h2 className="font-semibold">Transaction History</h2>
        </div>
        {transactions.length === 0 ? (
          <div className="flex flex-col items-center py-12 text-center">
            <BookOpen className="mb-2 h-6 w-6 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">No transactions found</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-muted/50 text-left text-sm text-muted-foreground">
                    <th className="px-6 py-3 font-medium">Type</th>
                    <th className="px-6 py-3 font-medium">Amount</th>
                    <th className="px-6 py-3 font-medium">Description</th>
                    <th className="px-6 py-3 font-medium">Date</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {transactions.map((t) => (
                    <tr key={t.id} className="text-sm hover:bg-muted/30">
                      <td className="px-6 py-4">
                        <span
                          className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${txTypeColors[t.type] || "bg-muted text-muted-foreground"}`}
                        >
                          {t.type}
                        </span>
                      </td>
                      <td className="px-6 py-4 font-medium">
                        {(t.amount / 100).toFixed(2)} {t.currency}
                      </td>
                      <td className="max-w-xs truncate px-6 py-4 text-muted-foreground">
                        {t.description || "—"}
                      </td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {new Date(t.timestamp).toLocaleDateString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex items-center justify-between px-6 py-3 text-sm text-muted-foreground">
              <p>Showing {skip + 1}–{skip + transactions.length}</p>
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
                  disabled={transactions.length < take}
                  className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-accent disabled:opacity-50"
                >
                  Next <ChevronRight className="h-4 w-4" />
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
