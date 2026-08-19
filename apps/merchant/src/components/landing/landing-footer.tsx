import Link from "next/link"
import { BrandMark } from "@/components/landing/brand-mark"

const columns = [
  {
    title: "Product",
    links: [
      { label: "Features", href: "#features" },
      { label: "How it works", href: "#how-it-works" },
      { label: "Customers", href: "#customers" },
      { label: "Pricing", href: "#pricing" },
    ],
  },
  {
    title: "Platform",
    links: [
      { label: "Hosted checkout", href: "/register" },
      { label: "Payment links", href: "/register" },
      { label: "Subscriptions", href: "/register" },
      { label: "Webhooks", href: "/register" },
    ],
  },
  {
    title: "Account",
    links: [
      { label: "Sign in", href: "/login" },
      { label: "Create account", href: "/register" },
    ],
  },
]

export function LandingFooter() {
  return (
    <footer className="border-t bg-muted/30">
      <div className="mx-auto max-w-6xl px-4 py-14 sm:px-6">
        <div className="grid gap-10 md:grid-cols-5">
          <div className="md:col-span-2">
            <Link
              href="/"
              className="flex items-center gap-2.5"
              aria-label="PaymentSwitch home"
            >
              <BrandMark />
              <span className="text-[15px] font-semibold tracking-tight">
                PaymentSwitch
              </span>
            </Link>
            <p className="mt-4 max-w-sm text-sm leading-relaxed text-muted-foreground">
              The payment switch behind modern commerce — real-time payments,
              subscriptions, hosted checkout, and settlement on a distributed,
              event-driven core.
            </p>
            <p className="mt-4 inline-flex items-center gap-2 rounded-full border bg-card px-3 py-1 text-xs text-muted-foreground">
              <span className="relative flex h-1.5 w-1.5" aria-hidden="true">
                <span className="absolute inline-flex h-full w-full animate-pulse-ring rounded-full bg-success" />
                <span className="relative inline-flex h-1.5 w-1.5 rounded-full bg-success" />
              </span>
              All systems operational
            </p>
          </div>

          {columns.map((col) => (
            <div key={col.title}>
              <h3 className="text-sm font-semibold tracking-tight">{col.title}</h3>
              <ul className="mt-4 space-y-2.5">
                {col.links.map((l) => (
                  <li key={`${col.title}-${l.label}`}>
                    <Link
                      href={l.href}
                      className="text-sm text-muted-foreground transition-colors hover:text-foreground"
                    >
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-12 flex flex-col items-center justify-between gap-3 border-t pt-6 text-xs text-muted-foreground sm:flex-row">
          <p>&copy; {new Date().getFullYear()} PaymentSwitch. All rights reserved.</p>
          <p>Built on .NET microservices, RabbitMQ, and Kubernetes.</p>
        </div>
      </div>
    </footer>
  )
}
