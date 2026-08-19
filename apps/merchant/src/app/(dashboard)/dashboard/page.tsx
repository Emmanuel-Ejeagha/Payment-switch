"use client"

import { useEffect, useState, useCallback, useMemo } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { ArrowUpRight, Clock, CreditCard, Hourglass, Lock, MailCheck, Plus, ThumbsDown, Wallet } from "lucide-react"
import type { UserDto, MerchantDto, BalanceDto, PaymentIntentDto } from "@paymentswitch/shared"
import {
  Card,
  CardHeader,
  ErrorPanel,
  EmptyState,
  IdCell,
  PageHeader,
  Select,
  SectionLabel,
  StatSkeleton,
  StatTile,
  StatusPill,
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
} from "@/components/ui"
import { formatDateTime, formatFigure, shortId } from "@/lib/format"

const SUPPORTED_CURRENCIES = ["USD", "EUR", "GBP", "NGN"]

function zeroBalance(currency: string, merchantId: string): BalanceDto {
  return { merchantId, available: 0, pending: 0, reserved: 0, currency }
}

export default function MerchantDashboardPage() {
  const router = useRouter()
  const [user, setUser] = useState<UserDto | null>(null)
  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [balances, setBalances] = useState<BalanceDto[]>([])
  const [payments, setPayments] = useState<PaymentIntentDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [selectedCurrency, setSelectedCurrency] = useState<string>("ALL")
  const [resending, setResending] = useState(false)
  const [resendMsg, setResendMsg] = useState<string | null>(null)

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
        if (!merchantRes.ok) {
          // Signed in but never onboarded (or onboarding failed at registration).
          // Send them somewhere they can fix it rather than a dead-end error.
          if (merchantRes.status === 404) { router.replace("/onboarding"); return }
          setError("Failed to load merchant profile"); setLoading(false); return
        }
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
  }, [router])

  useEffect(() => {
    if (!merchant) return
    const interval = setInterval(loadData, 30000)
    return () => clearInterval(interval)
  }, [merchant, loadData])

  const allZero = allBalances.every((b) => b.available === 0 && b.pending === 0 && b.reserved === 0)
  const visibleBalances =
    selectedCurrency === "ALL"
      ? allBalances
      : allBalances.filter((b) => b.currency === selectedCurrency)

  if (loading) {
    return (
      <div className="space-y-8">
        <PageHeader title="Dashboard" description="Loading your account…" />
        <StatSkeleton />
        <Card className="overflow-hidden">
          <CardHeader title="Recent payments" icon={CreditCard} />
          <TableSkeleton rows={5} columns={5} />
        </Card>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-8">
        <PageHeader title="Dashboard" />
        <ErrorPanel title="Failed to load dashboard" message={error} />
      </div>
    )
  }

  const firstName = user?.fullName?.split(" ")[0]

  const resendVerification = async () => {
    if (!user?.email) return
    setResending(true)
    setResendMsg(null)
    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/resend-verification`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email: user.email }),
        }
      )
      if (!res.ok) {
        const body = await res.json()
        setResendMsg(body.message ?? body.detail ?? "Could not resend the email. Try again shortly.")
        return
      }
      setResendMsg("A new verification email is on its way. Check your inbox.")
    } catch {
      setResendMsg("Backend unreachable. Please try again later.")
    } finally {
      setResending(false)
    }
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title={firstName ? `Welcome back, ${firstName}` : "Dashboard"}
        description={
          merchant
            ? `Balances and recent activity for ${merchant.businessName}.`
            : "Balances and recent activity for your account."
        }
        actions={
          <Link
            href="/payments"
            className="inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-sm font-medium text-primary-foreground shadow-subtle transition-colors hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" aria-hidden="true" />
            New payment
          </Link>
        }
      />

      {user && !user.emailConfirmed && (
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg border border-amber-600/30 bg-amber-600/10 p-4">
          <MailCheck className="h-5 w-5 shrink-0 text-amber-700 dark:text-amber-400" aria-hidden="true" />
          <div className="min-w-0 flex-1 text-sm text-amber-800 dark:text-amber-300">
            <p className="font-medium">Verify your email to unlock API keys</p>
            <p className="text-xs opacity-80">
              We sent a link to {user.email}. Confirm it before you can generate live or test keys.
            </p>
            {resendMsg && <p className="mt-1 text-xs">{resendMsg}</p>}
          </div>
          <button
            type="button"
            onClick={resendVerification}
            disabled={resending}
            className="rounded-md border border-amber-600/40 bg-amber-600/10 px-3 py-1.5 text-xs font-medium text-amber-700 transition-colors hover:bg-amber-600/20 disabled:opacity-60 dark:text-amber-400"
          >
            {resending ? "Sending…" : "Resend email"}
          </button>
        </div>
      )}

      {merchant?.status === "Pending" && (
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg border border-amber-600/30 bg-amber-600/10 p-4">
          <Hourglass className="h-5 w-5 shrink-0 text-amber-700 dark:text-amber-400" aria-hidden="true" />
          <div className="min-w-0 flex-1 text-sm text-amber-800 dark:text-amber-300">
            <p className="font-medium">Your account is under review</p>
            <p className="text-xs opacity-80">
              Our team is checking your details. You can browse the dashboard, but payments stay
              disabled until your account is approved and activated.
            </p>
          </div>
        </div>
      )}

      {merchant?.status === "Approved" && (
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg border border-sky-600/30 bg-sky-600/10 p-4">
          <MailCheck className="h-5 w-5 shrink-0 text-sky-700 dark:text-sky-400" aria-hidden="true" />
          <div className="min-w-0 flex-1 text-sm text-sky-800 dark:text-sky-300">
            <p className="font-medium">You&apos;re approved — almost live</p>
            <p className="text-xs opacity-80">
              An administrator is activating your account. You&apos;ll be able to accept payments as soon
              as that goes through.
            </p>
          </div>
        </div>
      )}

      {merchant?.status === "Rejected" && (
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg border border-red-600/30 bg-red-600/10 p-4">
          <ThumbsDown className="h-5 w-5 shrink-0 text-red-700 dark:text-red-400" aria-hidden="true" />
          <div className="min-w-0 flex-1 text-sm text-red-800 dark:text-red-300">
            <p className="font-medium">Your application was declined</p>
            <p className="text-xs opacity-80">
              {merchant.rejectionReason ||
                "Our team could not approve your business at this time. Contact support to appeal."}
            </p>
          </div>
        </div>
      )}

      {merchant?.status === "Suspended" && (
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg border border-red-600/30 bg-red-600/10 p-4">
          <Lock className="h-5 w-5 shrink-0 text-red-700 dark:text-red-400" aria-hidden="true" />
          <div className="min-w-0 flex-1 text-sm text-red-800 dark:text-red-300">
            <p className="font-medium">Your account is suspended</p>
            <p className="text-xs opacity-80">
              Payments are paused. Contact support to find out why and get reactivated.
            </p>
          </div>
        </div>
      )}

      <section className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <SectionLabel>Balances</SectionLabel>
          <div className="w-44">
            <Select
              value={selectedCurrency}
              onChange={(e) => setSelectedCurrency(e.target.value)}
              aria-label="Filter balances by currency"
              className="h-9"
            >
              <option value="ALL">All currencies</option>
              {SUPPORTED_CURRENCIES.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </Select>
          </div>
        </div>

        {allZero ? (
          <Card>
            <EmptyState
              icon={Wallet}
              title="No balance yet"
              description="Balances appear here once your first payment settles. Create a payment to see money move through the ledger."
              action={
                <Link
                  href="/payments"
                  className="inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-sm font-medium text-primary-foreground shadow-subtle transition-colors hover:bg-primary/90"
                >
                  <Plus className="h-4 w-4" aria-hidden="true" />
                  Create a payment
                </Link>
              }
            />
          </Card>
        ) : (
          visibleBalances.map((b) => (
            <div key={b.currency} className="space-y-3">
              <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                {b.currency}
              </p>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <StatTile
                  label="Available"
                  value={formatFigure(b.available)}
                  unit={b.currency}
                  icon={Wallet}
                  tone="success"
                  hint="Ready for payout"
                />
                <StatTile
                  label="Pending"
                  value={formatFigure(b.pending)}
                  unit={b.currency}
                  icon={Clock}
                  tone="warning"
                  hint="Awaiting settlement"
                />
                <StatTile
                  label="Reserved"
                  value={formatFigure(b.reserved)}
                  unit={b.currency}
                  icon={Lock}
                  tone="info"
                  hint="Held for disputes"
                />
              </div>
            </div>
          ))
        )}

        {!allZero && visibleBalances.length === 0 && (
          <Card>
            <EmptyState
              icon={Wallet}
              title={`No ${selectedCurrency} balance`}
              description="Switch the filter back to all currencies to see the accounts that do have a balance."
            />
          </Card>
        )}
      </section>

      <Card className="overflow-hidden">
        <CardHeader
          title="Recent payments"
          description="The five most recent payment intents"
          icon={CreditCard}
          action={
            <Link
              href="/payments"
              className="inline-flex items-center gap-1 text-sm font-medium text-primary transition-colors hover:text-primary/80"
            >
              View all
              <ArrowUpRight className="h-3.5 w-3.5" aria-hidden="true" />
            </Link>
          }
        />
        {payments.length === 0 ? (
          <EmptyState
            icon={CreditCard}
            title="No payments yet"
            description="Once you create a payment intent it shows up here with its live status."
            action={
              <Link
                href="/payments"
                className="inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-sm font-medium text-primary-foreground shadow-subtle transition-colors hover:bg-primary/90"
              >
                <Plus className="h-4 w-4" aria-hidden="true" />
                Create a payment
              </Link>
            }
          />
        ) : (
          <TableWrap>
            <THead>
              <TH>Intent</TH>
              <TH align="right">Amount</TH>
              <TH>Currency</TH>
              <TH>Status</TH>
              <TH>Created</TH>
            </THead>
            <TBody>
              {payments.map((p) => (
                <TR key={p.intentId}>
                  <TD>
                    <Link
                      href={`/payments/${p.intentId}`}
                      className="font-mono text-xs text-primary transition-colors hover:underline"
                    >
                      {shortId(p.intentId)}
                    </Link>
                  </TD>
                  <TD align="right" className="tabular font-medium">
                    {formatFigure(p.amount)}
                  </TD>
                  <TD>
                    <IdCell>{p.currency}</IdCell>
                  </TD>
                  <TD>
                    <StatusPill status={p.status} />
                  </TD>
                  <TD className="whitespace-nowrap text-xs text-muted-foreground">
                    {formatDateTime(p.createdAt)}
                  </TD>
                </TR>
              ))}
            </TBody>
          </TableWrap>
        )}
      </Card>
    </div>
  )
}
