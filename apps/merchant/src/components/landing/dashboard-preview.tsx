import { ArrowUpRight, CreditCard, Link2, RefreshCw } from "lucide-react"

/**
 * A static, self-contained rendering of the merchant dashboard used as the hero
 * visual. Everything is markup and inline SVG — no data fetching, no remote
 * assets, and every figure is a hard-coded string so server and client output
 * match exactly (locale-formatted numbers would risk a hydration mismatch).
 */

const LINE = "M0 74 L34 66 L68 70 L102 52 L136 58 L170 38 L204 44 L238 26 L272 30 L306 12 L320 8"
const AREA = `${LINE} L320 96 L0 96 Z`

const metrics = [
  { label: "Succeeded", value: "1,284", icon: CreditCard },
  { label: "Links paid", value: "312", icon: Link2 },
  { label: "Renewals", value: "96", icon: RefreshCw },
]

const rows = [
  { id: "pi_3Qx8Fa2h", customer: "Lumen Retail", amount: "$1,240.00", state: "Succeeded" },
  { id: "pi_3Qx7Zk9d", customer: "Northwind Studio", amount: "$89.00", state: "Succeeded" },
  { id: "pi_3Qx7Rb4m", customer: "Tessellate", amount: "$460.50", state: "Pending" },
]

export function DashboardPreview() {
  return (
    <div className="overflow-hidden rounded-2xl border bg-card shadow-lift">
      <div className="flex items-center gap-2 border-b bg-muted/40 px-4 py-3">
        <span className="flex gap-1.5" aria-hidden="true">
          <span className="h-2.5 w-2.5 rounded-full bg-destructive/50" />
          <span className="h-2.5 w-2.5 rounded-full bg-amber-400/60" />
          <span className="h-2.5 w-2.5 rounded-full bg-success/50" />
        </span>
        <span className="mx-auto hidden rounded-md border bg-background px-3 py-1 text-[11px] text-muted-foreground sm:block">
          app.paymentswitch.io/dashboard
        </span>
      </div>

      <div className="space-y-5 p-5 sm:p-6">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Total volume
            </p>
            <p className="tabular mt-1 text-3xl font-semibold tracking-tight sm:text-4xl">
              $284,912.40
            </p>
          </div>
          <span className="inline-flex items-center gap-1 rounded-full bg-success/10 px-2.5 py-1 text-xs font-medium text-success">
            <ArrowUpRight className="h-3.5 w-3.5" />
            18.2% vs last month
          </span>
        </div>

        <div className="relative h-24 w-full">
          <svg
            viewBox="0 0 320 96"
            preserveAspectRatio="none"
            className="h-full w-full"
            role="img"
            aria-label="Payment volume trending upward over the last thirty days"
          >
            <defs>
              <linearGradient id="ps-area" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="hsl(var(--primary))" stopOpacity="0.28" />
                <stop offset="100%" stopColor="hsl(var(--primary))" stopOpacity="0" />
              </linearGradient>
            </defs>
            <path d={AREA} fill="url(#ps-area)" />
            <path
              d={LINE}
              fill="none"
              stroke="hsl(var(--primary))"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            />
          </svg>
        </div>

        <div className="grid grid-cols-3 gap-3">
          {metrics.map((m) => (
            <div key={m.label} className="rounded-xl border bg-background p-3">
              <m.icon className="h-4 w-4 text-primary" aria-hidden="true" />
              <p className="tabular mt-2 text-lg font-semibold leading-none">{m.value}</p>
              <p className="mt-1 truncate text-[11px] text-muted-foreground">{m.label}</p>
            </div>
          ))}
        </div>

        <div className="overflow-hidden rounded-xl border">
          {rows.map((r, i) => (
            <div
              key={r.id}
              className={`flex items-center gap-3 px-3 py-2.5 text-xs ${
                i > 0 ? "border-t" : ""
              }`}
            >
              <span className="hidden font-mono text-muted-foreground sm:inline">{r.id}</span>
              <span className="truncate font-medium">{r.customer}</span>
              <span className="tabular ml-auto font-medium">{r.amount}</span>
              <span
                className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${
                  r.state === "Succeeded"
                    ? "bg-success/10 text-success"
                    : "bg-amber-500/10 text-amber-600 dark:text-amber-400"
                }`}
              >
                {r.state}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
