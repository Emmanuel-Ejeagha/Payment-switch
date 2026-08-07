import Link from "next/link"
import {
  CreditCard,
  Link2,
  RefreshCw,
  Bell,
  Webhook,
  BookOpen,
  Banknote,
  Zap,
  ArrowRight,
  ShieldCheck,
  Activity,
  Wallet,
} from "lucide-react"

export function LandingHero() {
  return (
    <section className="relative overflow-hidden">
      <div className="absolute inset-x-0 top-0 h-96 bg-gradient-to-b from-primary/10 to-transparent" />
      <div className="relative mx-auto max-w-6xl px-4 pb-20 pt-20 text-center sm:px-6 sm:pt-28">
        <span className="inline-flex items-center gap-2 rounded-full border bg-card px-3 py-1 text-xs font-medium text-muted-foreground">
          <Activity className="h-3.5 w-3.5 text-primary" />
          Real-time payment switch built on a distributed core
        </span>
        <h1 className="mx-auto mt-6 max-w-3xl text-4xl font-bold tracking-tight sm:text-5xl">
          Accept payments anywhere, built like a bank
        </h1>
        <p className="mx-auto mt-4 max-w-2xl text-base text-muted-foreground sm:text-lg">
          PaymentSwitch gives merchants a Stripe-style payments platform — hosted
          checkout, subscriptions, webhooks, and daily settlement — powered by six
          event-driven microservices.
        </p>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <Link
            href="/register"
            className="inline-flex h-11 items-center gap-2 rounded-lg bg-primary px-6 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          >
            Try the live demo <ArrowRight className="h-4 w-4" />
          </Link>
          <Link
            href="/login"
            className="inline-flex h-11 items-center rounded-lg border bg-card px-6 text-sm font-medium transition-colors hover:bg-accent"
          >
            Sign in
          </Link>
        </div>
      </div>
    </section>
  )
}

