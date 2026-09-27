"use client"

import { usePathname } from "next/navigation"
import { useEffect, useRef, useState } from "react"
import { Bell, CheckCheck, Menu } from "lucide-react"
import { cn } from "@paymentswitch/shared"
import { useNotifications } from "@/components/use-notifications"
import { titleForPath } from "@/components/layout/nav-items"
import { formatDateTime } from "@/lib/format"

/**
 * Page title, mobile menu trigger, and the live-notifications popover.
 *
 * The popover used to live in the sidebar footer and was positioned with hard
 * viewport offsets (`fixed bottom-20 left-16`), so on any screen wide enough to
 * show the sidebar it opened on top of the sidebar itself. Anchoring it to a
 * button in the topbar means it is positioned relative to its trigger and cannot
 * collide with anything.
 */
export function Topbar({ onMenu }: { onMenu: () => void }) {
  const pathname = usePathname()
  const { connected, events, clearEvents } = useNotifications()
  const [open, setOpen] = useState(false)
  const popover = useRef<HTMLDivElement>(null)

  // A popover left open across a navigation hangs over the new page. Reset it
  // during render (the documented replacement for setState-in-effect) so the
  // popover never survives a route change.
  const [closedForPath, setClosedForPath] = useState(pathname)
  if (pathname !== closedForPath) {
    setClosedForPath(pathname)
    setOpen(false)
  }

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: MouseEvent) => {
      if (!popover.current?.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false)
    }
    document.addEventListener("mousedown", onPointerDown)
    document.addEventListener("keydown", onKeyDown)
    return () => {
      document.removeEventListener("mousedown", onPointerDown)
      document.removeEventListener("keydown", onKeyDown)
    }
  }, [open])

  return (
    <header className="relative z-30 flex h-16 shrink-0 items-center gap-3 border-b bg-background px-4 sm:px-6 lg:px-8">
      <button
        type="button"
        onClick={onMenu}
        className="-ml-1 rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground md:hidden"
      >
        <Menu className="h-5 w-5" aria-hidden="true" />
        <span className="sr-only">Open navigation</span>
      </button>

      <h1 className="truncate text-[15px] font-semibold tracking-tight">
        {titleForPath(pathname)}
      </h1>

      <div className="ml-auto flex items-center gap-2">
        <span className="hidden items-center gap-1.5 text-xs text-muted-foreground sm:flex">
          <span
            className={cn(
              "h-1.5 w-1.5 rounded-full",
              connected ? "bg-emerald-500" : "bg-muted-foreground/40",
            )}
            aria-hidden="true"
          />
          {connected ? "Live" : "Offline"}
        </span>

        <div className="relative" ref={popover}>
          <button
            type="button"
            onClick={() => setOpen((previous) => !previous)}
            aria-expanded={open}
            aria-haspopup="dialog"
            className="relative rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
          >
            <Bell className="h-[18px] w-[18px]" aria-hidden="true" />
            {events.length > 0 && (
              <span className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold leading-none text-primary-foreground">
                {events.length > 9 ? "9+" : events.length}
              </span>
            )}
            <span className="sr-only">
              Notifications{events.length > 0 ? ` (${events.length} new)` : ""}
            </span>
          </button>

          {open && (
            <div
              role="dialog"
              aria-label="Notifications"
              className="absolute right-0 top-full z-40 mt-2 w-[min(20rem,calc(100vw-2rem))] animate-fade-in overflow-hidden rounded-xl border bg-card shadow-lift"
            >
              <div className="flex items-center justify-between gap-2 border-b px-3.5 py-2.5">
                <p className="text-sm font-semibold">Notifications</p>
                {events.length > 0 && (
                  <button
                    type="button"
                    onClick={clearEvents}
                    className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                  >
                    <CheckCheck className="h-3.5 w-3.5" aria-hidden="true" />
                    Clear
                  </button>
                )}
              </div>

              {events.length === 0 ? (
                <div className="px-4 py-8 text-center">
                  <span
                    className="mx-auto flex h-9 w-9 items-center justify-center rounded-full bg-muted text-muted-foreground"
                    aria-hidden="true"
                  >
                    <Bell className="h-4 w-4" />
                  </span>
                  <p className="mt-2.5 text-sm font-medium">
                    {connected ? "You're all caught up" : "Connecting…"}
                  </p>
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    {connected
                      ? "Payment events appear here as they happen."
                      : "Live payment events will appear here once connected."}
                  </p>
                </div>
              ) : (
                <ul className="max-h-80 divide-y overflow-y-auto">
                  {events.slice(0, 20).map((event, index) => (
                    <li key={`${event.timestamp}-${index}`} className="px-3.5 py-2.5">
                      <p className="text-sm font-medium">{event.eventType}</p>
                      <p className="mt-0.5 text-xs text-muted-foreground">{event.message}</p>
                      <p className="mt-1 text-[11px] text-muted-foreground/70">
                        {formatDateTime(event.timestamp)}
                      </p>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
