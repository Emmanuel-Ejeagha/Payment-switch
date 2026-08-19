import Link from "next/link"
import { ArrowRight, Check } from "lucide-react"

const assurances = ["Free to start", "No card required", "Cancel anytime"]

export function LandingCta() {
  return (
    <section className="relative overflow-hidden">
      <div
        className="pointer-events-none absolute inset-0 bg-grid opacity-50"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute -bottom-32 left-1/2 h-[26rem] w-[52rem] -translate-x-1/2 rounded-full bg-primary/15 blur-3xl dark:bg-primary/10"
        aria-hidden="true"
      />

      <div className="relative mx-auto max-w-3xl px-4 py-20 text-center sm:px-6 sm:py-28">
        <h2 className="text-3xl font-semibold tracking-tight sm:text-4xl">
          Ready to move money?
        </h2>
        <p className="mx-auto mt-4 max-w-xl text-muted-foreground">
          Create your merchant account and take your first payment today. You only pay
          when your customers do.
        </p>

        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
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
            Sign in
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
    </section>
  )
}
