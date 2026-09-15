"use client"

import { useEffect, useState, useCallback } from "react"
import * as signalR from "@microsoft/signalr"

export interface PaymentEvent {
  eventType: string
  message: string
  timestamp: string
}

async function getAccessToken(): Promise<string | null> {
  const res = await fetch("/api/auth/token")
  if (!res.ok) return null
  const data = (await res.json()) as { accessToken?: string }
  return data.accessToken ?? null
}

export function useNotifications() {
  const [connected, setConnected] = useState(false)
  const [events, setEvents] = useState<PaymentEvent[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    let conn: signalR.HubConnection | null = null

    async function init() {
      const token = await getAccessToken()
      if (cancelled) return
      if (!token) {
        setError(null)
        setConnected(false)
        return
      }

      conn = new signalR.HubConnectionBuilder()
        .withUrl(
          `${process.env.NEXT_PUBLIC_API_URL}/notification/hubs/payment-notifications`,
          {
            withCredentials: false,
            accessTokenFactory: () => token,
          }
        )
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      conn.on("PaymentEvent", (event: PaymentEvent) => {
        setEvents((prev) => [event, ...prev])
      })

      conn.onreconnecting(() => setConnected(false))
      conn.onreconnected(() => setConnected(true))
      conn.onclose(() => setConnected(false))

      try {
        await conn.start()
        if (!cancelled) {
          setConnected(true)
          setError(null)
        }
      } catch (err) {
        if (!cancelled) {
          setConnected(false)
          setError(err instanceof Error ? err.message : "Connection failed")
        }
      }
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
