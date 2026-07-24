"use client"

import { useEffect, useState } from "react"
import { BookOpen, ChevronLeft, ChevronRight, Wallet, ArrowDownLeft, ArrowUpRight, Lock } from "lucide-react"
import type { MerchantDto, BalanceDto, LedgerTransactionDto } from "@paymentswitch/shared"

const txTypeColors: Record<string, string> = {
  Credit: "bg-emerald-500/10 text-emerald-600",
  Debit: "bg-red-500/10 text-red-600",
  Reserve: "bg-amber-500/10 text-amber-600",
  Release: "bg-blue-500/10 text-blue-600",
}

function formatAmount(amount: number, currency: string) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: currency || "USD",
  }).format(amount)
}

export default function LedgerPage() {
  const [merchants, setMerchants] = useState<MerchantDto[]>([])
  const [selectedMerchantId, setSelectedMerchantId] = useState("")
  const [balance, setBalance] = useState<BalanceDto | null>(null)
  const [transactions, setTransactions] = useState<LedgerTransactionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [skip, setSkip] = useState(0)
  const take = 10

  useEffect(() => {
    async function loadMerchants() {
      const res = await fetch("/api/proxy/merchant/api/v1/merchants?skip=0&take=100")
      if (res.ok) {
        const list: MerchantDto[] = await res.json()
        setMerchants(list)
        if (list.length > 0 && !selectedMerchantId) {
          setSelectedMerchantId(list[0].id)
        }
      }
    }
    loadMerchants()
  }, [])

  useEffect(() => {
    if (!selectedMerchantId) return

    async function loadLedger() {
      setLoading(true)
      try {
        const [balRes, txRes] = await Promise.all([
          fetch(`/api/proxy/ledger/api/v1/ledger/balance?merchantId=${selectedMerchantId}`),
          fetch(`/api/proxy/ledger/api/v1/ledger/transactions?merchantId=${selectedMerchantId}&skip=${skip}&take=${take}`),
        ])
        if (balRes.ok) setBalance(await balRes.json())
        if (txRes.ok) setTransactions(await txRes.json())
      } finally {
        setLoading(false)
      }
    }
    loadLedger()
  }, [selectedMerchantId, skip])

  const selectedMerchant = merchants.find((m) => m.id === selectedMerchantId)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Ledger</h1>
        <p className="text-sm text-muted-foreground">
          Merchant balances and transaction history
        </p>
      </div>

      <select
        value={selectedMerchantId}
        onChange={(e) => { setSelectedMerchantId(e.target.value); setSkip(0) }}
        className="flex h-10 max-w-sm rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
      >
        {merchants.length === 0 && <option value="">No merchants available</option>}
        {merchants.map((m) => (
          <option key={m.id} value={m.id}>{m.businessName}</option>
        ))}
      </select>

      {loading ? (
        <div className="space-y-4">
          <div className="h-28 animate-pulse rounded-xl bg-muted" />
          <div className="h-64 animate-pulse rounded-xl bg-muted" />
        </div>
      ) : balance ? (
        <>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="rounded-xl border p-6">
              <div className="flex items-center gap-3 mb-3">
                <div className="rounded-lg bg-emerald-500/10 p-2">
                  <ArrowDownLeft className="h-5 w-5 text-emerald-600" />
                </div>
                <p className="text-sm text-muted-foreground">Available</p>
              </div>
              <p className="text-2xl font-semibold">
                {formatAmount(balance.available, balance.currency)}
              </p>
            </div>
            <div className="rounded-xl border p-6">
              <div className="flex items-center gap-3 mb-3">
                <div className="rounded-lg bg-amber-500/10 p-2">
                  <ArrowUpRight className="h-5 w-5 text-amber-600" />
                </div>
                <p className="text-sm text-muted-foreground">Pending</p>
              </div>
              <p className="text-2xl font-semibold">
                {formatAmount(balance.pending, balance.currency)}
              </p>
            </div>
            <div className="rounded-xl border p-6">
              <div className="flex items-center gap-3 mb-3">
                <div className="rounded-lg bg-blue-500/10 p-2">
                  <Lock className="h-5 w-5 text-blue-600" />
                </div>
                <p className="text-sm text-muted-foreground">Reserved</p>
              </div>
              <p className="text-2xl font-semibold">
                {formatAmount(balance.reserved, balance.currency)}
              </p>
            </div>
          </div>

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
                <div className="overflow-x-auto"><table className="w-full">
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
                          {formatAmount(t.amount, t.currency)}
                        </td>
                        <td className="px-6 py-4 text-muted-foreground max-w-xs truncate">
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
        </>
      ) : (
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
          <p className="font-medium">Could not load ledger data</p>
        </div>
      )}
    </div>
  )
}
