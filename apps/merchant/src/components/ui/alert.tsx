import { AlertCircle, AlertTriangle, CheckCircle2, Info } from "lucide-react"
import { cn } from "@paymentswitch/shared"

type Variant = "error" | "success" | "warning" | "info"

const config: Record<
  Variant,
  { wrap: string; icon: React.ComponentType<{ className?: string }>; iconClass: string; role: "alert" | "status" }
> = {
  error: {
    wrap: "border-destructive/30 bg-destructive/10 text-destructive",
    icon: AlertCircle,
    iconClass: "text-destructive",
    role: "alert",
  },
  success: {
    wrap: "border-emerald-500/30 bg-emerald-500/10 text-emerald-800 dark:text-emerald-300",
    icon: CheckCircle2,
    iconClass: "text-emerald-600 dark:text-emerald-400",
    role: "status",
  },
  warning: {
    wrap: "border-amber-500/30 bg-amber-500/10 text-amber-900 dark:text-amber-200",
    icon: AlertTriangle,
    iconClass: "text-amber-600 dark:text-amber-400",
    role: "status",
  },
  info: {
    wrap: "border-primary/30 bg-primary/10 text-foreground",
    icon: Info,
    iconClass: "text-primary",
    role: "status",
  },
}

/**
 * Inline feedback banner.
 *
 * Errors get `role="alert"` so a screen reader announces them the moment a
 * failed mutation renders one; confirmations use the politer `role="status"`.
 * The previous banners were plain divs, so a blind user saw a form appear to do
 * nothing at all when a request failed.
 */
export function Alert({
  variant = "error",
  title,
  children,
  action,
  className,
}: {
  variant?: Variant
  title?: React.ReactNode
  children?: React.ReactNode
  action?: React.ReactNode
  className?: string
}) {
  const { wrap, icon: Icon, iconClass, role } = config[variant]
  return (
    <div
      role={role}
      className={cn(
        "flex items-start gap-3 rounded-lg border p-3.5 text-sm",
        wrap,
        className,
      )}
    >
      <Icon className={cn("mt-0.5 h-4 w-4 shrink-0", iconClass)} aria-hidden="true" />
      <div className="min-w-0 flex-1">
        {title && <p className="font-medium">{title}</p>}
        {children && (
          <div className={cn("leading-relaxed", title && "mt-0.5 opacity-90")}>{children}</div>
        )}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  )
}

/** Full-panel error used where a page cannot render at all. */
export function ErrorPanel({
  title,
  message,
  action,
}: {
  title: string
  message?: string | null
  action?: React.ReactNode
}) {
  return (
    <div
      role="alert"
      className="flex flex-col items-center rounded-xl border border-destructive/30 bg-destructive/5 px-6 py-12 text-center"
    >
      <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-destructive/10 text-destructive">
        <AlertCircle className="h-5 w-5" aria-hidden="true" />
      </span>
      <p className="mt-4 font-medium text-foreground">{title}</p>
      {message && (
        <p className="mt-1 max-w-md text-sm leading-relaxed text-muted-foreground">{message}</p>
      )}
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}
