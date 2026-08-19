import { ChevronDown } from "lucide-react"
import { cn } from "@paymentswitch/shared"

/**
 * Shared control surface. Four slightly different versions of this string were
 * scattered across the dashboard — differing heights, differing focus
 * treatments, and two that dropped the disabled styling entirely.
 */
export const controlClass =
  "w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground " +
  "placeholder:text-muted-foreground/70 transition-colors " +
  "hover:border-ring/40 " +
  "focus:border-ring focus:outline-none focus:ring-2 focus:ring-ring/25 " +
  "disabled:cursor-not-allowed disabled:bg-muted disabled:text-muted-foreground"

const heightClass = "h-10"

interface FieldProps {
  label: React.ReactNode
  htmlFor?: string
  required?: boolean
  /** Helper text under the control. */
  hint?: React.ReactNode
  error?: string | null
  className?: string
  children: React.ReactNode
}

export function Field({
  label,
  htmlFor,
  required,
  hint,
  error,
  className,
  children,
}: FieldProps) {
  return (
    <div className={cn("space-y-1.5", className)}>
      <label htmlFor={htmlFor} className="block text-sm font-medium text-foreground">
        {label}
        {required && (
          <span className="ml-0.5 text-destructive" aria-hidden="true">
            *
          </span>
        )}
      </label>
      {children}
      {error ? (
        <p className="text-xs text-destructive">{error}</p>
      ) : (
        hint && <p className="text-xs text-muted-foreground">{hint}</p>
      )}
    </div>
  )
}

export function Input({
  className,
  ...props
}: React.InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn(controlClass, heightClass, className)} {...props} />
}

/**
 * Native selects render a platform-coloured arrow that clashes with dark mode,
 * so the control is neutralised with `appearance-none` and given its own.
 */
export function Select({
  className,
  children,
  ...props
}: React.SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <div className="relative">
      <select
        className={cn(controlClass, heightClass, "appearance-none pr-9", className)}
        {...props}
      >
        {children}
      </select>
      <ChevronDown
        className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden="true"
      />
    </div>
  )
}

/** Amount input with the currency code pinned inside the control. */
export function AmountInput({
  currency,
  className,
  ...props
}: React.InputHTMLAttributes<HTMLInputElement> & { currency?: string }) {
  return (
    <div className="relative">
      <input
        type="number"
        min="0"
        step="0.01"
        inputMode="decimal"
        className={cn(controlClass, heightClass, "tabular", currency && "pr-14", className)}
        {...props}
      />
      {currency && (
        <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs font-medium text-muted-foreground">
          {currency}
        </span>
      )}
    </div>
  )
}

export function Toggle({
  checked,
  onChange,
  disabled,
  label,
}: {
  checked: boolean
  onChange: (next: boolean) => void
  disabled?: boolean
  /** Accessible name — the visible label lives outside the control. */
  label: string
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors",
        "disabled:cursor-not-allowed disabled:opacity-50",
        checked ? "bg-primary" : "bg-input",
      )}
    >
      <span
        className={cn(
          "inline-block h-5 w-5 transform rounded-full bg-background shadow transition-transform",
          checked ? "translate-x-[22px]" : "translate-x-0.5",
        )}
      />
    </button>
  )
}
