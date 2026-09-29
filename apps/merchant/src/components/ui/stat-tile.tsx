import { cn } from "@paymentswitch/shared"
import type { Tone } from "@/components/ui/badge"

const iconTones: Record<Tone, string> = {
  neutral: "bg-muted text-muted-foreground",
  success: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  warning: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
  danger: "bg-red-500/10 text-red-600 dark:text-red-400",
  info: "bg-sky-500/10 text-sky-600 dark:text-sky-400",
  brand: "bg-primary/10 text-primary",
}

/**
 * Metric tile for balances and counts.
 *
 * The figure is the point, so it leads at display size with tabular figures —
 * columns of tiles line up on the decimal instead of drifting. The currency code
 * is set smaller and muted so it reads as a unit rather than part of the number.
 */
export function StatTile({
  label,
  value,
  unit,
  hint,
  icon: Icon,
  tone = "brand",
  className,
}: {
  label: string
  value: string
  /** Currency code or similar, rendered de-emphasised after the figure. */
  unit?: string
  hint?: React.ReactNode
  icon: React.ComponentType<{ className?: string }>
  tone?: Tone
  className?: string
}) {
  return (
    <div
      className={cn(
        "group relative overflow-hidden rounded-xl border bg-card p-5 shadow-subtle transition-shadow hover:shadow-card",
        className,
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-medium text-muted-foreground">{label}</p>
        <span
          className={cn(
            "flex h-8 w-8 shrink-0 items-center justify-center rounded-lg",
            iconTones[tone],
          )}
          aria-hidden="true"
        >
          <Icon className="h-4 w-4" />
        </span>
      </div>
      <p className="tabular mt-3 text-2xl font-semibold tracking-tight">
        {value}
        {unit && (
          <span className="ml-1.5 text-sm font-medium text-muted-foreground">{unit}</span>
        )}
      </p>
      {hint && <p className="mt-1 text-xs text-muted-foreground">{hint}</p>}
    </div>
  )
}
