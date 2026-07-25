"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import {
  Home,
  CreditCard,
  BookOpen,
  Key,
  Settings,
  LogOut,
  Store,
  X,
} from "lucide-react"

const navItems = [
  { label: "Dashboard", href: "/", icon: Home },
  { label: "Payments", href: "/payments", icon: CreditCard },
  { label: "Ledger", href: "/ledger", icon: BookOpen },
  { label: "API Keys", href: "/api-keys", icon: Key },
  { label: "Settings", href: "/settings", icon: Settings },
]

interface SidebarProps {
  open: boolean
  onClose: () => void
}

export function Sidebar({ open, onClose }: SidebarProps) {
  const router = useRouter()

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
            className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          >
            <item.icon className="h-4 w-4" />
            {item.label}
          </Link>
        ))}
      </nav>
      <div className="border-t p-4">
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
