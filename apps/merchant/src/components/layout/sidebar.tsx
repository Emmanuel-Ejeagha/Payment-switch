"use client"

import Link from "next/link"
import { useRouter, usePathname } from "next/navigation"
import {
  Home,
  CreditCard,
  BookOpen,
  Key,
  Settings,
  User,
  LogOut,
  Store,
  X,
  Sun,
  Moon,
  Bell,
} from "lucide-react"
import { useTheme } from "@/components/theme-provider"
import { useNotifications } from "@/components/use-notifications"
import { useEffect, useState } from "react"
import type { MerchantDto, UserDto } from "@paymentswitch/shared"

const navItems = [
  { label: "Dashboard", href: "/", icon: Home },
  { label: "Payments", href: "/payments", icon: CreditCard },
  { label: "Ledger", href: "/ledger", icon: BookOpen },
  { label: "API Keys", href: "/api-keys", icon: Key },
  { label: "Profile", href: "/profile", icon: User },
  { label: "Settings", href: "/settings", icon: Settings },
]

interface SidebarProps {
  open: boolean
  onClose: () => void
}

export function Sidebar({ open, onClose }: SidebarProps) {
  const router = useRouter()
  const pathname = usePathname()
  const { theme, toggle } = useTheme()
  const [merchantId, setMerchantId] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) return
        const userData: UserDto = await userRes.json()
        const merchantRes = await fetch(`/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(userData.email)}`)
        if (!merchantRes.ok) return
        const merchantData: MerchantDto = await merchantRes.json()
        setMerchantId(merchantData.id)
      } catch {}
    }
    load()
  }, [])

  const { connected, events } = useNotifications(merchantId)

  const handleLogout = async () => {
    await fetch("/api/auth/logout", { method: "POST" })
    router.push("/login")
  }

  const nav = (
    <>
      <div className="flex h-14 items-center justify-between border-b px-6">
        <div className="flex items-center gap-3">
          <Store className="h-5 w-5" />
          <span className="font-semibold">PaymentSwitch</span>
        </div>
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
      <div className="border-t p-4 space-y-2">
        {connected && events.length > 0 && (
          <div className="flex items-center gap-2 rounded-lg border border-primary/20 bg-primary/5 px-3 py-2 text-xs text-muted-foreground">
            <Bell className="h-3.5 w-3.5 text-primary" />
            <span className="truncate">{events[0].message}</span>
          </div>
        )}
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
