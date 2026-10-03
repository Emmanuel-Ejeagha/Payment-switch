"use client"

import { useEffect, useState, useCallback } from "react"
import * as signalR from "@microsoft/signalr"

export interface PaymentEvent {
  eventType: string
  message: string
  timestamp: string
}

async function getAccessToken(): Promise<string> {
  const res = await fetch("/api/auth/token")
  if (!res.ok) throw new Error("Not authenticated")
  const data = (await res.json()) as { accessToken?: string }
  return data.accessToken ?? ""
}

export function useNotifications() {
  const [connected, setConnected] = useState(false)
  const [events, setEvents] = useState<PaymentEvent[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    let conn: signalR.HubConnection | null = null

    async function init() {
      // No refresh session (logged out, expired everywhere, or cookies lost):
      // realtime is unavailable. Stay quiet instead of spamming negotiation
      // failures and retry storms for a non-critical enhancement.
      try {
        await getAccessToken()
      } catch {
        if (!cancelled) setConnected(false)
        return
      }
      if (cancelled) return

      conn = new signalR.HubConnectionBuilder()
        .withUrl(
          `${process.env.NEXT_PUBLIC_API_URL}/notification/hubs/payment-notifications`,
          {
            withCredentials: false,
            accessTokenFactory: getAccessToken,
          }
        )
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
      .build()

    conn.on("PaymentEvent", (event: PaymentEvent) => {
      if (!cancelled) setEvents((prev) => [event, ...prev])
    })

    conn.onreconnecting(() => {
      if (!cancelled) setConnected(false)
    })
    conn.onreconnected(() => {
      if (!cancelled) setConnected(true)
    })
    conn.onclose(() => {
      if (!cancelled) setConnected(false)
    })

    async function start() {
      try {
        await conn!.start()
        if (cancelled) return
        setConnected(true)
        setError(null)
      } catch (err) {
        if (cancelled) return
        setConnected(false)
        setError(err instanceof Error ? err.message : "Connection failed")
      }
    }

    start()
    }

    init()

    return () => {
      cancelled = true
      conn?.stop()
    }
  }, [])

  const clearEvents = useCallback(() => setEvents([]), [])

  return { connected, events, error, clearEvents }
}
