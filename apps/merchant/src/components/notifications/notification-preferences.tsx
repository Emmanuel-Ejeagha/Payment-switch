"use client"

import { useCallback, useEffect, useState } from "react"
import { Bell } from "lucide-react"
import type { NotificationPreferenceDto } from "@paymentswitch/shared"
import { Alert, Card, CardBody, CardHeader, Skeleton, Toggle } from "@/components/ui"
import { humanize } from "@/lib/format"

export const NOTIFICATION_CHANNELS = ["email", "sms", "webhook"] as const
export const NOTIFICATION_EVENT_TYPES = [
  "PaymentAuthorizedDomainEvent",
  "PaymentCapturedDomainEvent",
  "PaymentRefundedDomainEvent",
  "PaymentIntentCreatedDomainEvent",
  "PaymentVoidedDomainEvent",
] as const

type Channel = (typeof NOTIFICATION_CHANNELS)[number]
type EventType = (typeof NOTIFICATION_EVENT_TYPES)[number]

function buildDefaultState(): Record<Channel, Record<EventType, boolean>> {
  const state = {} as Record<Channel, Record<EventType, boolean>>
  for (const channel of NOTIFICATION_CHANNELS) {
    state[channel] = {} as Record<EventType, boolean>
    for (const eventType of NOTIFICATION_EVENT_TYPES) {
      state[channel][eventType] = true
    }
  }
  return state
}

export function NotificationPreferences() {
  const [prefs, setPrefs] = useState<Record<Channel, Record<EventType, boolean>>>(buildDefaultState)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [savingKey, setSavingKey] = useState<string | null>(null)
  const [status, setStatus] = useState<{ message: string; error: boolean } | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const res = await fetch("/api/proxy/notification/api/v1/notifications/preferences")
        if (!res.ok) {
          if (!cancelled) setError(`Failed to load preferences (${res.status})`)
          return
        }
        const data: NotificationPreferenceDto[] = await res.json()
        if (cancelled) return
        const next = buildDefaultState()
        for (const pref of data) {
          const channel = pref.channel as Channel
          const eventType = pref.eventType as EventType
          if (channel in next && eventType in next[channel]) {
            next[channel][eventType] = pref.enabled
          }
        }
        setPrefs(next)
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load preferences")
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [])

  const toggle = useCallback(async (channel: Channel, eventType: EventType) => {
    const key = `${channel}:${eventType}`
    const current = prefs[channel][eventType]
    setSavingKey(key)
    setStatus(null)
    try {
      const res = await fetch("/api/proxy/notification/api/v1/notifications/preferences", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channel, eventType, enabled: !current }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => null)
        setStatus({ message: body?.detail ?? body?.title ?? `Failed (${res.status})`, error: true })
        return
      }
      setPrefs((prev) => ({ ...prev, [channel]: { ...prev[channel], [eventType]: !current } }))
      setStatus({ message: "Preference saved", error: false })
    } catch {
      setStatus({ message: "Failed to save preference", error: true })
    } finally {
      setSavingKey(null)
    }
  }, [prefs])

  if (loading) {
    return (
      <Card>
        <CardHeader title="Notifications" description="What we send you and how" icon={Bell} />
        <CardBody className="space-y-3">
          <Skeleton className="h-10" />
          <Skeleton className="h-10" />
          <Skeleton className="h-10" />
        </CardBody>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardHeader title="Notifications" description="What we send you and how" icon={Bell} />
        <CardBody>
          <Alert variant="error" title="Could not load preferences">{error}</Alert>
        </CardBody>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader
        title="Notifications"
        description="Choose which payment events reach you and on which channel. Preferences apply to your account across the platform."
        icon={Bell}
      />
      <CardBody className="space-y-6">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[560px] text-sm">
            <thead>
              <tr className="border-b text-left text-xs text-muted-foreground">
                <th className="py-2 pr-4 font-medium">Event</th>
                {NOTIFICATION_CHANNELS.map((channel) => (
                  <th key={channel} className="py-2 px-4 font-medium text-center">
                    {humanize(channel)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y">
              {NOTIFICATION_EVENT_TYPES.map((eventType) => (
                <tr key={eventType}>
                  <td className="py-2.5 pr-4 font-medium">{humanize(eventType)}</td>
                  {NOTIFICATION_CHANNELS.map((channel) => (
                    <td key={channel} className="py-2.5 px-4 text-center">
                      <Toggle
                        checked={prefs[channel][eventType]}
                        disabled={savingKey === `${channel}:${eventType}`}
                        onChange={() => void toggle(channel, eventType)}
                        label={`${humanize(eventType)} via ${humanize(channel)}`}
                      />
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {status && (
          <Alert variant={status.error ? "error" : "success"}>{status.message}</Alert>
        )}
        <p className="text-xs text-muted-foreground">
          Disabling a channel stops every delivery on that channel. Payment failures and security
          alerts are always delivered by email regardless of these settings.
        </p>
      </CardBody>
    </Card>
  )
}