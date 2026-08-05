"use client"

import { useEffect, useState, useCallback, useMemo } from "react"
import { Wallet, Clock, Lock, CreditCard, ChevronDown } from "lucide-react"
import { StatsCard } from "@paymentswitch/ui"
import type { UserDto, MerchantDto, BalanceDto, PaymentIntentDto } from "@paymentswitch/shared"

const SUPPORTED_CURRENCIES = ["USD", "EUR", "GBP", "NGN"]

function zeroBalance(currency: string, merchantId: string): BalanceDto {
  return { merchantId, available: 0, pending: 0, reserved: 0, currency }
}

export default function MerchantDashboardPage() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [balances, setBalances] = useState<BalanceDto[]>([])
  const [payments, setPayments] = useState<PaymentIntentDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [selectedCurrency, setSelectedCurrency] = useState<string>("ALL")

  const allBalances = useMemo(() => {
    if (!merchant || balances.length === 0) return balances
    const map = new Map(balances.map((b) => [b.currency, b]))
    return SUPPORTED_CURRENCIES.map((c) => map.get(c) ?? zeroBalance(c, merchant.id))
  }, [balances, merchant])

  const loadData = useCallback(async () => {
    if (!merchant) return
    const [balanceRes, paymentsRes] = await Promise.all([
      fetch(`/api/proxy/ledger/api/v1/ledger/balances?merchantId=${merchant.id}`),
      fetch(`/api/proxy/payment/api/v1/payments?merchantId=${merchant.id}&skip=0&take=5`),
    ])
    if (balanceRes.ok) setBalances(await balanceRes.json())
    if (paymentsRes.ok) setPayments(await paymentsRes.json())
  }, [merchant])

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

        const [balanceRes, paymentsRes] = await Promise.all([
          fetch(`/api/proxy/ledger/api/v1/ledger/balances?merchantId=${merchantData.id}`),
          fetch(`/api/proxy/payment/api/v1/payments?merchantId=${merchantData.id}&skip=0&take=5`),
        ])

        if (balanceRes.ok) setBalances(await balanceRes.json())
        if (paymentsRes.ok) setPayments(await paymentsRes.json())
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load dashboard")
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [])

  useEffect(() => {
    if (!merchant) return
    const interval = setInterval(loadData, 30000)
    return () => clearInterval(interval)
  }, [merchant, loadData])

  const allZero = allBalances.every((b) => b.available === 0 && b.pending === 0 && b.reserved === 0)

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-3xl font-semibold">Dashboard</h1>
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
        <h1 className="text-3xl font-semibold">Dashboard</h1>
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
          Welcome back{merchant ? `, ${merchant.businessName}` : ""}
        </p>
      </div>

      <div className="space-y-4">
        <div className="relative w-48">
          <select
            value={selectedCurrency}
            onChange={(e) => setSelectedCurrency(e.target.value)}
            className="w-full appearance-none rounded-lg border bg-card px-3 py-2 pr-8 text-sm outline-none focus:ring-2 focus:ring-primary"
          >
            <option value="ALL">All currencies</option>
            {SUPPORTED_CURRENCIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
          <ChevronDown className="pointer-events-none absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        </div>
        {(selectedCurrency === "ALL" ? allBalances : allBalances.filter((b) => b.currency === selectedCurrency)).map((b) => (
          <div key={b.currency}>
            <p className="mb-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              {b.currency}
            </p>
            <div className="grid gap-4 md:grid-cols-3">
              <StatsCard
                title="Available"
                value={`${(b.available / 100).toFixed(2)} ${b.currency}`}
                icon={Wallet}
                description="Ready for payout"
              />
              <StatsCard
                title="Pending"
                value={`${(b.pending / 100).toFixed(2)} ${b.currency}`}
                icon={Clock}
                description="Awaiting settlement"
              />
              <StatsCard
                title="Reserved"
                value={`${(b.reserved / 100).toFixed(2)} ${b.currency}`}
                icon={Lock}
                description="Held for disputes"
              />
            </div>
          </div>
        ))}
        {allZero && (
          <div className="rounded-xl border bg-card p-6 text-sm text-muted-foreground">
            No balance data yet. Create a payment to get started.
          </div>
        )}
      </div>

      <div className="rounded-xl border bg-card">
        <div className="flex items-center justify-between border-b px-6 py-4">
          <h2 className="font-semibold">Recent payments</h2>
        </div>
        {payments.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
            <CreditCard className="h-8 w-8" />
            <p className="text-sm">No payments yet</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="px-6 py-3 text-left font-medium">ID</th>
                  <th className="px-6 py-3 text-left font-medium">Amount</th>
                  <th className="px-6 py-3 text-left font-medium">Currency</th>
                  <th className="px-6 py-3 text-left font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {payments.map((p) => (
                  <tr key={p.intentId} className="border-b last:border-0 hover:bg-muted/50">
                    <td className="px-6 py-3 font-mono text-xs">{p.intentId.slice(0, 12)}...</td>
                    <td className="px-6 py-3">{(p.amount / 100).toFixed(2)}</td>
                    <td className="px-6 py-3">{p.currency}</td>
                    <td className="px-6 py-3">
                      <span className="inline-flex rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary">
                        {p.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
