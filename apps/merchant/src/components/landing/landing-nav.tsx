"use client"

import Link from "next/link"
import { useState } from "react"
import { Store, Sun, Moon, Menu, X } from "lucide-react"
import { useTheme } from "@/components/theme-provider"

const links = [
  { label: "Features", href: "#features" },
  { label: "How it works", href: "#how-it-works" },
  { label: "Pricing", href: "#pricing" },
]

export function LandingNav() {
  const { toggle } = useTheme()
  const [open, setOpen] = useState(false)

  return (
    <header className="sticky top-0 z-50 border-b bg-background/80 backdrop-blur">
      <nav className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
        <Link href="/" className="flex items-center gap-2">
          <span className="rounded-lg bg-primary/10 p-1.5">
            <Store className="h-5 w-5 text-primary" />
          </span>
          <span className="text-lg font-semibold">PaymentSwitch</span>
        </Link>

        <div className="hidden items-center gap-6 md:flex">
          {links.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
            >
              {l.label}
            </a>
          ))}
        </div>

        <div className="hidden items-center gap-3 md:flex">
          <button
            onClick={toggle}
            aria-label="Toggle theme"
            className="rounded-lg p-2 hover:bg-accent"
          >
            <Moon className="h-4 w-4 dark:hidden" />
            <Sun className="hidden h-4 w-4 dark:block" />
          </button>
          <Link
            href="/login"
            className="text-sm font-medium text-muted-foreground hover:text-foreground"
          >
            Login
          </Link>
          <Link
            href="/register"
            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          >
            Create account
          </Link>
        </div>

        <button
          onClick={() => setOpen(!open)}
          aria-label="Toggle menu"
          className="rounded-lg p-2 hover:bg-accent md:hidden"
        >
          {open ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </button>
      </nav>

      {open && (
        <div className="border-t bg-background px-4 py-4 md:hidden">
          <div className="flex flex-col gap-3">
            {links.map((l) => (
              <a
                key={l.href}
                href={l.href}
                onClick={() => setOpen(false)}
                className="text-sm text-muted-foreground hover:text-foreground"
              >
                {l.label}
              </a>
            ))}
            <div className="mt-2 flex items-center gap-3 border-t pt-3">
              <button
                onClick={toggle}
                aria-label="Toggle theme"
                className="rounded-lg p-2 hover:bg-accent"
              >
                <Moon className="h-4 w-4 dark:hidden" />
                <Sun className="hidden h-4 w-4 dark:block" />
              </button>
              <Link
                href="/login"
                onClick={() => setOpen(false)}
                className="text-sm font-medium text-muted-foreground hover:text-foreground"
              >
                Login
              </Link>
              <Link
                href="/register"
                onClick={() => setOpen(false)}
                className="ml-auto rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Create account
              </Link>
            </div>
          </div>
        </div>
      )}
    </header>
  )
}
