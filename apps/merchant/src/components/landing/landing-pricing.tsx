import Link from "next/link"
import { Check } from "lucide-react"

const plans = [
  {
    name: "Starter",
    price: "$0",
    period: "to start",
    description: "Go live in minutes and pay only when you get paid.",
    features: ["1.5% per transaction", "Hosted checkout", "Payment links", "Merchant dashboard"],
    cta: "Start for free",
    highlighted: false,
  },
  {
    name: "Growth",
    price: "$29",
    period: "/month",
    description: "For businesses with recurring revenue to run.",
    features: [
      "Everything in Starter",
      "Subscriptions & invoices",
      "Webhook analytics",
      "Priority routing",
    ],
    cta: "Choose Growth",
    highlighted: true,
  },
  {
    name: "Scale",
    price: "Custom",
    period: "",
    description: "For high-volume platforms and marketplaces.",
    features: [
      "Custom settlement cycles",
      "Multi-currency payouts",
      "Dedicated support",
      "Volume pricing",
    ],
    cta: "Talk to us",
    highlighted: false,
  },
]

export function LandingPricing() {
  return (
    <section id="pricing" className="scroll-mt-24 border-y bg-muted/30">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6 sm:py-24">
        <div className="mx-auto max-w-2xl text-center">
          <p className="text-xs font-semibold uppercase tracking-wider text-primary">
            Pricing
          </p>
          <h2 className="mt-3 text-3xl font-semibold tracking-tight sm:text-4xl">
            Simple and transparent
          </h2>
          <p className="mt-4 text-muted-foreground">
            Start free, upgrade when you grow. No setup fees, no monthly minimums.
          </p>
        </div>

        <div className="mt-14 grid items-start gap-6 md:grid-cols-3">
          {plans.map((p) => (
            <div
              key={p.name}
              className={`relative flex flex-col rounded-2xl p-6 ${
                p.highlighted
                  ? "border-2 border-primary bg-card shadow-lift md:-mt-3 md:pb-8 md:pt-8"
                  : "border bg-card shadow-subtle"
              }`}
            >
              {p.highlighted && (
                <span className="absolute -top-3 left-6 rounded-full bg-brand-gradient px-3 py-1 text-[11px] font-semibold uppercase tracking-wide text-white shadow-subtle">
                  Most popular
                </span>
              )}

              <h3 className="font-semibold tracking-tight">{p.name}</h3>
              <p className="mt-3 flex items-baseline gap-1.5">
                <span className="tabular text-4xl font-semibold tracking-tight">
                  {p.price}
                </span>
                {p.period && (
                  <span className="text-sm text-muted-foreground">{p.period}</span>
                )}
              </p>
              <p className="mt-2 text-sm text-muted-foreground">{p.description}</p>

              <ul className="mt-6 flex-1 space-y-2.5">
                {p.features.map((f) => (
                  <li key={f} className="flex items-start gap-2.5 text-sm">
                    <Check
                      className="mt-0.5 h-4 w-4 shrink-0 text-success"
                      aria-hidden="true"
                    />
                    <span className="text-muted-foreground">{f}</span>
                  </li>
                ))}
              </ul>

              <Link
                href="/register"
                className={`mt-7 inline-flex h-11 w-full items-center justify-center rounded-lg px-4 text-sm font-medium transition-colors ${
                  p.highlighted
                    ? "bg-primary text-primary-foreground shadow-glow hover:bg-primary/90"
                    : "border bg-background hover:bg-accent"
                }`}
              >
                {p.cta}
              </Link>
            </div>
          ))}
        </div>

        <p className="mt-8 text-center text-xs text-muted-foreground">
          All plans include signed webhooks, the double-entry ledger, and daily settlement.
        </p>
      </div>
    </section>
  )
}
