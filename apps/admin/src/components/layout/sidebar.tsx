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

export function Sidebar() {
  const router = useRouter()
  const { theme, toggle } = useTheme()

  const handleLogout = async () => {
    await fetch("/api/auth/logout", { method: "POST" })
    router.push("/login")
  }

  return (
    <aside className="flex h-full w-60 flex-col border-r bg-card">
      <div className="flex h-14 items-center border-b px-6 font-semibold">
        PaymentSwitch
      </div>
      <nav className="flex-1 space-y-1 p-4">
        {navItems.map((item) => (
          <Link
            key={item.label}
            href={item.href}
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
    </aside>
  )
}
