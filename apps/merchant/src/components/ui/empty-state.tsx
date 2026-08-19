import { cn } from "@paymentswitch/shared"

/**
 * Empty state for list views.
 *
 * The old version was a grey icon over grey text, which read as a loading
 * failure rather than "nothing here yet". This one names the thing that is
 * missing, explains what creating one does, and — when the caller passes an
 * action — offers the way out. The icon sits in a soft tinted plate so the block
 * has a visual anchor.
 */
export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  className,
}: {
  icon: React.ComponentType<{ className?: string }>
  title: string
  description?: React.ReactNode
  action?: React.ReactNode
  className?: string
}) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center px-6 py-14 text-center",
        className,
      )}
    >
      <span
        className="relative flex h-12 w-12 items-center justify-center rounded-xl bg-muted text-muted-foreground ring-1 ring-inset ring-border"
        aria-hidden="true"
      >
        <Icon className="h-5 w-5" />
      </span>
      <p className="mt-4 text-sm font-medium text-foreground">{title}</p>
      {description && (
        <p className="mt-1 max-w-sm text-sm leading-relaxed text-muted-foreground">
          {description}
        </p>
      )}
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}
