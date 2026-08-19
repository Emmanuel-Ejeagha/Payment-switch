"use client"

import { useEffect, useState, useCallback, useMemo, Fragment } from "react"
import Link from "next/link"
import {
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Clock,
  RotateCcw,
  Send,
  Webhook,
  XCircle,
} from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { WebhookEventDto } from "@paymentswitch/shared"
import {
  Alert,
  Badge,
  Button,
  Card,
  CardHeader,
  EmptyState,
  PageHeader,
  StatSkeleton,
  StatTile,
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
  type Tone,
} from "@/components/ui"
import { formatDateTime } from "@/lib/format"

const STATUS_TONES: Record<string, Tone> = {
  Delivered: "success",
  Pending: "warning",
  Failed: "danger",
}

export default function WebhooksPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const [events, setEvents] = useState<WebhookEventDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const [expanded, setExpanded] = useState<string | null>(null)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const loadEvents = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/webhookevents?merchantId=${merchantId}&skip=0&take=100`)
    if (res.ok) setEvents(await res.json())
  }, [])

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    const mid = merchantId
    async function load() {
      try {
        await loadEvents(mid)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load webhook events")
      } finally {
        setDataReady(true)
      }
    }
    load()
  }, [merchantLoading, merchantId, loadEvents])

  const replay = async (id: string) => {
    if (!merchantId) return
    setWorking(true)
    setError(null)
    setNotice(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/webhookevents/${id}/replay?merchantId=${merchantId}`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Replay failed")
        return
      }
      setNotice("Event queued for redelivery.")
      await loadEvents(merchantId)
    } finally {
      setWorking(false)
    }
  }

  const sendTest = async () => {
    if (!merchantId) return
    setWorking(true)
    setError(null)
    setNotice(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/webhookevents/test?merchantId=${merchantId}`, { method: "POST" })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Failed to send test event")
        return
      }
      setNotice("Test event sent to your configured endpoint.")
      await loadEvents(merchantId)
    } finally {
      setWorking(false)
    }
  }

  const formatPayload = (payload: string) => {
    try {
      return JSON.stringify(JSON.parse(payload), null, 2)
    } catch {
      return payload
    }
  }

  const counts = useMemo(
    () => ({
      delivered: events.filter((e) => e.status === "Delivered").length,
      pending: events.filter((e) => e.status === "Pending").length,
      failed: events.filter((e) => e.status === "Failed").length,
    }),
    [events],
  )

  if (loading) {
    return (
      <div className="space-y-8">
        <PageHeader title="Webhooks" description="Delivery log for events sent to your endpoint." />
        <StatSkeleton count={3} />
        <Card className="overflow-hidden">
          <CardHeader title="Delivery log" icon={Webhook} />
          <TableSkeleton rows={5} columns={6} />
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="Webhooks"
        description={
          <>
            Every event PaymentSwitch has tried to deliver to your endpoint, with the exact payload
            we sent. Change the destination URL in{" "}
            <Link href="/settings" className="font-medium text-primary underline underline-offset-2">
              Settings
            </Link>
            .
          </>
        }
        actions={
          <Button
            variant="primary"
            icon={Send}
            onClick={sendTest}
            pending={working}
            disabled={!merchantId}
          >
            Send test event
          </Button>
        }
      />

      {(error || merchantError) && (
        <Alert variant="error" title="Something went wrong">
          {error || merchantError}
        </Alert>
      )}
      {notice && <Alert variant="success">{notice}</Alert>}

      {events.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-3">
          <StatTile
            label="Delivered"
            value={String(counts.delivered)}
            icon={CheckCircle2}
            tone="success"
            hint="Endpoint returned 2xx"
          />
          <StatTile
            label="Pending"
            value={String(counts.pending)}
            icon={Clock}
            tone="warning"
            hint="Queued or awaiting retry"
          />
          <StatTile
            label="Failed"
            value={String(counts.failed)}
            icon={XCircle}
            tone="danger"
            hint="Retries exhausted"
          />
        </div>
      )}

      <Card className="overflow-hidden">
        <CardHeader
          title="Delivery log"
          description={`${events.length} event${events.length === 1 ? "" : "s"}`}
          icon={Webhook}
        />
        {events.length === 0 ? (
          <EmptyState
            icon={Webhook}
            title="No webhook events yet"
            description="Once an endpoint is configured, every payment event we send lands here with its full payload and delivery status."
            action={
              <Button
                variant="primary"
                icon={Send}
                onClick={sendTest}
                pending={working}
                disabled={!merchantId}
              >
                Send a test event
              </Button>
            }
          />
        ) : (
          <TableWrap>
            <THead>
              <TH className="w-10 px-3 sm:px-4">
                <span className="sr-only">Expand</span>
              </TH>
              <TH>Event</TH>
              <TH>Status</TH>
              <TH align="right">Attempts</TH>
              <TH>Created</TH>
              <TH>Delivered</TH>
              <TH align="right">Actions</TH>
            </THead>
            <TBody>
              {events.map((ev) => {
                const isOpen = expanded === ev.id
                return (
                  <Fragment key={ev.id}>
                    <TR className={isOpen ? "bg-muted/40" : undefined}>
                      <TD className="px-3 sm:px-4">
                        <button
                          type="button"
                          onClick={() => setExpanded(isOpen ? null : ev.id)}
                          aria-expanded={isOpen}
                          aria-controls={`payload-${ev.id}`}
                          className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
                        >
                          {isOpen ? (
                            <ChevronDown className="h-4 w-4" aria-hidden="true" />
                          ) : (
                            <ChevronRight className="h-4 w-4" aria-hidden="true" />
                          )}
                          <span className="sr-only">
                            {isOpen ? "Hide" : "Show"} payload for {ev.eventType}
                          </span>
                        </button>
                      </TD>
                      <TD>
                        <span className="font-mono text-xs">{ev.eventType}</span>
                      </TD>
                      <TD>
                        <Badge tone={STATUS_TONES[ev.status] ?? "neutral"} dot>
                          {ev.status}
                        </Badge>
                      </TD>
                      <TD align="right" className="tabular text-muted-foreground">
                        {ev.attemptCount}
                      </TD>
                      <TD className="whitespace-nowrap text-xs text-muted-foreground">
                        {formatDateTime(ev.createdAt)}
                      </TD>
                      <TD className="whitespace-nowrap text-xs text-muted-foreground">
                        {ev.deliveredAt ? formatDateTime(ev.deliveredAt) : "—"}
                      </TD>
                      <TD align="right">
                        {ev.status !== "Delivered" ? (
                          <Button
                            size="sm"
                            icon={RotateCcw}
                            disabled={working}
                            onClick={() => replay(ev.id)}
                          >
                            Replay
                          </Button>
                        ) : (
                          <span className="text-xs text-muted-foreground">—</span>
                        )}
                      </TD>
                    </TR>
                    {isOpen && (
                      <tr className="bg-muted/30">
                        <TD colSpan={7} id={`payload-${ev.id}`} className="px-5 py-4 sm:px-6">
                          <div className="space-y-3">
                            {ev.failureReason && (
                              <Alert variant="error" title="Last error">
                                {ev.failureReason}
                              </Alert>
                            )}
                            {ev.nextRetryAt && (
                              <p className="text-xs text-muted-foreground">
                                Next retry {formatDateTime(ev.nextRetryAt)}
                              </p>
                            )}
                            <div>
                              <p className="mb-1.5 text-xs font-medium text-muted-foreground">
                                Payload
                              </p>
                              <pre className="max-h-64 overflow-auto rounded-lg border bg-background p-3 font-mono text-xs leading-relaxed">
                                {formatPayload(ev.payload)}
                              </pre>
                            </div>
                          </div>
                        </TD>
                      </tr>
                    )}
                  </Fragment>
                )
              })}
            </TBody>
          </TableWrap>
        )}
      </Card>
    </div>
  )
}
