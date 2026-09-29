"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { CreditCard, Plus } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { PaymentIntentDto } from "@paymentswitch/shared"
import { useToast } from "@/components/toast"
import {
  Alert,
  AmountInput,
  Button,
  Card,
  CardHeader,
  EmptyState,
  ErrorPanel,
  Field,
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
} from "@/components/ui"
import { formatDateTime, formatFigure, shortId } from "@/lib/format"

export default function PaymentsPage() {
  const { showToast } = useToast()
  const { merchant, loading: merchantLoading } = useMerchant()
  const [payments, setPayments] = useState<PaymentIntentDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const loading = merchantLoading || (merchant !== null && !dataReady)

  useEffect(() => {
    if (merchantLoading) return
    if (!merchant) return
    const m = merchant
    let cancelled = false
    async function load() {
      setLoadError(null)
      try {
        const paymentsRes = await fetch(`/api/proxy/payment/api/v1/payments?merchantId=${m.id}&skip=0&take=10`)
        if (cancelled) return
        if (paymentsRes.ok) {
          setPayments(await paymentsRes.json())
        } else {
          setLoadError(`Failed to load payments (${paymentsRes.status})`)
        }
      } catch {
        if (!cancelled) setLoadError("Failed to load payments")
      } finally {
        if (!cancelled) setDataReady(true)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [merchantLoading, merchant])

  return (
    <div className="space-y-8">
      <PageHeader
        title="Payments"
        description="Every payment intent on your account, newest first. Open one to authorize, capture, void, or refund it."
        actions={
          <Button variant="primary" icon={Plus} onClick={() => setShowCreate(true)} disabled={!merchant}>
            Create payment
          </Button>
        }
      />

      <Card className="overflow-hidden">
        <CardHeader
          title="Payment intents"
          description={loading ? "Loading…" : `${payments.length} shown`}
          icon={CreditCard}
        />
        {loading ? (
          <TableSkeleton rows={6} columns={6} />
        ) : loadError && payments.length === 0 ? (
          <ErrorPanel title="Failed to load payments" message={loadError} />
        ) : payments.length === 0 ? (
          <EmptyState
            icon={CreditCard}
            title="No payments yet"
            description="Create a payment intent to start moving money. You can authorize and capture it in two steps, or capture straight away."
            action={
              <Button variant="primary" icon={Plus} onClick={() => setShowCreate(true)} disabled={!merchant}>
                Create payment
              </Button>
            }
          />
        ) : (
          <TableWrap>
            <THead>
              <TH>Intent</TH>
              <TH align="right">Amount</TH>
              <TH>Currency</TH>
              <TH>Status</TH>
              <TH align="right">Transactions</TH>
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
                    {p.cardBrand && p.cardLastFour && (
                      <p className="mt-0.5 text-[11px] text-muted-foreground">
                        {p.cardBrand} •••• {p.cardLastFour}
                      </p>
                    )}
                  </TD>
                  <TD align="right" className="tabular font-medium">
                    {formatFigure(p.amount)}
                  </TD>
                  <TD className="text-muted-foreground">{p.currency}</TD>
                  <TD>
                    <StatusPill status={p.status} />
                  </TD>
                  <TD align="right" className="tabular text-muted-foreground">
                    {p.transactions?.length ?? 0}
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

      {showCreate && merchant && (
        <CreatePaymentModal
          merchantId={merchant.id}
          onClose={() => setShowCreate(false)}
          onCreated={async (intentId) => {
            const res = await fetch(`/api/proxy/payment/api/v1/payments/${intentId}`)
            if (res.ok) {
              const p = await res.json()
              setPayments((prev) => [p, ...prev])
              showToast("success", "Payment created successfully")
            }
            setShowCreate(false)
          }}
        />
      )}
    </div>
  )
}

function CreatePaymentModal({
  merchantId,
  onClose,
  onCreated,
}: {
  merchantId: string
  onClose: () => void
  onCreated: (intentId: string) => void
}) {
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("USD")
  const [paymentMethod, setPaymentMethod] = useState("Card")
  const [cardLastFour, setCardLastFour] = useState("")
  const [cardBrand, setCardBrand] = useState("")
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      const res = await fetch("/api/proxy/payment/api/v1/payments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId,
          amount: Math.round(parseFloat(amount) * 100),
          currency,
          paymentMethod,
          ...(paymentMethod === "Card" ? { cardLastFour, cardBrand } : {}),
          idempotencyKey: crypto.randomUUID(),
        }),
      })

      if (!res.ok) {
        const body = await res.json()
        setError(body.title ?? body.detail ?? "Failed to create payment")
        return
      }

      const { intentId } = await res.json()
      onCreated(intentId)
    } catch {
      setError("Failed to create payment")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      title="Create payment"
      description="Creates a payment intent you can authorize and capture."
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" form="create-payment" variant="primary" pending={submitting}>
            {submitting ? "Creating…" : "Create payment"}
          </Button>
        </>
      }
    >
      <form id="create-payment" onSubmit={handleSubmit} className="space-y-4">
        <Field label="Amount" htmlFor="amount" required hint="Charged in the currency selected below.">
          <AmountInput
            id="amount"
            currency={currency}
            placeholder="0.00"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Currency" htmlFor="currency">
            <Select id="currency" value={currency} onChange={(e) => setCurrency(e.target.value)}>
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
              <option value="GBP">GBP</option>
              <option value="NGN">NGN</option>
            </Select>
          </Field>

          <Field label="Payment method" htmlFor="method">
            <Select id="method" value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)}>
              <option value="Card">Card</option>
              <option value="Bank">Bank transfer</option>
              <option value="MobileMoney">Mobile money</option>
            </Select>
          </Field>
        </div>

        {paymentMethod === "Card" && (
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Card (last 4)" htmlFor="last4" required>
              <Input
                id="last4"
                inputMode="numeric"
                maxLength={4}
                pattern="[0-9]{4}"
                placeholder="1234"
                value={cardLastFour}
                onChange={(e) => setCardLastFour(e.target.value.replace(/\D/g, "").slice(0, 4))}
                required
                className="tabular"
              />
            </Field>

            <Field label="Card brand" htmlFor="brand">
              <Select id="brand" value={cardBrand} onChange={(e) => setCardBrand(e.target.value)}>
                <option value="">Select</option>
                <option value="Visa">Visa</option>
                <option value="Mastercard">Mastercard</option>
                <option value="Amex">Amex</option>
              </Select>
            </Field>
          </div>
        )}

        {error && <Alert variant="error">{error}</Alert>}
      </form>
    </Modal>
  )
}
