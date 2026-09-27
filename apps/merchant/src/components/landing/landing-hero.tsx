import Link from "next/link"
import { ArrowRight, Check, ShieldCheck } from "lucide-react"
import { DashboardPreview } from "@/components/landing/dashboard-preview"

const assurances = ["Free to start", "No card required", "Live in minutes"]

const trustedBy = [
  "Lumen Retail",
  "Northwind Studio",
  "Tessellate",
  "Harbour Foods",
  "Kestrel Labs",
  "Meridian Health",
]

export function LandingHero() {
  return (
    <section className="relative overflow-hidden">
      <div
        className="pointer-events-none absolute inset-x-0 top-0 h-[36rem] bg-grid mask-fade-b opacity-60"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute -top-40 left-1/2 h-[32rem] w-[64rem] -translate-x-1/2 rounded-full bg-primary/15 blur-3xl dark:bg-primary/10"
        aria-hidden="true"
      />

      <div className="relative mx-auto max-w-6xl px-4 pb-16 pt-16 sm:px-6 sm:pt-24">
        <div className="mx-auto max-w-3xl text-center">
          <span className="animate-fade-up inline-flex items-center gap-2 rounded-full border bg-card/80 px-3 py-1 text-xs font-medium text-muted-foreground shadow-subtle backdrop-blur">
            <span className="relative flex h-1.5 w-1.5" aria-hidden="true">
              <span className="absolute inline-flex h-full w-full animate-pulse-ring rounded-full bg-success" />
              <span className="relative inline-flex h-1.5 w-1.5 rounded-full bg-success" />
            </span>
            Real-time switching on a distributed, event-driven core
          </span>

          <h1 className="mt-6 text-4xl font-semibold leading-[1.08] tracking-tight sm:text-5xl md:text-6xl">
            Accept payments anywhere,
            <br className="hidden sm:block" />{" "}
            <span className="text-gradient-brand">built like a bank</span>
          </h1>

          <p
            className="animate-fade-up mx-auto mt-5 max-w-2xl text-base leading-relaxed text-muted-foreground sm:text-lg"
            style={{ animationDelay: "120ms" }}
          >
            Hosted checkout, payment links, subscriptions, signed webhooks, and a
            double-entry ledger that settles daily — one platform, six event-driven
            services, no infrastructure to run.
          </p>

          <div
            className="animate-fade-up mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row"
            style={{ animationDelay: "180ms" }}
          >
            <Link
              href="/register"
              className="inline-flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-primary px-6 text-sm font-medium text-primary-foreground shadow-glow transition-colors hover:bg-primary/90 sm:w-auto"
            >
              Create your free account
              <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </Link>
            <Link
              href="/login"
              className="inline-flex h-11 w-full items-center justify-center rounded-lg border bg-card px-6 text-sm font-medium shadow-subtle transition-colors hover:bg-accent sm:w-auto"
            >
              Sign in to your dashboard
            </Link>
          </div>

          <ul className="mt-6 flex flex-wrap items-center justify-center gap-x-5 gap-y-2">
            {assurances.map((a) => (
              <li key={a} className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <Check className="h-3.5 w-3.5 text-success" aria-hidden="true" />
                {a}
              </li>
            ))}
          </ul>
        </div>

        <div className="relative mt-14 sm:mt-16">
          <div className="mx-auto max-w-4xl">
            <DashboardPreview />
          </div>

          <div className="pointer-events-none absolute -left-2 top-12 hidden animate-float rounded-xl border bg-card/90 p-3 shadow-card backdrop-blur lg:block">
            <p className="text-[11px] text-muted-foreground">Authorised</p>
            <p className="tabular text-sm font-semibold">142 ms</p>
          </div>

          <div
            className="pointer-events-none absolute -right-2 bottom-16 hidden animate-float rounded-xl border bg-card/90 p-3 shadow-card backdrop-blur lg:block"
            style={{ animationDelay: "1.5s" }}
          >
            <p className="flex items-center gap-1.5 text-[11px] text-muted-foreground">
              <ShieldCheck className="h-3 w-3 text-success" aria-hidden="true" />
              Settled today
            </p>
            <p className="tabular text-sm font-semibold">$61,904.12</p>
          </div>
        </div>

        <div className="mt-16">
          <p className="text-center text-xs font-medium uppercase tracking-wider text-muted-foreground">
            Powering payments for teams like
          </p>
          <div className="mask-fade-x mt-5 overflow-hidden">
            <ul className="flex flex-wrap items-center justify-center gap-x-8 gap-y-3">
              {trustedBy.map((name) => (
                <li
                  key={name}
                  className="text-sm font-semibold tracking-tight text-muted-foreground/70"
                >
                  {name}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  )
}
