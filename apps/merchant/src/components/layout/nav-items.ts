import {
  BookOpen,
  CreditCard,
  Home,
  Key,
  Link2,
  Package,
  RefreshCw,
  Settings,
  User,
  Users,
  Webhook,
  type LucideIcon,
} from "lucide-react"

export interface NavItem {
  label: string
  href: string
  icon: LucideIcon
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

/**
 * The same eleven destinations as before, grouped.
 *
 * A flat list of eleven links gave no hint that Plans and Subscriptions belong
 * together while API keys and Webhooks are a different job entirely — so finding
 * anything meant reading every label. Grouping is the whole change; nothing was
 * added or removed.
 */
export const navGroups: NavGroup[] = [
  {
    label: "Overview",
    items: [{ label: "Dashboard", href: "/dashboard", icon: Home }],
  },
  {
    label: "Payments",
    items: [
      { label: "Payments", href: "/payments", icon: CreditCard },
      { label: "Payment links", href: "/payment-links", icon: Link2 },
      { label: "Ledger", href: "/ledger", icon: BookOpen },
    ],
  },
  {
    label: "Billing",
    items: [
      { label: "Customers", href: "/customers", icon: Users },
      { label: "Plans", href: "/plans", icon: Package },
      { label: "Subscriptions", href: "/subscriptions", icon: RefreshCw },
    ],
  },
  {
    label: "Developers",
    items: [
      { label: "API keys", href: "/api-keys", icon: Key },
      { label: "Webhooks", href: "/webhooks", icon: Webhook },
    ],
  },
  {
    label: "Account",
    items: [
      { label: "Profile", href: "/profile", icon: User },
      { label: "Settings", href: "/settings", icon: Settings },
    ],
  },
]

const flat = navGroups.flatMap((group) => group.items)

/**
 * Resolves the current pathname to a nav label for the topbar.
 * Matches the longest href prefix so nested routes such as `/payments/{id}`
 * still resolve to "Payments" rather than falling through to nothing.
 */
export function titleForPath(pathname: string): string {
  const match = flat
    .filter((item) => pathname === item.href || pathname.startsWith(`${item.href}/`))
    .sort((a, b) => b.href.length - a.href.length)[0]
  return match?.label ?? "Dashboard"
}

export function isActivePath(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`)
}