export function LandingStats() {
  const stats = [
    { value: "6", label: "Microservices", icon: Activity },
    { value: "24/7", label: "Real-time notifications", icon: Bell },
    { value: "1.5%", label: "Flat processing fee", icon: Wallet },
    { value: "100%", label: "Event-driven", icon: Zap },
  ]
  return (
    <section className="border-y bg-muted/30">
      <div className="mx-auto grid max-w-6xl grid-cols-2 gap-px px-4 py-8 sm:px-6 md:grid-cols-4">
        {stats.map((s) => (
          <div key={s.label} className="flex flex-col items-center gap-1 py-4 text-center">
            <s.icon className="h-4 w-4 text-primary" />
            <span className="text-2xl font-bold">{s.value}</span>
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
      "Authorize, capture, void, and refund with idempotent commands and full transaction history.",
  },
  {
    icon: Link2,
    title: "Payment links & checkout",
    description:
      "Create shareable payment links backed by a hosted checkout page with 3DS and card tokenization.",
  },
  {
    icon: RefreshCw,
    title: "Subscriptions & billing",
    description:
      "Recurring billing with plans, customers, invoices, and automatic recurring charges.",
  },
  {
    icon: Webhook,
    title: "Signed webhooks",
    description:
      "HMAC-signed event deliveries with retries, an event log, and a send-test-event API.",
  },
  {
    icon: Bell,
    title: "Real-time notifications",
    description:
      "Payment events push straight to your dashboard over a live SignalR connection.",
  },
  {
    icon: BookOpen,
    title: "Settlement & ledger",
    description:
      "A double-entry, event-sourced ledger with daily settlement and merchant payouts.",
  },
]

export function LandingFeatures() {
  return (
    <section id="features" className="mx-auto max-w-6xl px-4 py-20 sm:px-6">
      <div className="mx-auto max-w-2xl text-center">
        <h2 className="text-3xl font-bold tracking-tight">Everything you need to get paid</h2>
        <p className="mt-3 text-muted-foreground">
          A full payments stack — from checkout to settlement — managed from one dashboard.
        </p>
      </div>
      <div className="mt-12 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {features.map((f) => (
          <div key={f.title} className="rounded-xl border bg-card p-6">
            <span className="inline-flex rounded-lg bg-primary/10 p-2">
              <f.icon className="h-5 w-5 text-primary" />
            </span>
            <h3 className="mt-4 font-semibold">{f.title}</h3>
            <p className="mt-2 text-sm text-muted-foreground">{f.description}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

const steps = [
  {
    icon: Zap,
    title: "Create a payment",
    description:
      "Create a payment intent or a shareable payment link from your dashboard in seconds.",
  },
  {
    icon: CreditCard,
    title: "Your customer pays",
    description:
      "They check out on a hosted page with tokenized cards, 3DS, and instant feedback.",
  },
  {
    icon: Banknote,
    title: "Money settles",
    description:
      "Funds flow through a double-entry ledger and settle to your payout at end of day.",
  },
]

export function LandingHowItWorks() {
  return (
    <section id="how-it-works" className="border-y bg-muted/30">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6">
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="text-3xl font-bold tracking-tight">How it works</h2>
          <p className="mt-3 text-muted-foreground">
            From first payment to payout in three steps.
          </p>
        </div>
        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {steps.map((s, i) => (
            <div key={s.title} className="relative rounded-xl border bg-card p-6">
              <span className="text-xs font-semibold text-primary">Step {i + 1}</span>
              <div className="mt-3 flex items-center gap-2">
                <s.icon className="h-5 w-5 text-primary" />
                <h3 className="font-semibold">{s.title}</h3>
              </div>
              <p className="mt-2 text-sm text-muted-foreground">{s.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

const plans = [
  {
    name: "Starter",
    price: "$0",
    period: "to start",
    description: "Go live in minutes. Pay as you go.",
    features: ["1.5% per transaction", "Hosted checkout", "Payment links", "Dashboard"],
    highlighted: false,
  },
  {
    name: "Growth",
    price: "$29",
    period: "/month",
    description: "For scaling businesses.",
    features: [
      "Everything in Starter",
      "Subscriptions & invoices",
      "Webhook analytics",
      "Priority routing",
    ],
    highlighted: true,
  },
  {
    name: "Scale",
    price: "Custom",
    period: "",
    description: "For high-volume platforms.",
    features: ["Custom settlement cycles", "Dedicated support", "Multi-currency"],
    highlighted: false,
  },
]

export function LandingPricing() {
  return (
    <section id="pricing" className="mx-auto max-w-6xl px-4 py-20 sm:px-6">
      <div className="mx-auto max-w-2xl text-center">
        <h2 className="text-3xl font-bold tracking-tight">Simple, transparent pricing</h2>
        <p className="mt-3 text-muted-foreground">
          Start free. Upgrade when you grow.
        </p>
      </div>
      <div className="mt-12 grid gap-6 md:grid-cols-3">
        {plans.map((p) => (
          <div
            key={p.name}
            className={`rounded-xl border p-6 ${
              p.highlighted ? "border-primary bg-primary/5" : "bg-card"
            }`}
          >
            <h3 className="font-semibold">{p.name}</h3>
            <p className="mt-3">
              <span className="text-3xl font-bold">{p.price}</span>{" "}
              {p.period && <span className="text-sm text-muted-foreground">{p.period}</span>}
            </p>
            <p className="mt-2 text-sm text-muted-foreground">{p.description}</p>
            <ul className="mt-4 space-y-2">
              {p.features.map((f) => (
                <li key={f} className="flex items-center gap-2 text-sm">
                  <ShieldCheck className="h-4 w-4 text-primary" />
                  {f}
                </li>
              ))}
            </ul>
            <Link
              href="/register"
              className={`mt-6 inline-flex h-10 w-full items-center justify-center rounded-lg px-4 text-sm font-medium transition-colors ${
                p.highlighted
                  ? "bg-primary text-primary-foreground hover:bg-primary/90"
                  : "border bg-card hover:bg-accent"
              }`}
            >
              Get started
            </Link>
          </div>
        ))}
      </div>
    </section>
  )
}

export function LandingCta() {
  return (
    <section className="border-t bg-gradient-to-r from-primary/10 to-primary/5">
      <div className="mx-auto max-w-3xl px-4 py-20 text-center sm:px-6">
        <h2 className="text-3xl font-bold tracking-tight">Ready to move money?</h2>
        <p className="mt-3 text-muted-foreground">
          Create your merchant account and take your first payment in minutes. Free to start —
          no card required.
        </p>
        <Link
          href="/register"
          className="mt-8 inline-flex h-11 items-center gap-2 rounded-lg bg-primary px-6 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
        >
          Create your free account <ArrowRight className="h-4 w-4" />
        </Link>
      </div>
    </section>
  )
}
