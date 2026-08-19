import Image from "next/image"
import { Quote } from "lucide-react"
import { testimonials } from "@/lib/images"

export function LandingTestimonials() {
  return (
    <section id="customers" className="scroll-mt-24">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6 sm:py-24">
        <div className="mx-auto max-w-2xl text-center">
          <p className="text-xs font-semibold uppercase tracking-wider text-primary">
            Customers
          </p>
          <h2 className="mt-3 text-3xl font-semibold tracking-tight sm:text-4xl">
            Teams that stopped building billing
          </h2>
          <p className="mt-4 text-muted-foreground">
            Finance and engineering both get what they need out of the same platform.
          </p>
        </div>

        <div className="mt-14 grid gap-5 md:grid-cols-3">
          {testimonials.map((t) => (
            <figure
              key={t.name}
              className="flex flex-col rounded-2xl border bg-card p-6 shadow-subtle"
            >
              <Quote className="h-5 w-5 shrink-0 text-primary/40" aria-hidden="true" />
              <blockquote className="mt-4 flex-1 text-sm leading-relaxed text-muted-foreground">
                {t.quote}
              </blockquote>
              <figcaption className="mt-6 flex items-center gap-3 border-t pt-5">
                {/* The muted ring doubles as the placeholder if the CDN is slow
                    or unreachable, so the row never collapses. */}
                <span className="relative h-10 w-10 shrink-0 overflow-hidden rounded-full bg-muted ring-1 ring-border">
                  <Image
                    src={t.avatar}
                    alt={`Portrait of ${t.name}`}
                    width={40}
                    height={40}
                    unoptimized
                    className="h-full w-full object-cover"
                  />
                </span>
                <span className="min-w-0">
                  <span className="block truncate text-sm font-medium text-foreground">
                    {t.name}
                  </span>
                  <span className="block truncate text-xs text-muted-foreground">
                    {t.role}
                  </span>
                </span>
              </figcaption>
            </figure>
          ))}
        </div>
      </div>
    </section>
  )
}
