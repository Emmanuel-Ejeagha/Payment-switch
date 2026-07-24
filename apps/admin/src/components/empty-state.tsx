import { Inbox } from "lucide-react"
import type { LucideIcon } from "lucide-react"

interface EmptyStateProps {
  icon?: LucideIcon
  title?: string
  description?: string
}

export function EmptyState({
  icon: Icon = Inbox,
  title = "No data",
  description = "Nothing to show yet",
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center rounded-xl border border-dashed py-16 text-center">
      <Icon className="mb-3 h-8 w-8 text-muted-foreground" />
      <p className="font-medium">{title}</p>
      <p className="text-sm text-muted-foreground">{description}</p>
    </div>
  )
}
