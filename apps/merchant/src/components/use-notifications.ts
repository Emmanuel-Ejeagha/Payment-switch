"use client"

import { useEffect, useState, useCallback } from "react"
import * as signalR from "@microsoft/signalr"

export interface PaymentEvent {
  eventType: string
  message: string
  timestamp: string
}

export function useNotifications(merchantId: string | null) {
  const [connected, setConnected] = useState(false)
  const [events, setEvents] = useState<PaymentEvent[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!merchantId) return

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(
        `${process.env.NEXT_PUBLIC_API_URL}/notification/hubs/payment-notifications?merchantId=${merchantId}`,
        { withCredentials: false }
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

    async function start() {
      try {
        await conn.start()
        setConnected(true)
        setError(null)
      } catch (err) {
        setConnected(false)
        setError(err instanceof Error ? err.message : "Connection failed")
      }
    }

    start()

    return () => {
      conn.stop()
    }
  }, [merchantId])

  const clearEvents = useCallback(() => setEvents([]), [])

  return { connected, events, error, clearEvents }
}
