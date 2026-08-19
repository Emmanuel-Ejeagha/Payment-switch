import { cn } from "@paymentswitch/shared"

/**
 * Semantic tones. Colours come from Tailwind's fixed palette rather than the
 * theme tokens because these need to read as *meaning* (money settled, delivery
 * failed) independently of the brand primary, and they must hold up in both
 * light and dark mode.
 */
export type Tone = "neutral" | "success" | "warning" | "danger" | "info" | "brand"

const toneClasses: Record<Tone, string> = {
  neutral: "bg-muted text-muted-foreground ring-border",
  success:
    "bg-emerald-500/10 text-emerald-700 ring-emerald-600/20 dark:text-emerald-400 dark:ring-emerald-400/25",
  warning:
    "bg-amber-500/10 text-amber-700 ring-amber-600/20 dark:text-amber-400 dark:ring-amber-400/25",
  danger:
    "bg-red-500/10 text-red-700 ring-red-600/20 dark:text-red-400 dark:ring-red-400/25",
  info: "bg-sky-500/10 text-sky-700 ring-sky-600/20 dark:text-sky-400 dark:ring-sky-400/25",
  brand: "bg-primary/10 text-primary ring-primary/20",
}

const dotClasses: Record<Tone, string> = {
  neutral: "bg-muted-foreground/60",
  success: "bg-emerald-500",
  warning: "bg-amber-500",
  danger: "bg-red-500",
  info: "bg-sky-500",
  brand: "bg-primary",
}

interface BadgeProps {
  children: React.ReactNode
  tone?: Tone
  /** Show a leading status dot. Useful in tables where the label alone reads flat. */
  dot?: boolean
  className?: string
}

export function Badge({ children, tone = "neutral", dot = false, className }: BadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-0.5",
        "text-xs font-medium ring-1 ring-inset",
        toneClasses[tone],
        className,
      )}
    >
      {dot && <span className={cn("h-1.5 w-1.5 rounded-full", dotClasses[tone])} aria-hidden="true" />}
      {children}
    </span>
  )
}

/**
 * One place that decides what every status string in the product *means*.
 *
 * Before this, five separate colour maps disagreed: a payment's `Captured` was
 * brand-blue on one page and untinted on another, and anything unmapped fell
 * through to grey — so `Failed` and `Voided` rendered as calmly as `Settled`.
 * Keys are lower-cased on lookup so backend casing changes don't silently
 * downgrade a status to neutral.
 */
const statusTones: Record<string, Tone> = {
  // Payment intent lifecycle
  succeeded: "success",
  settled: "success",
  captured: "success",
  authorized: "info",
  pending: "warning",
  processing: "warning",
  requiresaction: "warning",
  failed: "danger",
  declined: "danger",
  voided: "neutral",
  canceled: "neutral",
  cancelled: "neutral",
  refunded: "neutral",
  partiallyrefunded: "neutral",

  // Merchant / plan / link / key
  active: "success",
  approved: "info",
  rejected: "danger",
  inactive: "neutral",
  archived: "neutral",
  revoked: "neutral",
  suspended: "danger",

  // Subscriptions
  trialing: "info",
  pastdue: "danger",
  unpaid: "danger",
  incomplete: "warning",

  // Webhook delivery
  delivered: "success",
  retrying: "warning",

  // Ledger entries
  credit: "success",
  debit: "danger",
  reserve: "warning",
  release: "info",
}

export function toneForStatus(status: string): Tone {
  return statusTones[status.toLowerCase().replace(/[\s_-]/g, "")] ?? "neutral"
}

/** A badge that picks its own tone from the status string. */
export function StatusPill({
  status,
  dot = true,
  className,
}: {
  status: string
  dot?: boolean
  className?: string
}) {
  return (
    <Badge tone={toneForStatus(status)} dot={dot} className={className}>
      {status}
    </Badge>
  )
}
