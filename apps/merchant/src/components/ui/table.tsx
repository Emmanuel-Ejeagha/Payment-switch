import { cn } from "@paymentswitch/shared"

/**
 * Table scaffolding shared by every list view.
 *
 * The wrapper owns the horizontal scroll so wide tables scroll inside their card
 * instead of pushing the page sideways on mobile — the previous per-page
 * `overflow-x-auto` sat on some tables and not others.
 */
export function TableWrap({
  className,
  children,
}: {
  className?: string
  children: React.ReactNode
}) {
  return (
    <div className={cn("w-full overflow-x-auto", className)}>
      <table className="w-full min-w-[40rem] border-collapse text-sm">{children}</table>
    </div>
  )
}

export function THead({ children }: { children: React.ReactNode }) {
  return (
    <thead className="bg-muted/40">
      <tr className="border-b">{children}</tr>
    </thead>
  )
}

export function TH({
  children,
  className,
  align = "left",
  ...props
}: React.ThHTMLAttributes<HTMLTableCellElement> & { align?: "left" | "right" }) {
  return (
    <th
      scope="col"
      className={cn(
        "whitespace-nowrap px-5 py-2.5 text-xs font-medium uppercase tracking-wide text-muted-foreground sm:px-6",
        align === "right" && "text-right",
        align === "left" && "text-left",
        className,
      )}
      {...props}
    >
      {children}
    </th>
  )
}

export function TBody({ children }: { children: React.ReactNode }) {
  return <tbody className="divide-y">{children}</tbody>
}

export function TR({
  className,
  children,
  ...props
}: React.HTMLAttributes<HTMLTableRowElement>) {
  return (
    <tr className={cn("transition-colors hover:bg-muted/40", className)} {...props}>
      {children}
    </tr>
  )
}

export function TD({
  children,
  className,
  align = "left",
  ...props
}: React.TdHTMLAttributes<HTMLTableCellElement> & { align?: "left" | "right" }) {
  return (
    <td
      className={cn(
        "px-5 py-3 align-middle sm:px-6",
        align === "right" && "text-right",
        className,
      )}
      {...props}
    >
      {children}
    </td>
  )
}

/** Monospace identifier cell — codes, intent ids, key ids. */
export function IdCell({ children }: { children: React.ReactNode }) {
  return (
    <span className="font-mono text-xs text-muted-foreground">{children}</span>
  )
}
