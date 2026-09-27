import { Zap, CreditCard, Banknote } from "lucide-react"

const steps = [
  {
    icon: Zap,
    title: "Create a payment",
    description:
      "Spin up a payment intent from the API, or a shareable payment link from the dashboard, in a single call.",
  },
  {
    icon: CreditCard,
    title: "Your customer pays",
    description:
      "They check out on a hosted page with tokenised cards, 3-D Secure, and instant confirmation.",
  },
  {
    icon: Banknote,
    title: "Money settles",
    description:
      "Funds move through the double-entry ledger and settle into your payout at the end of the day.",
  },
]

export function LandingHowItWorks() {
  return (
    <section id="how-it-works" className="scroll-mt-24 border-y bg-muted/30">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6 sm:py-24">
        <div className="mx-auto max-w-2xl text-center">
          <p className="text-xs font-semibold uppercase tracking-wider text-primary">
            How it works
          </p>
          <h2 className="mt-3 text-3xl font-semibold tracking-tight sm:text-4xl">
            First payment to payout in three steps
          </h2>
          <p className="mt-4 text-muted-foreground">
            No gateway contracts to negotiate and no reconciliation spreadsheets to
            maintain.
          </p>
        </div>

        <ol className="relative mt-14 grid gap-6 md:grid-cols-3">
          {/* Connector rail, desktop only. Sits behind the cards. */}
          <span
            className="pointer-events-none absolute left-0 right-0 top-[3.25rem] hidden h-px bg-gradient-to-r from-transparent via-border to-transparent md:block"
            aria-hidden="true"
          />

          {steps.map((s, i) => (
            <li
              key={s.title}
              className="relative rounded-2xl border bg-card p-6 shadow-subtle"
            >
              <div className="flex items-center gap-3">
                <span className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-brand-gradient text-sm font-semibold text-white shadow-subtle">
                  {i + 1}
                </span>
                <s.icon className="h-5 w-5 text-primary" aria-hidden="true" />
              </div>
              <h3 className="mt-4 font-semibold tracking-tight">{s.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                {s.description}
              </p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  )
}
