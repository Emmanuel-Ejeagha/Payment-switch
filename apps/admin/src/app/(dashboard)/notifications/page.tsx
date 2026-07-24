"use client"

import { useEffect, useState } from "react"
import { Bell, ChevronLeft, ChevronRight, Mail, Globe, Clock, RefreshCw, CheckCircle2 } from "lucide-react"
import type { NotificationDto } from "@paymentswitch/shared"

const channelIcons: Record<string, typeof Mail> = {
  Email: Mail,
  Webhook: Globe,
}

const statusColors: Record<string, string> = {
  Sent: "bg-emerald-500/10 text-emerald-600",
  Pending: "bg-amber-500/10 text-amber-600",
  Failed: "bg-red-500/10 text-red-600",
  Cancelled: "bg-muted text-muted-foreground",
}

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([])
  const [loading, setLoading] = useState(true)
  const [skip, setSkip] = useState(0)
  const [statusFilter, setStatusFilter] = useState("")
  const [channelFilter, setChannelFilter] = useState("")
  const take = 10

  useEffect(() => {
    async function load() {
      setLoading(true)
      const params = new URLSearchParams({ skip: String(skip), take: String(take) })
      if (statusFilter) params.set("status", statusFilter)
      if (channelFilter) params.set("channel", channelFilter)
      const res = await fetch(`/api/proxy/notification/api/v1/notification?${params}`)
      if (res.ok) setNotifications(await res.json())
      setLoading(false)
    }
    load()
  }, [skip, statusFilter, channelFilter])

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Notifications</h1>
        <p className="text-sm text-muted-foreground">
          Email and webhook notification history
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setSkip(0) }}
          className="flex h-10 rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          <option value="">All statuses</option>
          <option value="Sent">Sent</option>
          <option value="Pending">Pending</option>
          <option value="Failed">Failed</option>
        </select>
        <select
          value={channelFilter}
          onChange={(e) => { setChannelFilter(e.target.value); setSkip(0) }}
          className="flex h-10 rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          <option value="">All channels</option>
          <option value="Email">Email</option>
          <option value="Webhook">Webhook</option>
        </select>
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-24 animate-pulse rounded-xl bg-muted" />
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <div className="flex flex-col items-center rounded-xl border border-dashed py-16 text-center">
          <Bell className="mb-3 h-8 w-8 text-muted-foreground" />
          <p className="font-medium">No notifications</p>
          <p className="text-sm text-muted-foreground">
            {statusFilter || channelFilter ? "Try different filters" : "Notifications will appear here"}
          </p>
        </div>
      ) : (
        <>
          <div className="space-y-3">
            {notifications.map((n) => {
              const ChannelIcon = channelIcons[n.channel] || Mail
              return (
                <div key={n.id} className="rounded-xl border p-5 hover:bg-muted/20 transition-colors">
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex items-center gap-3">
                      <div className="rounded-lg bg-muted p-2">
                        <ChannelIcon className="h-4 w-4 text-muted-foreground" />
                      </div>
                      <div>
                        <p className="font-medium">{n.recipient}</p>
                        <p className="text-xs text-muted-foreground">{n.channel}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      {n.retryCount > 0 && (
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <RefreshCw className="h-3 w-3" />
                          {n.retryCount}
                        </div>
                      )}
                      <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${statusColors[n.status] || "bg-muted text-muted-foreground"}`}>
                        {n.status}
                      </span>
                    </div>
                  </div>
                  {n.subject && (
                    <p className="text-sm font-medium mb-1">{n.subject}</p>
                  )}
                  <div className="flex items-center gap-4 text-xs text-muted-foreground">
                    <div className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {new Date(n.createdAt).toLocaleString()}
                    </div>
                    {n.processedAt && (
                      <div className="flex items-center gap-1">
                        <CheckCircle2 className="h-3 w-3" />
                        {new Date(n.processedAt).toLocaleString()}
                      </div>
                    )}
                    {n.nextRetryAt && (
                      <div className="flex items-center gap-1">
                        <RefreshCw className="h-3 w-3" />
                        Retry: {new Date(n.nextRetryAt).toLocaleString()}
                      </div>
                    )}
                  </div>
                </div>
              )
            })}
          </div>

          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <p>Showing {skip + 1}–{skip + notifications.length}</p>
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
                disabled={notifications.length < take}
                className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-accent disabled:opacity-50"
              >
                Next <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
