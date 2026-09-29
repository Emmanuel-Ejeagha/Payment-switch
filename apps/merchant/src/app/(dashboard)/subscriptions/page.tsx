"use client"

import { useEffect, useState, useCallback } from "react"
import Link from "next/link"
import { Ban, CalendarClock, Plus, RefreshCw, Repeat } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { SubscriptionDto, CustomerDto, PlanDto } from "@paymentswitch/shared"
import {
  Alert,
  Button,
  Card,
  CardHeader,
  EmptyState,
  Field,
  IdCell,
  Input,
  Modal,
  PageHeader,
  Select,
  StatusPill,
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
  useConfirm,
} from "@/components/ui"
import { formatAmount, formatDate, shortId } from "@/lib/format"

function extractError(body: unknown): string {
  if (!body || typeof body !== "object") return "Request failed"
  const b = body as Record<string, unknown>
  if (typeof b.message === "string") return b.message
  if (typeof b.detail === "string") return b.detail
  if (b.errors && typeof b.errors === "object") {
    const parts = Object.values(b.errors as Record<string, unknown>).flatMap((v) =>
      Array.isArray(v) ? v.map(String) : [String(v)]
    )
    if (parts.length) return parts.join(" ")
  }
  if (typeof b.title === "string") return b.title
  return "Request failed"
}

