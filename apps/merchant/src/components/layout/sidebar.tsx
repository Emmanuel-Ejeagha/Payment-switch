"use client"

import Link from "next/link"
import { useRouter, usePathname } from "next/navigation"
import { useEffect } from "react"
import { LogOut, Moon, Sun, X } from "lucide-react"
import { cn } from "@paymentswitch/shared"
import { useTheme } from "@/components/theme-provider"
import { BrandMark } from "@/components/landing/brand-mark"
import { isActivePath, navGroups } from "@/components/layout/nav-items"

interface SidebarProps {
  open: boolean
  onClose: () => void
}

function NavContents({ onNavigate }: { onNavigate?: () => void }) {
  const router = useRouter()
  const pathname = usePathname()
  const { toggle } = useTheme()

  const handleLogout = async () => {
    await fetch("/api/auth/logout", { method: "POST" })
    router.push("/login")
  }

  return (
    <>
      <div className="flex h-16 shrink-0 items-center px-5">
        <Link href="/dashboard" onClick={onNavigate} className="flex items-center gap-2.5 rounded-lg">
          <BrandMark className="h-8 w-8" />
          <span className="text-[15px] font-semibold tracking-tight">PaymentSwitch</span>
        </Link>
      </div>

      <nav aria-label="Main" className="flex-1 space-y-6 overflow-y-auto px-3 pb-4">
        {navGroups.map((group) => (
          <div key={group.label}>
            <p className="px-3 pb-1.5 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground/70">
              {group.label}
            </p>
            <ul className="space-y-0.5">
              {group.items.map((item) => {
                const active = isActivePath(pathname, item.href)
                return (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      onClick={onNavigate}
                      aria-current={active ? "page" : undefined}
                      className={cn(
                        "relative flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors",
                        active
                          ? "bg-primary/10 font-medium text-primary"
                          : "text-muted-foreground hover:bg-accent hover:text-accent-foreground",
                      )}
                    >
                      {active && (
                        <span
                          aria-hidden="true"
                          className="absolute left-0 top-1/2 h-5 w-0.5 -translate-y-1/2 rounded-r-full bg-primary"
                        />
                      )}
                      <item.icon className="h-4 w-4 shrink-0" aria-hidden="true" />
                      {item.label}
                    </Link>
                  </li>
                )
              })}
            </ul>
          </div>
        ))}
      </nav>

      <div className="shrink-0 space-y-0.5 border-t p-3">
        <button
          type="button"
          onClick={toggle}
          className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          <Moon className="h-4 w-4 shrink-0 dark:hidden" aria-hidden="true" />
          <Sun className="hidden h-4 w-4 shrink-0 dark:block" aria-hidden="true" />
          <span className="dark:hidden">Dark mode</span>
          <span className="hidden dark:inline">Light mode</span>
        </button>
        <button
          type="button"
          onClick={handleLogout}
          className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          <LogOut className="h-4 w-4 shrink-0" aria-hidden="true" />
          Sign out
        </button>
      </div>
    </>
  )
}

export function Sidebar({ open, onClose }: SidebarProps) {
  // The drawer used to stay mounted and merely translate off-screen, which left
  // all eleven links in the tab order while invisible — tabbing through the page
  // walked into a menu the user could not see. Mounting on demand avoids that.
  useEffect(() => {
    if (!open) return
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose()
    }
    document.addEventListener("keydown", onKeyDown)
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = "hidden"
    return () => {
      document.removeEventListener("keydown", onKeyDown)
      document.body.style.overflow = previousOverflow
    }
  }, [open, onClose])

  return (
    <>
      {open && (
        <div className="md:hidden">
          <div
            className="fixed inset-0 z-40 animate-fade-in bg-foreground/40 backdrop-blur-[2px]"
            onClick={onClose}
            aria-hidden="true"
          />
          <aside className="fixed inset-y-0 left-0 z-50 flex w-[17rem] flex-col border-r bg-card shadow-lift">
            <button
              type="button"
              onClick={onClose}
              className="absolute right-3 top-4 rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            >
              <X className="h-4 w-4" aria-hidden="true" />
              <span className="sr-only">Close navigation</span>
            </button>
            <NavContents onNavigate={onClose} />
          </aside>
        </div>
      )}

      <aside className="hidden w-64 shrink-0 flex-col border-r bg-card md:flex">
        <NavContents />
      </aside>
    </>
  )
}
