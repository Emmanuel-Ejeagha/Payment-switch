import type { LucideIcon } from "lucide-react"

interface StatsCardProps {
  title: string
  value: string | number
  description?: string
  icon: LucideIcon
}

export function StatsCard({ title, value, description, icon: Icon }: StatsCardProps) {
  return (
    <div className="rounded-xl border bg-card p-6 text-card-foreground shadow-sm">
      <div className="flex items-center gap-4">
        <div className="rounded-lg bg-primary/10 p-3">
          <Icon className="h-5 w-5 text-primary" />
        </div>
        <div className="flex-1">
          <p className="text-sm text-muted-foreground">{title}</p>
          <p className="text-2xl font-semibold">{value}</p>
          {description && (
            <p className="text-xs text-muted-foreground">{description}</p>
          )}
        </div>
      </div>
    </div>
  )
}
