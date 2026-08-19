"use client"

import { useEffect, useState, useCallback } from "react"
import { Archive, Package, Plus } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { PlanDto } from "@paymentswitch/shared"
import {
  Alert,
  AmountInput,
  Badge,
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
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
  useConfirm,
} from "@/components/ui"
import { formatAmount, formatDate, formatInterval } from "@/lib/format"

const SUPPORTED_CURRENCIES = ["USD", "EUR", "GBP", "NGN"]
const INTERVAL_UNITS = ["Day", "Week", "Month", "Year"]

export default function PlansPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const { confirm, dialog } = useConfirm()
  const [plans, setPlans] = useState<PlanDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const [showForm, setShowForm] = useState(false)
  const [name, setName] = useState("")
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("USD")
  const [intervalUnit, setIntervalUnit] = useState("Month")
  const [intervalCount, setIntervalCount] = useState("1")
  const [description, setDescription] = useState("")

  const loadPlans = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/plans?merchantId=${merchantId}&skip=0&take=100`)
    if (res.ok) setPlans(await res.json())
  }, [])

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    const mid = merchantId
    async function load() {
      try {
        await loadPlans(mid)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load plans")
      } finally {
        setDataReady(true)
      }
    }
    load()
  }, [merchantLoading, merchantId, loadPlans])

  const createPlan = async () => {
    if (!merchantId || !name || !amount) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch("/api/proxy/payment/api/v1/plans", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId,
          name,
          amount: Math.round(parseFloat(amount) * 100),
          currency,
          intervalUnit,
          intervalCount: parseInt(intervalCount, 10) || 1,
          description: description || null,
        }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Failed to create plan")
        return
      }
      await loadPlans(merchantId)
      setShowForm(false)
      setName("")
      setAmount("")
      setDescription("")
      setIntervalCount("1")
    } finally {
      setWorking(false)
    }
  }

  const archivePlan = async (plan: PlanDto) => {
    if (!merchantId) return
    const confirmed = await confirm({
      title: "Archive plan",
      message: (
        <>
          <strong className="font-medium text-foreground">{plan.name}</strong> will stop accepting new
          subscribers. Existing subscriptions keep billing on their current schedule.
        </>
      ),
      confirmLabel: "Archive plan",
    })
    if (!confirmed) return

    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/plans/${plan.id}/archive`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Failed to archive plan")
        return
      }
      await loadPlans(merchantId)
    } finally {
      setWorking(false)
    }
  }

  const activeCount = plans.filter((p) => p.active).length

  return (
    <div className="space-y-8">
      <PageHeader
        title="Plans"
        description="Recurring billing templates. A plan sets the amount and cadence; subscriptions attach a customer to one."
        actions={
          <Button variant="primary" icon={Plus} onClick={() => setShowForm(true)} disabled={!merchantId}>
            New plan
          </Button>
        }
      />

      {(error || merchantError) && (
        <Alert variant="error" title="Something went wrong">
          {error || merchantError}
        </Alert>
      )}

      <Card className="overflow-hidden">
        <CardHeader
          title="Your plans"
          description={loading ? "Loading…" : `${activeCount} active of ${plans.length}`}
          icon={Package}
        />
        {loading ? (
          <TableSkeleton rows={5} columns={6} />
        ) : plans.length === 0 ? (
          <EmptyState
            icon={Package}
            title="No plans yet"
            description="Create a plan to bill customers on a repeating schedule — monthly, yearly, or any interval you choose."
            action={
              <Button variant="primary" icon={Plus} onClick={() => setShowForm(true)} disabled={!merchantId}>
                Create your first plan
              </Button>
            }
          />
        ) : (
          <TableWrap>
            <THead>
              <TH>Code</TH>
              <TH>Plan</TH>
              <TH align="right">Amount</TH>
              <TH>Billing</TH>
              <TH>Status</TH>
              <TH>Created</TH>
              <TH align="right">Actions</TH>
            </THead>
            <TBody>
              {plans.map((p) => (
                <TR key={p.id}>
                  <TD>
                    <IdCell>{p.code}</IdCell>
                  </TD>
                  <TD>
                    <p className="font-medium">{p.name}</p>
                    {p.description && (
                      <p className="max-w-[18rem] truncate text-xs text-muted-foreground">
                        {p.description}
                      </p>
                    )}
                  </TD>
                  <TD align="right" className="tabular whitespace-nowrap font-medium">
                    {formatAmount(p.amount, p.currency)}
                  </TD>
                  <TD className="whitespace-nowrap text-muted-foreground">
                    {formatInterval(p.intervalCount, p.intervalUnit)}
                  </TD>
                  <TD>
                    <Badge tone={p.active ? "success" : "neutral"} dot>
                      {p.active ? "Active" : "Archived"}
                    </Badge>
                  </TD>
                  <TD className="whitespace-nowrap text-xs text-muted-foreground">
                    {formatDate(p.createdAt)}
                  </TD>
                  <TD align="right">
                    {p.active ? (
                      <Button
                        size="sm"
                        icon={Archive}
                        disabled={working}
                        onClick={() => archivePlan(p)}
                      >
                        Archive
                      </Button>
                    ) : (
                      <span className="text-xs text-muted-foreground">—</span>
                    )}
                  </TD>
                </TR>
              ))}
            </TBody>
          </TableWrap>
        )}
      </Card>

      {showForm && (
        <Modal
          title="New plan"
          description="Amount and interval are fixed once subscribers are attached."
          onClose={() => setShowForm(false)}
          size="lg"
          footer={
            <>
              <Button onClick={() => setShowForm(false)}>Cancel</Button>
              <Button
                variant="primary"
                icon={Package}
                onClick={createPlan}
                pending={working}
                disabled={!name || !amount}
              >
                {working ? "Creating…" : "Create plan"}
              </Button>
            </>
          }
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Name" htmlFor="plan-name" required className="sm:col-span-2">
              <Input
                id="plan-name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Pro monthly"
              />
            </Field>
            <Field label="Amount" htmlFor="plan-amount" required>
              <AmountInput
                id="plan-amount"
                currency={currency}
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="29.99"
              />
            </Field>
            <Field label="Currency" htmlFor="plan-currency">
              <Select id="plan-currency" value={currency} onChange={(e) => setCurrency(e.target.value)}>
                {SUPPORTED_CURRENCIES.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </Select>
            </Field>
            <Field
              label="Bills every"
              htmlFor="plan-interval-count"
              hint={`Currently: ${formatInterval(parseInt(intervalCount, 10) || 1, intervalUnit)}`}
            >
              <Input
                id="plan-interval-count"
                type="number"
                min="1"
                value={intervalCount}
                onChange={(e) => setIntervalCount(e.target.value)}
                className="tabular"
              />
            </Field>
            <Field label="Interval" htmlFor="plan-interval-unit">
              <Select
                id="plan-interval-unit"
                value={intervalUnit}
                onChange={(e) => setIntervalUnit(e.target.value)}
              >
                {INTERVAL_UNITS.map((u) => (
                  <option key={u} value={u}>{u}</option>
                ))}
              </Select>
            </Field>
            <Field
              label="Description"
              htmlFor="plan-description"
              hint="Optional. Helps you tell similar plans apart."
              className="sm:col-span-2"
            >
              <Input
                id="plan-description"
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Full access, unlimited seats"
              />
            </Field>
          </div>
        </Modal>
      )}

      {dialog}
    </div>
  )
}
