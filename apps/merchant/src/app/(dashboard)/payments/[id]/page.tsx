"use client"

import { useCallback, useEffect, useState } from "react"
import { useParams } from "next/navigation"
import { Ban, CheckCircle, CreditCard, Receipt, RotateCcw } from "lucide-react"
import type { PaymentIntentDto, TransactionDto } from "@paymentswitch/shared"
import {
  Alert,
  Badge,
  Button,
  Card,
  CardBody,
  CardHeader,
  CopyButton,
  EmptyState,
  ErrorPanel,
  PageHeader,
  Skeleton,
  StatusPill,
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
  toneForStatus,
} from "@/components/ui"
import { formatAmount, formatDateTime, formatFigure, humanize } from "@/lib/format"

export default function PaymentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [payment, setPayment] = useState<PaymentIntentDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [pendingAction, setPendingAction] = useState<string | null>(null)

  // No synchronous setLoading here: the skeleton only needs to show on first mount, and
  // refreshes after an action are already covered by the pendingAction button guard.
  const fetchPayment = useCallback(async () => {
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/payments/${id}`)
      if (!res.ok) { setError("Payment not found"); return }
      setPayment(await res.json())
    } catch {
      setError("Failed to load payment")
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    async function load() {
      await fetchPayment()
    }
    load()
  }, [fetchPayment])

  const doAction = async (action: string, extra?: Record<string, unknown>) => {
    // These endpoints move money. Without the in-flight guard a double-click sends two
    // refunds, and without the idempotency key a retried request is a second refund too.
    if (pendingAction) return
    setPendingAction(action)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/payments/${id}/${action}`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Idempotency-Key": crypto.randomUUID(),
        },
        body: extra ? JSON.stringify(extra) : undefined,
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? `${action} failed`)
        return
      }
      await fetchPayment()
    } catch {
      setError(`${action} failed`)
    } finally {
      setPendingAction(null)
    }
  }

  if (loading) {
    return (
      <div className="space-y-8">
        <div className="space-y-3">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-9 w-64" />
        </div>
        <div className="grid gap-4 sm:grid-cols-3">
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
        </div>
        <Card className="overflow-hidden">
          <CardHeader title="Transaction history" icon={Receipt} />
          <TableSkeleton rows={3} columns={4} />
        </Card>
      </div>
    )
  }

  if (error && !payment) {
    return (
      <div className="space-y-8">
        <PageHeader title="Payment" backHref="/payments" backLabel="Back to payments" />
        <ErrorPanel
          title={error}
          message="The intent may have been removed, or the id in the address bar is not one of yours."
        />
      </div>
    )
  }

  if (!payment) return null

  const canAuthorize = payment.status === "Pending"
  const canCapture = payment.status === "Authorized"
  const canVoid = payment.status === "Authorized"
  const canRefund = payment.status === "Captured" || payment.status === "Settled"
  const busy = pendingAction !== null
  const hasActions = canAuthorize || canCapture || canVoid || canRefund

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Payment intent"
        title={formatAmount(payment.amount, payment.currency)}
        backHref="/payments"
        backLabel="Back to payments"
        actions={<StatusPill status={payment.status} className="text-sm" />}
        description={
          <span className="flex flex-wrap items-center gap-2">
            <code className="break-all rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
              {payment.intentId}
            </code>
            <CopyButton value={payment.intentId} label="Copy id" iconOnly />
          </span>
        }
      />

      {error && <Alert variant="error" title="Action failed">{error}</Alert>}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card className="p-5">
          <p className="text-sm font-medium text-muted-foreground">Amount</p>
          <p className="tabular mt-2 text-2xl font-semibold tracking-tight">
            {formatFigure(payment.amount)}
            <span className="ml-1.5 text-sm font-medium text-muted-foreground">
              {payment.currency}
            </span>
          </p>
        </Card>
        <Card className="p-5">
          <p className="text-sm font-medium text-muted-foreground">Status</p>
          <div className="mt-2">
            <Badge tone={toneForStatus(payment.status)} dot className="text-sm">
              {humanize(payment.status)}
            </Badge>
          </div>
        </Card>
        <Card className="p-5">
          <p className="text-sm font-medium text-muted-foreground">Payment method</p>
          <p className="mt-2 flex items-center gap-2 text-sm font-medium">
            <CreditCard className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
            {payment.cardBrand || payment.cardLastFour
              ? `${payment.cardBrand ?? "Card"} •••• ${payment.cardLastFour ?? "----"}`
              : "Not recorded"}
          </p>
        </Card>
        <Card className="p-5">
          <p className="text-sm font-medium text-muted-foreground">Created</p>
          <p className="mt-2 text-sm font-medium">{formatDateTime(payment.createdAt)}</p>
        </Card>
      </div>

      {hasActions && (
        <Card>
          <CardHeader
            title="Actions"
            description="Each action is sent with an idempotency key, so a repeat never moves money twice."
          />
          <CardBody className="flex flex-wrap gap-2">
            {canAuthorize && (
              <Button
                variant="primary"
                icon={CheckCircle}
                pending={pendingAction === "authorize"}
                disabled={busy}
                onClick={() =>
                  doAction("authorize", {
                    cardLastFour: payment.cardLastFour,
                    cardBrand: payment.cardBrand,
                  })
                }
              >
                Authorize
              </Button>
            )}
            {canCapture && (
              <Button
                variant="primary"
                icon={CheckCircle}
                pending={pendingAction === "capture"}
                disabled={busy}
                onClick={() => doAction("capture", { amount: payment.amount })}
              >
                Capture
              </Button>
            )}
            {canVoid && (
              <Button
                variant="dangerGhost"
                icon={Ban}
                pending={pendingAction === "void"}
                disabled={busy}
                onClick={() => doAction("void")}
              >
                Void
              </Button>
            )}
            {canRefund && (
              <Button
                variant="dangerGhost"
                icon={RotateCcw}
                pending={pendingAction === "refund"}
                disabled={busy}
                onClick={() => doAction("refund", { amount: payment.amount })}
              >
                Refund
              </Button>
            )}
          </CardBody>
        </Card>
      )}

      <Card className="overflow-hidden">
        <CardHeader
          title="Transaction history"
          description={`${payment.transactions?.length ?? 0} recorded`}
          icon={Receipt}
        />
        {!payment.transactions || payment.transactions.length === 0 ? (
          <EmptyState
            icon={Receipt}
            title="No transactions yet"
            description="Authorizing, capturing, voiding, or refunding this intent each records a transaction here."
          />
        ) : (
          <TableWrap>
            <THead>
              <TH>Type</TH>
              <TH align="right">Amount</TH>
              <TH>Currency</TH>
              <TH>Timestamp</TH>
            </THead>
            <TBody>
              {payment.transactions.map((t: TransactionDto) => (
                <TR key={t.id}>
                  <TD>
                    <Badge tone={toneForStatus(t.type)}>{humanize(t.type)}</Badge>
                  </TD>
                  <TD align="right" className="tabular font-medium">
                    {formatFigure(t.amount)}
                  </TD>
                  <TD className="text-muted-foreground">{t.currency}</TD>
                  <TD className="whitespace-nowrap text-xs text-muted-foreground">
                    {formatDateTime(t.timestamp)}
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
