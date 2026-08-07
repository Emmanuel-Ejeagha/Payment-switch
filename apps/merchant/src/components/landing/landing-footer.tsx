import Link from "next/link"
import { Store } from "lucide-react"

const columns = [
  {
    title: "Product",
    links: [
      { label: "Features", href: "#features" },
      { label: "How it works", href: "#how-it-works" },
      { label: "Pricing", href: "#pricing" },
      { label: "Payment links", href: "/register" },
    ],
  },
  {
    title: "Company",
    links: [
      { label: "Login", href: "/login" },
      { label: "Create account", href: "/register" },
    ],
  },
]

export function LandingFooter() {
  return (
    <footer className="border-t bg-muted/30">
      <div className="mx-auto max-w-6xl px-4 py-12 sm:px-6">
        <div className="grid gap-8 md:grid-cols-4">
          <div className="md:col-span-2">
            <div className="flex items-center gap-2">
              <span className="rounded-lg bg-primary/10 p-1.5">
                <Store className="h-5 w-5 text-primary" />
              </span>
              <span className="text-lg font-semibold">PaymentSwitch</span>
            </div>
            <p className="mt-3 max-w-sm text-sm text-muted-foreground">
              The payment switch that powers modern commerce — real-time payments,
              subscriptions, hosted checkout, and settlements on a distributed,
              event-driven core.
            </p>
          </div>

          {columns.map((col) => (
            <div key={col.title}>
              <h3 className="text-sm font-semibold">{col.title}</h3>
              <ul className="mt-3 space-y-2">
                {col.links.map((l) => (
                  <li key={l.label}>
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

        <div className="mt-10 flex flex-col items-center justify-between gap-3 border-t pt-6 text-xs text-muted-foreground sm:flex-row">
          <p>&copy; {new Date().getFullYear()} PaymentSwitch. All rights reserved.</p>
          <p>Built on .NET microservices, RabbitMQ, and Kubernetes.</p>
        </div>
      </div>
    </footer>
  )
}
