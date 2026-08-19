import Link from "next/link"
import { ArrowLeft } from "lucide-react"
import { cn } from "@paymentswitch/shared"

/**
 * Every page opened with a hand-rolled `<h1 className="text-3xl …">` plus a
 * muted paragraph, and the ones with actions each re-derived their own flex row.
 * This is that pattern, once — including the optional back link that the payment
 * detail page needs.
 */
export function PageHeader({
  title,
  description,
  eyebrow,
  actions,
  backHref,
  backLabel = "Back",
  className,
}: {
  title: React.ReactNode
  description?: React.ReactNode
  /** Small label above the title — used for record type on detail pages. */
  eyebrow?: React.ReactNode
  actions?: React.ReactNode
  backHref?: string
  backLabel?: string
  className?: string
}) {
  return (
    <div className={cn("space-y-4", className)}>
      {backHref && (
        <Link
          href={backHref}
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          {backLabel}
        </Link>
      )}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="min-w-0 space-y-1">
          {eyebrow && (
            <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              {eyebrow}
            </p>
          )}
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">{title}</h1>
          {description && (
            <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground">
              {description}
            </p>
          )}
        </div>
        {actions && <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div>}
      </div>
    </div>
  )
}

/** Small uppercase divider used to label a group of cards within a page. */
export function SectionLabel({
  children,
  className,
}: {
  children: React.ReactNode
  className?: string
}) {
  return (
    <p
      className={cn(
        "text-xs font-semibold uppercase tracking-wider text-muted-foreground",
        className,
      )}
    >
      {children}
    </p>
  )
}
