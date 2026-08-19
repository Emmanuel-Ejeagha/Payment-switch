"use client"

import Link from "next/link"
import { Building2, CalendarDays, Check, Mail, Minus, Settings, Shield, Store, User } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import {
  Alert,
  Badge,
  Card,
  CardBody,
  CardHeader,
  ErrorPanel,
  PageHeader,
  Skeleton,
  StatusPill,
} from "@/components/ui"
import { formatDate, shortId } from "@/lib/format"

function DetailRow({
  label,
  children,
}: {
  label: string
  children: React.ReactNode
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-2 py-2.5">
      <dt className="text-sm text-muted-foreground">{label}</dt>
      <dd className="min-w-0 text-sm font-medium">{children}</dd>
    </div>
  )
}

/** Two letters from the account name — a lightweight stand-in for an avatar upload. */
function initialsOf(name: string | null | undefined) {
  const parts = (name ?? "").trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return "?"
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export default function ProfilePage() {
  const { user, merchant, loading, error } = useMerchant()

  if (loading) {
    return (
      <div className="space-y-8">
        <PageHeader title="Profile" description="Your account and merchant information." />
        <Skeleton className="h-32" />
        <div className="grid gap-6 md:grid-cols-2">
          <Skeleton className="h-64" />
          <Skeleton className="h-64" />
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-8">
        <PageHeader title="Profile" description="Your account and merchant information." />
        <ErrorPanel title="Failed to load profile" message={error} />
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="Profile"
        description="Who you are on PaymentSwitch, and the business your payments settle to."
      />

      {merchant?.status === "Rejected" && (
        <Alert variant="error" title="Your application was declined">
          <p>
            {merchant.rejectionReason ||
              "Our team could not approve your business at this time."}
          </p>
        </Alert>
      )}

      {merchant?.status === "Pending" && (
        <Alert variant="warning" title="Application under review">
          <p>Your business is being reviewed by our team. We usually decide within one business day.</p>
        </Alert>
      )}

      {merchant?.status === "Approved" && (
        <Alert variant="info" title="Application approved">
          <p>Your business is approved and ready to go live. An administrator activates it shortly.</p>
        </Alert>
      )}

      <Card className="overflow-hidden">
        <div className="flex flex-wrap items-center gap-5 p-6">
          <span
            className="flex h-16 w-16 shrink-0 items-center justify-center rounded-2xl bg-primary/10 text-xl font-semibold tracking-tight text-primary"
            aria-hidden="true"
          >
            {initialsOf(user?.fullName)}
          </span>
          <div className="min-w-0 flex-1 space-y-1">
            <h2 className="truncate text-xl font-semibold tracking-tight">
              {user?.fullName || "Your account"}
            </h2>
            <p className="flex items-center gap-1.5 truncate text-sm text-muted-foreground">
              <Mail className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              {user?.email ?? "—"}
            </p>
            <div className="flex flex-wrap items-center gap-2 pt-1">
              {merchant && <StatusPill status={merchant.status} />}
              <Badge tone={user?.isActive ? "success" : "neutral"} dot>
                {user?.isActive ? "Account active" : "Account inactive"}
              </Badge>
            </div>
          </div>
          <Link
            href="/security"
            className="inline-flex h-9 shrink-0 items-center gap-2 rounded-lg border px-3.5 text-sm font-medium transition-colors hover:bg-accent"
          >
            <Shield className="h-4 w-4" aria-hidden="true" />
            Security
          </Link>
          <Link
            href="/settings"
            className="inline-flex h-9 shrink-0 items-center gap-2 rounded-lg border px-3.5 text-sm font-medium transition-colors hover:bg-accent"
          >
            <Settings className="h-4 w-4" aria-hidden="true" />
            Settings
          </Link>
        </div>
      </Card>

      <div className="grid gap-6 md:grid-cols-2">
        {user && (
          <Card>
            <CardHeader title="Account" description="Your sign-in identity" icon={User} />
            <CardBody className="py-2">
              <dl className="divide-y">
                <DetailRow label="Name">{user.fullName || "—"}</DetailRow>
                <DetailRow label="Email">
                  <span className="break-all">{user.email}</span>
                </DetailRow>
                <DetailRow label="Roles">
                  {user.roles.length > 0 ? (
                    <span className="flex flex-wrap justify-end gap-1.5">
                      {user.roles.map((role) => (
                        <Badge key={role} tone="brand">
                          {role}
                        </Badge>
                      ))}
                    </span>
                  ) : (
                    "—"
                  )}
                </DetailRow>
                <DetailRow label="Status">
                  {user.isActive ? (
                    <span className="flex items-center gap-1.5 text-emerald-600 dark:text-emerald-400">
                      <Check className="h-4 w-4" aria-hidden="true" />
                      Active
                    </span>
                  ) : (
                    <span className="flex items-center gap-1.5 text-muted-foreground">
                      <Minus className="h-4 w-4" aria-hidden="true" />
                      Inactive
                    </span>
                  )}
                </DetailRow>
              </dl>
            </CardBody>
          </Card>
        )}

        {merchant ? (
          <Card>
            <CardHeader title="Merchant" description="The business behind your payments" icon={Store} />
            <CardBody className="py-2">
              <dl className="divide-y">
                <DetailRow label="Business name">{merchant.businessName}</DetailRow>
                <DetailRow label="Email">
                  <span className="break-all">{merchant.email}</span>
                </DetailRow>
                <DetailRow label="Status">
                  <StatusPill status={merchant.status} />
                </DetailRow>
                <DetailRow label="Merchant ID">
                  <span className="font-mono text-xs text-muted-foreground">
                    {shortId(merchant.id, 10, 6)}
                  </span>
                </DetailRow>
                <DetailRow label="Member since">
                  <span className="flex items-center gap-1.5">
                    <CalendarDays className="h-3.5 w-3.5 text-muted-foreground" aria-hidden="true" />
                    {formatDate(merchant.createdAt)}
                  </span>
                </DetailRow>
                <DetailRow label="Contact person">{merchant.contactPerson || "—"}</DetailRow>
                <DetailRow label="Phone">{merchant.contactPhone || "—"}</DetailRow>
                <DetailRow label="Address" >{merchant.contactAddress || "—"}</DetailRow>
                <DetailRow label="Settlement account">
                  {merchant.settlementBankName
                    ? `${merchant.settlementBankName} · ${merchant.settlementBankAccountNumber ?? "—"}`
                    : "—"}
                </DetailRow>
                <DetailRow label="Settlement schedule">
                  {merchant.settlementCurrency
                    ? `${merchant.settlementCurrency} · ${merchant.settlementSchedule ?? "default"}`
                    : "—"}
                </DetailRow>
              </dl>
            </CardBody>
          </Card>
        ) : (
          <Card>
            <CardHeader title="Merchant" icon={Store} />
            <CardBody className="flex flex-col items-start gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                <Building2 className="h-5 w-5" aria-hidden="true" />
              </span>
              <div>
                <p className="text-sm font-medium">No merchant profile yet</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Create one to start accepting payments. It only takes your business name.
                </p>
              </div>
              <Link
                href="/onboarding"
                className="inline-flex h-9 items-center rounded-lg bg-primary px-3.5 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90"
              >
                Finish setup
              </Link>
            </CardBody>
          </Card>
        )}
      </div>

      <p className="flex items-start gap-2 text-xs text-muted-foreground">
        <Shield className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden="true" />
        Account details come from your sign-in provider. To change your name or email, update them
        there and they will refresh here on your next sign-in.
      </p>
    </div>
  )
}
