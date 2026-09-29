import Link from "next/link"
import { ArrowLeft, Compass, LayoutDashboard, LifeBuoy } from "lucide-react"
import { BrandMark } from "@/components/landing/brand-mark"

const SUGGESTIONS = [
  { href: "/dashboard", label: "Dashboard", hint: "Balances and recent payments", icon: LayoutDashboard },
  { href: "/payments", label: "Payments", hint: "Every intent you have created", icon: Compass },
  { href: "/settings", label: "Settings", hint: "Webhooks, methods, capture", icon: LifeBuoy },
]

export default function NotFound() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-surface px-4 py-12">
      <div className="w-full max-w-lg space-y-6 text-center">
        <Link
          href="/"
          className="inline-flex items-center gap-2.5 transition-opacity hover:opacity-80"
        >
          <BrandMark className="h-8 w-8" />
          <span className="text-lg font-semibold tracking-tight">PaymentSwitch</span>
        </Link>

        <div className="rounded-2xl border bg-card p-8 shadow-card">
          <p className="text-5xl font-semibold tracking-tight text-primary sm:text-6xl">404</p>
          <h1 className="mt-3 text-xl font-semibold tracking-tight">Page not found</h1>
          <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
            The address you opened does not match anything here. It may have been renamed, or the
            link that brought you was incomplete.
          </p>

          <ul className="mt-6 space-y-2 text-left">
            {SUGGESTIONS.map(({ href, label, hint, icon: Icon }) => (
              <li key={href}>
                <Link
                  href={href}
                  className="flex items-center gap-3 rounded-lg border px-3.5 py-3 transition-colors hover:bg-accent"
                >
                  <span
                    className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground"
                    aria-hidden="true"
                  >
                    <Icon className="h-4 w-4" />
                  </span>
                  <span className="min-w-0">
                    <span className="block text-sm font-medium">{label}</span>
                    <span className="block text-xs text-muted-foreground">{hint}</span>
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        </div>

        <Link
          href="/"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          Back to the home page
        </Link>
      </div>
    </div>
  )
}
