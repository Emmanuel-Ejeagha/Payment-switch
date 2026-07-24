import { Store } from "lucide-react"
import type { MerchantDto } from "@paymentswitch/shared"
import { StatusBadge } from "./status-badge"

interface RecentMerchantsProps {
  merchants: MerchantDto[]
}

export function RecentMerchants({ merchants }: RecentMerchantsProps) {
  if (merchants.length === 0) {
    return (
      <div className="rounded-xl border bg-card p-6 text-card-foreground shadow-sm">
        <div className="flex items-center gap-2 mb-4">
          <Store className="h-5 w-5 text-muted-foreground" />
          <h2 className="font-semibold">Recent Merchants</h2>
        </div>
        <p className="text-sm text-muted-foreground">No merchants yet.</p>
      </div>
    )
  }

  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow-sm">
      <div className="flex items-center gap-2 border-b px-6 py-4">
        <Store className="h-5 w-5 text-muted-foreground" />
        <h2 className="font-semibold">Recent Merchants</h2>
      </div>
      <div className="divide-y">
        {merchants.map((m) => (
          <div key={m.id} className="flex items-center justify-between px-6 py-3">
            <div>
              <p className="text-sm font-medium">{m.businessName}</p>
              <p className="text-xs text-muted-foreground">{m.email}</p>
            </div>
            <StatusBadge status={m.status} />
          </div>
        ))}
      </div>
    </div>
  )
}
