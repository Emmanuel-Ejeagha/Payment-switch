import {
  CreditCard,
  Link2,
  RefreshCw,
  Bell,
  Webhook,
  BookOpen,
  Activity,
  Wallet,
  Zap,
} from "lucide-react"

const stats = [
  { value: "6", label: "Event-driven services", icon: Activity },
  { value: "24/7", label: "Live notifications", icon: Bell },
  { value: "1.5%", label: "Flat processing fee", icon: Wallet },
  { value: "<200ms", label: "Median authorisation", icon: Zap },
]

export function LandingStats() {
  return (
    <section className="border-y bg-muted/30">
      <div className="mx-auto grid max-w-6xl grid-cols-2 divide-y divide-border px-4 sm:px-6 md:grid-cols-4 md:divide-x md:divide-y-0">
        {stats.map((s) => (
          <div
            key={s.label}
            className="flex flex-col items-center gap-1.5 px-4 py-8 text-center"
          >
            <s.icon className="h-4 w-4 text-primary" aria-hidden="true" />
            <span className="tabular text-2xl font-semibold tracking-tight">{s.value}</span>
            <span className="text-xs text-muted-foreground">{s.label}</span>
          </div>
        ))}
      </div>
    </section>
  )
}

const features = [
  {
    icon: CreditCard,
    title: "Payments & intents",
    description:
      "Authorise, capture, void, and refund through idempotent commands, with the full transaction history on every payment.",
  },
  {
    icon: Link2,
    title: "Payment links & checkout",
    description:
      "Share a link, get paid on a hosted page with card tokenisation and 3-D Secure. No frontend work required.",
  },
  {
    icon: RefreshCw,
    title: "Subscriptions & billing",
    description:
      "Plans, customers, invoices, and automatic recurring charges — dunning and retries handled for you.",
  },
  {
    icon: Webhook,
    title: "Signed webhooks",
    description:
      "HMAC-signed deliveries with automatic retries, a searchable event log, and a send-test-event endpoint.",
  },
  {
    icon: Bell,
    title: "Real-time notifications",
    description:
      "Payment events stream to your dashboard over a live SignalR connection the moment they happen.",
  },
  {
    icon: BookOpen,
    title: "Settlement & ledger",
    description:
      "A double-entry, event-sourced ledger with daily settlement runs and traceable merchant payouts.",
  },
]

export function LandingFeatures() {
  return (
    <section id="features" className="scroll-mt-24">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6 sm:py-24">
        <div className="mx-auto max-w-2xl text-center">
          <p className="text-xs font-semibold uppercase tracking-wider text-primary">
            Platform
          </p>
          <h2 className="mt-3 text-3xl font-semibold tracking-tight sm:text-4xl">
            Everything you need to get paid
          </h2>
          <p className="mt-4 text-muted-foreground">
            A complete payments stack, from the first checkout to the final payout,
            managed from one dashboard.
          </p>
        </div>

        <div className="mt-14 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {features.map((f) => (
            <div
              key={f.title}
              className="group relative rounded-2xl border bg-card p-6 shadow-subtle transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-card"
            >
              <span className="inline-flex rounded-xl bg-primary/10 p-2.5 text-primary transition-colors group-hover:bg-primary group-hover:text-primary-foreground">
                <f.icon className="h-5 w-5" aria-hidden="true" />
              </span>
              <h3 className="mt-4 font-semibold tracking-tight">{f.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                {f.description}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
