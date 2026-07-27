"use client"

import { useEffect, useState, useCallback } from "react"
import * as signalR from "@microsoft/signalr"

export interface PaymentEvent {
  eventType: string
  message: string
  timestamp: string
}

export function useNotifications(merchantId: string | null) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [connected, setConnected] = useState(false)
  const [events, setEvents] = useState<PaymentEvent[]>([])

  useEffect(() => {
    if (!merchantId) return

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/notification/hubs/payment-notifications?merchantId=${merchantId}`)
      .withAutomaticReconnect()
      .build()

    conn.on("PaymentEvent", (event: PaymentEvent) => {
      setEvents((prev) => [event, ...prev])
    })

    async function start() {
      try {
        await conn.start()
        setConnected(true)
      } catch {
        setConnected(false)
      }
    }

    setConnection(conn)
    start()

    return () => {
      conn.stop()
    }
  }, [merchantId])

  const clearEvents = useCallback(() => setEvents([]), [])

  return { connected, events, clearEvents }
}
