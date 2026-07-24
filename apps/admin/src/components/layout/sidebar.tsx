"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
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
  const { theme, toggle } = useTheme()

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
            className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          >
            <item.icon className="h-4 w-4" />
            {item.label}
          </Link>
        ))}
      </nav>
      <div className="border-t p-4 space-y-1">
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
      {/* Mobile overlay */}
      {open && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={onClose}
        />
      )}

      {/* Mobile drawer */}
      <aside
        className={`fixed inset-y-0 left-0 z-50 flex w-60 flex-col border-r bg-card transition-transform duration-200 md:static md:z-auto md:translate-x-0 ${
          open ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        {nav}
      </aside>

      {/* Desktop sidebar */}
      <aside className="hidden md:flex md:h-full md:w-60 md:flex-col md:border-r md:bg-card">
        {nav}
      </aside>
    </>
  )
}
