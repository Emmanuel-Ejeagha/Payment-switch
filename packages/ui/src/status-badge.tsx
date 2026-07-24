import { cn } from "@paymentswitch/shared"

const colors: Record<string, string> = {
  Active: "bg-emerald-500/10 text-emerald-600",
  Pending: "bg-amber-500/10 text-amber-600",
  Suspended: "bg-red-500/10 text-red-600",
}

export function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "rounded-full px-2.5 py-0.5 text-xs font-medium",
        colors[status] || "bg-muted text-muted-foreground",
      )}
    >
      {status}
    </span>
  )
}
