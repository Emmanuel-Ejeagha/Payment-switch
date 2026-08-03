"use client"

import Link from "next/link"
import { useRouter, usePathname } from "next/navigation"
import {
  Home,
  Store,
  CreditCard,
  BookOpen,
  Bell,
  Banknote,
  Shield,
  LogOut,
  Sun,
  Moon,
  X,
} from "lucide-react"
import { useTheme } from "@/components/theme-provider"
import { useEffect, useState } from "react"
import * as signalR from "@microsoft/signalr"

interface PaymentEvent {
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

const navItems = [
  { label: "Dashboard", href: "/", icon: Home },
  { label: "Merchants", href: "/merchants", icon: Store },
  { label: "Payments", href: "/payments", icon: CreditCard },
  { label: "Ledger", href: "/ledger", icon: BookOpen },
  { label: "Settlements", href: "/settlements", icon: Banknote },
  { label: "Notifications", href: "/notifications", icon: Bell },
  { label: "Admin", href: "/admin", icon: Shield },
]

interface SidebarProps {
  open: boolean
  onClose: () => void
}

export function Sidebar({ open, onClose }: SidebarProps) {
  const router = useRouter()
  const pathname = usePathname()
  const { theme, toggle } = useTheme()
  const [events, setEvents] = useState<PaymentEvent[]>([])
  const [connected, setConnected] = useState(false)
  const [showNotifications, setShowNotifications] = useState(false)

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
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
      setEvents((prev) => [event, ...prev])
    })

    conn.onreconnecting(() => setConnected(false))
    conn.onreconnected(() => setConnected(true))
    conn.onclose(() => setConnected(false))

    async function start() {
      try {
        await conn.start()
        setConnected(true)
      } catch {
        setConnected(false)
      }
    }
    start()

    return () => { conn.stop() }
  }, [])

  const handleLogout = async () => {
    await fetch("/api/auth/logout", { method: "POST" })
    router.push("/login")
  }

  const nav = (
    <>
      <div className="flex h-14 items-center justify-between border-b px-6">
        <span className="font-semibold">PaymentSwitch</span>
        <button onClick={onClose} className="rounded-lg p-1 hover:bg-accent md:hidden">
          <X className="h-5 w-5" />
        </button>
      </div>
      <nav className="flex-1 space-y-1 p-4">
        {navItems.map((item) => (
          <Link
            key={item.label}
            href={item.href}
            onClick={onClose}
            className={`flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors hover:bg-accent hover:text-accent-foreground ${
              pathname === item.href
                ? "bg-accent text-accent-foreground font-medium"
                : "text-muted-foreground"
            }`}
          >
            <item.icon className="h-4 w-4" />
            {item.label}
          </Link>
        ))}
      </nav>
      <div className="border-t p-4 space-y-1">
        <div className="relative">
          <button
            onClick={() => setShowNotifications((p) => !p)}
            className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          >
            <Bell className="h-4 w-4" />
            Notifications
            {events.length > 0 && (
              <span className="ml-auto inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-[11px] font-semibold text-primary-foreground">
                {events.length}
              </span>
            )}
            {connected && (
              <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" title="Connected" />
            )}
          </button>
          {showNotifications && events.length > 0 && (
            <div className="fixed bottom-20 left-16 z-50 max-h-64 w-72 overflow-y-auto rounded-lg border bg-card shadow-lg">
              <div className="flex items-center justify-between border-b px-3 py-2">
                <span className="text-xs font-semibold">Notifications</span>
                <button
                  onClick={() => setShowNotifications(false)}
                  className="text-muted-foreground hover:text-foreground"
                >
                  <X className="h-3 w-3" />
                </button>
              </div>
              {events.slice(0, 10).map((ev, i) => (
                <div key={i} className="border-b px-3 py-2 text-xs last:border-0">
                  <p className="font-medium">{ev.eventType}</p>
                  <p className="text-muted-foreground">{ev.message}</p>
                </div>
              ))}
            </div>
          )}
        </div>
        <button
          onClick={toggle}
          className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          {theme === "light" ? <Moon className="h-4 w-4" /> : <Sun className="h-4 w-4" />}
          {theme === "light" ? "Dark mode" : "Light mode"}
        </button>
        <button
          onClick={handleLogout}
          className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          <LogOut className="h-4 w-4" />
          Sign out
        </button>
      </div>
    </>
  )

  return (
    <>
      {open && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={onClose}
        />
      )}
      <aside
        className={`fixed inset-y-0 left-0 z-50 flex w-60 flex-col border-r bg-card transition-transform duration-200 md:hidden ${
          open ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        {nav}
      </aside>
      <aside className="hidden md:flex md:h-full md:w-60 md:flex-col md:border-r md:bg-card">
        {nav}
      </aside>
    </>
  )
}