export default function SubscriptionsPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const { confirm, dialog } = useConfirm()
  const [subscriptions, setSubscriptions] = useState<SubscriptionDto[]>([])
  const [customers, setCustomers] = useState<CustomerDto[]>([])
  const [plans, setPlans] = useState<PlanDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const [showForm, setShowForm] = useState(false)
  const [customerId, setCustomerId] = useState("")
  const [planId, setPlanId] = useState("")
  const [cardToken, setCardToken] = useState("")

  const loadSubscriptions = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/subscriptions?merchantId=${merchantId}&skip=0&take=100`)
    if (res.ok) setSubscriptions(await res.json())
  }, [])

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    let cancelled = false
    async function load() {
      try {
        const [subsRes, custRes, plansRes] = await Promise.all([
          fetch(`/api/proxy/payment/api/v1/subscriptions?merchantId=${merchantId}&skip=0&take=100`),
          fetch(`/api/proxy/payment/api/v1/customers?merchantId=${merchantId}&skip=0&take=100`),
          fetch(`/api/proxy/payment/api/v1/plans?merchantId=${merchantId}&skip=0&take=100`),
        ])
        if (cancelled) return
        if (subsRes.ok) setSubscriptions(await subsRes.json())
        if (custRes.ok) setCustomers(await custRes.json())
        if (plansRes.ok) setPlans(await plansRes.json().then((p) => p.filter((x: PlanDto) => x.active)))
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load subscriptions")
      } finally {
        if (!cancelled) setDataReady(true)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [merchantLoading, merchantId])

  const createSubscription = async () => {
    if (!merchantId || !customerId || !planId || !cardToken) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch("/api/proxy/payment/api/v1/subscriptions", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ merchantId, customerId, planId, cardToken }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => undefined)
        setError(extractError(body))
        return
      }
      await loadSubscriptions(merchantId)
      setShowForm(false)
      setCustomerId("")
      setPlanId("")
      setCardToken("")
    } finally {
      setWorking(false)
    }
  }

  const cancelSubscription = async (subscription: SubscriptionDto, atPeriodEnd: boolean) => {
    if (!merchantId) return
    const confirmed = await confirm({
      title: atPeriodEnd ? "Cancel at period end" : "Cancel immediately",
      message: atPeriodEnd ? (
        <>
          <strong className="font-medium text-foreground">{subscription.code}</strong> keeps its access
          until {formatDate(subscription.currentPeriodEnd)} and is not billed again after that.
        </>
      ) : (
        <>
          <strong className="font-medium text-foreground">{subscription.code}</strong> ends right now.
          The current period is not refunded automatically.
        </>
      ),
      confirmLabel: atPeriodEnd ? "Cancel at period end" : "Cancel now",
      cancelLabel: "Keep subscription",
      destructive: !atPeriodEnd,
    })
    if (!confirmed) return

    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/subscriptions/${subscription.id}/cancel?atPeriodEnd=${atPeriodEnd}`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => undefined)
        setError(extractError(body))
        return
      }
      await loadSubscriptions(merchantId)
    } finally {
      setWorking(false)
    }
  }

  const collectNow = async (id: string) => {
    if (!merchantId) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/subscriptions/${id}/collect`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => undefined)
        setError(extractError(body))
        return
      }
      await loadSubscriptions(merchantId)
    } finally {
      setWorking(false)
    }
  }

  const blocked = customers.length === 0 || plans.length === 0

  return (
    <div className="space-y-8">
      <PageHeader
        title="Subscriptions"
        description="Recurring billing for your customers. Each subscription pairs a customer with a plan and bills on that plan's schedule."
        actions={
          <Button
            variant="primary"
            icon={Plus}
            onClick={() => setShowForm(true)}
            disabled={!merchantId}
          >
            New subscription
          </Button>
        }
      />

      {(error || merchantError) && (
        <Alert variant="error" title="Something went wrong">
          {error || merchantError}
        </Alert>
      )}

      {!loading && blocked && (
        <Alert variant="info" title="Two things are needed first">
          A subscription needs a customer and an active plan.{" "}
          {customers.length === 0 && (
            <>
              You have no customers —{" "}
              <Link href="/customers" className="font-medium text-primary underline">
                add one
              </Link>
              .{" "}
            </>
          )}
          {plans.length === 0 && (
            <>
              You have no active plans —{" "}
              <Link href="/plans" className="font-medium text-primary underline">
                create one
              </Link>
              .
            </>
          )}
        </Alert>
      )}

      <Card className="overflow-hidden">
        <CardHeader
          title="All subscriptions"
          description={loading ? "Loading…" : `${subscriptions.length} total`}
          icon={RefreshCw}
        />
        {loading ? (
          <TableSkeleton rows={5} columns={7} />
        ) : subscriptions.length === 0 ? (
          <EmptyState
            icon={RefreshCw}
            title="No subscriptions yet"
            description="Attach a customer to a plan and PaymentSwitch bills them automatically on every cycle."
            action={
              <Button
                variant="primary"
                icon={Plus}
                onClick={() => setShowForm(true)}
                disabled={!merchantId}
              >
                Create a subscription
              </Button>
            }
          />
        ) : (
          <TableWrap>
            <THead>
              <TH>Code</TH>
              <TH>Status</TH>
              <TH>Customer</TH>
              <TH>Plan</TH>
              <TH>Current period</TH>
              <TH>Next billing</TH>
              <TH align="right">Actions</TH>
            </THead>
            <TBody>
              {subscriptions.map((s) => {
                const customer = customers.find((c) => c.id === s.customerId)
                const plan = plans.find((p) => p.id === s.planId)
                const canAct = s.status === "Active" && !s.cancelAtPeriodEnd
                return (
                  <TR key={s.id}>
                    <TD>
                      <IdCell>{s.code}</IdCell>
                    </TD>
                    <TD>
                      <StatusPill status={s.status} />
                      {s.cancelAtPeriodEnd && (
                        <p className="mt-1 text-[11px] text-muted-foreground">Ends at period end</p>
                      )}
                    </TD>
                    <TD>{customer?.email ?? <IdCell>{shortId(s.customerId)}</IdCell>}</TD>
                    <TD>
                      {plan ? (
                        <>
                          <p className="font-medium">{plan.name}</p>
                          <p className="tabular text-xs text-muted-foreground">
                            {formatAmount(plan.amount, plan.currency)}
                          </p>
                        </>
                      ) : (
                        <IdCell>{shortId(s.planId)}</IdCell>
                      )}
                    </TD>
                    <TD className="whitespace-nowrap text-xs text-muted-foreground">
                      {formatDate(s.currentPeriodStart)} → {formatDate(s.currentPeriodEnd)}
                    </TD>
                    <TD className="whitespace-nowrap text-xs text-muted-foreground">
                      {formatDate(s.nextBillingAt)}
                    </TD>
                    <TD align="right">
                      <div className="flex items-center justify-end gap-1.5">
                        {canAct ? (
                          <>
                            <Button
                              size="sm"
                              icon={Repeat}
                              iconOnly
                              disabled={working}
                              onClick={() => collectNow(s.id)}
                            >
                              Collect {s.code} now
                            </Button>
                            <Button
                              size="sm"
                              icon={CalendarClock}
                              iconOnly
                              disabled={working}
                              onClick={() => cancelSubscription(s, true)}
                            >
                              Cancel {s.code} at period end
                            </Button>
                            <Button
                              size="sm"
                              variant="dangerGhost"
                              icon={Ban}
                              iconOnly
                              disabled={working}
                              onClick={() => cancelSubscription(s, false)}
                            >
                              Cancel {s.code} immediately
                            </Button>
                          </>
                        ) : (
                          <span className="text-xs text-muted-foreground">—</span>
                        )}
                      </div>
                    </TD>
                  </TR>
                )
              })}
            </TBody>
          </TableWrap>
        )}
      </Card>

      {showForm && (
        <Modal
          title="New subscription"
          description="Billing starts immediately and repeats on the plan's schedule."
          onClose={() => setShowForm(false)}
          size="lg"
          footer={
            <>
              <Button onClick={() => setShowForm(false)}>Cancel</Button>
              <Button
                variant="primary"
                onClick={createSubscription}
                pending={working}
                disabled={!customerId || !planId || !cardToken}
              >
                {working ? "Creating…" : "Create subscription"}
              </Button>
            </>
          }
        >
          <div className="space-y-4">
            <Field label="Customer" htmlFor="sub-customer" required>
              {customers.length === 0 ? (
                <div className="rounded-lg border border-dashed bg-muted/40 px-3 py-2.5 text-xs text-muted-foreground">
                  No customers yet.{" "}
                  <Link href="/customers" className="font-medium text-primary underline">
                    Create one
                  </Link>{" "}
                  before adding a subscription.
                </div>
              ) : (
                <Select
                  id="sub-customer"
                  value={customerId}
                  onChange={(e) => setCustomerId(e.target.value)}
                >
                  <option value="">Select customer</option>
                  {customers.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.email} ({c.code})
                    </option>
                  ))}
                </Select>
              )}
            </Field>

            <Field label="Plan" htmlFor="sub-plan" required>
              {plans.length === 0 ? (
                <div className="rounded-lg border border-dashed bg-muted/40 px-3 py-2.5 text-xs text-muted-foreground">
                  No active plans yet.{" "}
                  <Link href="/plans" className="font-medium text-primary underline">
                    Create one
                  </Link>{" "}
                  before adding a subscription.
                </div>
              ) : (
                <Select id="sub-plan" value={planId} onChange={(e) => setPlanId(e.target.value)}>
                  <option value="">Select plan</option>
                  {plans.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} — {formatAmount(p.amount, p.currency)}
                    </option>
                  ))}
                </Select>
              )}
            </Field>

            <Field
              label="Card token"
              htmlFor="sub-token"
              required
              hint="A vault token from the tokenize endpoint. Raw card numbers never pass through this form."
            >
              <Input
                id="sub-token"
                type="text"
                value={cardToken}
                onChange={(e) => setCardToken(e.target.value)}
                placeholder="tok_…"
                autoComplete="off"
                spellCheck={false}
                className="font-mono"
              />
            </Field>
          </div>
        </Modal>
      )}

      {dialog}
    </div>
  )
}
