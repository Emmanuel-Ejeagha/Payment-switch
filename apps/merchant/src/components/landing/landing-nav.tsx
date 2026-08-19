"use client"

import Link from "next/link"
import { useEffect, useState } from "react"
import { Sun, Moon, Menu, X, ArrowRight } from "lucide-react"
import { useTheme } from "@/components/theme-provider"
import { BrandMark } from "@/components/landing/brand-mark"

const links = [
  { label: "Features", href: "#features" },
  { label: "How it works", href: "#how-it-works" },
  { label: "Customers", href: "#customers" },
  { label: "Pricing", href: "#pricing" },
]

export function LandingNav() {
  const { toggle } = useTheme()
  const [open, setOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8)
    onScroll()
    window.addEventListener("scroll", onScroll, { passive: true })
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  // A resize past the md breakpoint leaves the mobile panel mounted but its
  // trigger hidden, so close it when the desktop nav takes over.
  useEffect(() => {
    const mq = window.matchMedia("(min-width: 768px)")
    const onChange = () => {
      if (mq.matches) setOpen(false)
    }
    mq.addEventListener("change", onChange)
    return () => mq.removeEventListener("change", onChange)
  }, [])

  return (
    <header
      className={`sticky top-0 z-50 transition-shadow duration-200 ${
        scrolled
          ? "border-b bg-background/85 shadow-subtle backdrop-blur-xl"
          : "border-b border-transparent bg-background/60 backdrop-blur"
      }`}
    >
      <nav className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
        <Link
          href="/"
          className="flex shrink-0 items-center gap-2.5 rounded-lg"
          aria-label="PaymentSwitch home"
        >
          <BrandMark />
          <span className="text-[15px] font-semibold tracking-tight">PaymentSwitch</span>
        </Link>

        <div className="hidden items-center gap-1 md:flex">
          {links.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className="rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            >
              {l.label}
            </a>
          ))}
        </div>

        <div className="hidden items-center gap-2 md:flex">
          <button
            type="button"
            onClick={toggle}
            aria-label="Toggle colour theme"
            className="rounded-lg border border-transparent p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
          >
            <Moon className="h-4 w-4 dark:hidden" aria-hidden="true" />
            <Sun className="hidden h-4 w-4 dark:block" aria-hidden="true" />
          </button>
          <Link
            href="/login"
            className="rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
          >
            Sign in
          </Link>
          <Link
            href="/register"
            className="inline-flex h-9 items-center gap-1.5 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-subtle transition-colors hover:bg-primary/90"
          >
            Get started
            <ArrowRight className="h-3.5 w-3.5" aria-hidden="true" />
          </Link>
        </div>

        <button
          type="button"
          onClick={() => setOpen(!open)}
          aria-label={open ? "Close menu" : "Open menu"}
          aria-expanded={open}
          aria-controls="mobile-nav"
          className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground md:hidden"
        >
          {open ? (
            <X className="h-5 w-5" aria-hidden="true" />
          ) : (
            <Menu className="h-5 w-5" aria-hidden="true" />
          )}
        </button>
      </nav>

      {open && (
        <div
          id="mobile-nav"
          className="animate-fade-in border-t bg-background px-4 pb-5 pt-2 md:hidden"
        >
          <div className="flex flex-col">
            {links.map((l) => (
              <a
                key={l.href}
                href={l.href}
                onClick={() => setOpen(false)}
                className="rounded-lg px-3 py-2.5 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
              >
                {l.label}
              </a>
            ))}
          </div>

          <div className="mt-4 flex items-center gap-2 border-t pt-4">
            <button
              type="button"
              onClick={toggle}
              aria-label="Toggle colour theme"
              className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            >
              <Moon className="h-4 w-4 dark:hidden" aria-hidden="true" />
              <Sun className="hidden h-4 w-4 dark:block" aria-hidden="true" />
            </button>
            <Link
              href="/login"
              onClick={() => setOpen(false)}
              className="rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            >
              Sign in
            </Link>
            <Link
              href="/register"
              onClick={() => setOpen(false)}
              className="ml-auto inline-flex h-9 items-center gap-1.5 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
              Get started
              <ArrowRight className="h-3.5 w-3.5" aria-hidden="true" />
            </Link>
          </div>
        </div>
      )}
    </header>
  )
}
