"use client"

import { useEffect, useState, useCallback, Fragment } from "react"
import { Webhook, RotateCcw, Send, ChevronDown, ChevronRight } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { WebhookEventDto } from "@paymentswitch/shared"

const STATUS_STYLES: Record<string, string> = {
  Delivered: "bg-emerald-500/10 text-emerald-600",
  Pending: "bg-amber-500/10 text-amber-600",
  Failed: "bg-destructive/10 text-destructive",
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

  if (loading) {
    return <div className="h-64 animate-pulse rounded-xl bg-muted" />
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold">Webhooks</h1>
          <p className="text-sm text-muted-foreground">Delivery log for events sent to your endpoint.</p>
        </div>
        <button
          onClick={sendTest}
          disabled={working}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
        >
          <Send className="h-4 w-4" />
          Send test event
        </button>
      </div>

      {(error || merchantError) && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error || merchantError}</div>}
      {notice && <div className="rounded-lg bg-primary/10 p-3 text-sm text-primary">{notice}</div>}

      <div className="rounded-xl border bg-card">
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold">Delivery log</h2>
        </div>
        {events.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
            <Webhook className="h-8 w-8" />
            <p className="text-sm">No webhook events yet</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="w-8 px-3 py-3" />
                  <th className="px-6 py-3 text-left font-medium">Event</th>
                  <th className="px-6 py-3 text-left font-medium">Status</th>
                  <th className="px-6 py-3 text-left font-medium">Attempts</th>
                  <th className="px-6 py-3 text-left font-medium">Created</th>
                  <th className="px-6 py-3 text-left font-medium">Delivered</th>
                  <th className="px-6 py-3 text-left font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {events.map((ev) => (
                  <Fragment key={ev.id}>
                    <tr className="border-b last:border-0 hover:bg-muted/50">
                      <td className="px-3 py-3">
                        <button
                          onClick={() => setExpanded(expanded === ev.id ? null : ev.id)}
                          className="rounded p-1 hover:bg-accent"
                          aria-label={expanded === ev.id ? "Hide payload" : "Show payload"}
                        >
                          {expanded === ev.id ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                        </button>
                      </td>
                      <td className="px-6 py-3 font-mono text-xs">{ev.eventType}</td>
                      <td className="px-6 py-3">
                        <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_STYLES[ev.status] ?? "bg-muted text-muted-foreground"}`}>
                          {ev.status}
                        </span>
                      </td>
                      <td className="px-6 py-3">{ev.attemptCount}</td>
                      <td className="px-6 py-3">{new Date(ev.createdAt).toLocaleString()}</td>
                      <td className="px-6 py-3">{ev.deliveredAt ? new Date(ev.deliveredAt).toLocaleString() : "—"}</td>
                      <td className="px-6 py-3">
                        {ev.status !== "Delivered" && (
                          <button
                            onClick={() => replay(ev.id)}
                            disabled={working}
                            className="inline-flex items-center gap-1 rounded-md border px-2 py-1 text-xs hover:bg-accent disabled:opacity-50"
                          >
                            <RotateCcw className="h-3 w-3" />
                            Replay
                          </button>
                        )}
                      </td>
                    </tr>
                    {expanded === ev.id && (
                      <tr className="border-b last:border-0 bg-muted/30">
                        <td colSpan={7} className="px-6 py-4">
                          {ev.failureReason && (
                            <p className="mb-2 text-xs text-destructive">Last error: {ev.failureReason}</p>
                          )}
                          {ev.nextRetryAt && (
                            <p className="mb-2 text-xs text-muted-foreground">
                              Next retry: {new Date(ev.nextRetryAt).toLocaleString()}
                            </p>
                          )}
                          <pre className="max-h-64 overflow-auto rounded-lg bg-background p-3 font-mono text-xs">
                            {formatPayload(ev.payload)}
                          </pre>
                        </td>
                      </tr>
                    )}
                  </Fragment>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
