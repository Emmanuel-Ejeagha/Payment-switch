import { Loader2 } from "lucide-react"
import { cn } from "@paymentswitch/shared"

type Variant = "primary" | "secondary" | "ghost" | "danger" | "dangerGhost"
type Size = "sm" | "md" | "lg"

const variantClasses: Record<Variant, string> = {
  primary:
    "bg-primary text-primary-foreground shadow-subtle hover:bg-primary/90 active:bg-primary/95",
  secondary: "border bg-card text-foreground shadow-subtle hover:bg-accent",
  ghost: "text-muted-foreground hover:bg-accent hover:text-accent-foreground",
  danger:
    "bg-destructive text-destructive-foreground shadow-subtle hover:bg-destructive/90",
  dangerGhost:
    "border border-destructive/40 text-destructive hover:bg-destructive/10",
}

const sizeClasses: Record<Size, string> = {
  sm: "h-8 gap-1.5 rounded-lg px-2.5 text-xs",
  md: "h-9 gap-2 rounded-lg px-3.5 text-sm",
  lg: "h-11 gap-2 rounded-lg px-5 text-sm",
}

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  /** Swaps the leading icon for a spinner and disables the button. */
  pending?: boolean
  icon?: React.ComponentType<{ className?: string }>
  /** Renders the icon only, with `children` used as the accessible name. */
  iconOnly?: boolean
}

export function Button({
  variant = "secondary",
  size = "md",
  pending = false,
  icon: Icon,
  iconOnly = false,
  disabled,
  className,
  children,
  // Buttons default to `type="submit"` inside a form, which silently submits the
  // surrounding form on any stray click. Every caller here is an action button
  // unless it says otherwise, so default to the inert type.
  type = "button",
  ...props
}: ButtonProps) {
  const showSpinner = pending
  return (
    <button
      type={type}
      disabled={disabled || pending}
      aria-busy={pending || undefined}
      className={cn(
        "inline-flex shrink-0 items-center justify-center font-medium transition-colors",
        "disabled:pointer-events-none disabled:opacity-50",
        sizeClasses[size],
        iconOnly && (size === "sm" ? "w-8 px-0" : size === "md" ? "w-9 px-0" : "w-11 px-0"),
        variantClasses[variant],
        className,
      )}
      {...props}
    >
      {showSpinner ? (
        <Loader2 className="h-4 w-4 shrink-0 animate-spin" aria-hidden="true" />
      ) : (
        Icon && <Icon className={cn("shrink-0", size === "sm" ? "h-3.5 w-3.5" : "h-4 w-4")} aria-hidden="true" />
      )}
      {iconOnly ? <span className="sr-only">{children}</span> : children}
    </button>
  )
}
