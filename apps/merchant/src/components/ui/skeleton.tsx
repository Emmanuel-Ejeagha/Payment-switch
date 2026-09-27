import { cn } from "@paymentswitch/shared"

/** Shimmer block. `animate-pulse` is disabled by the reduced-motion rule in globals.css. */
export function Skeleton({ className }: { className?: string }) {
  return <div className={cn("animate-pulse rounded-lg bg-muted", className)} />
}

/**
 * Loading placeholder shaped like the table it replaces, so the layout does not
 * jump when data lands. The old pages showed a single 16rem grey block for every
 * list, which shifted every row into place on arrival.
 */
export function TableSkeleton({ rows = 5, columns = 5 }: { rows?: number; columns?: number }) {
  return (
    <div className="divide-y">
      {Array.from({ length: rows }).map((_, r) => (
        <div key={r} className="flex items-center gap-6 px-5 py-3.5 sm:px-6">
          {Array.from({ length: columns }).map((_, c) => (
            <Skeleton
              key={c}
              className={cn(
                "h-4",
                c === 0 ? "w-28" : c === columns - 1 ? "ml-auto w-16" : "w-20",
              )}
            />
          ))}
        </div>
      ))}
    </div>
  )
}

export function StatSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <Skeleton key={i} className="h-[6.5rem]" />
      ))}
    </div>
  )
}
