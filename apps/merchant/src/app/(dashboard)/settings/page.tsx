"use client"

import { useState } from "react"
import Link from "next/link"
import { Check, CreditCard, Globe, Lock, Store, Webhook, Zap } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import { NotificationPreferences } from "@/components/notifications/notification-preferences"
import {
  Alert,
  Badge,
  Button,
  Card,
  CardBody,
  CardFooter,
  CardHeader,
  ErrorPanel,
  Field,
  Input,
  PageHeader,
  Skeleton,
  StatusPill,
  Toggle,
} from "@/components/ui"
import { humanize } from "@/lib/format"

const paymentMethods = ["card", "bank_transfer", "wallet", "ussd"]

export default function SettingsPage() {
  const { user, merchant, loading, error } = useMerchant()

  const [webhookUrl, setWebhookUrl] = useState("")
  const [enabledMethods, setEnabledMethods] = useState<string[]>([])
  const [autoCapture, setAutoCapture] = useState(true)
  const [loadedMerchantId, setLoadedMerchantId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [saveMessage, setSaveMessage] = useState<string | null>(null)
  const [saveError, setSaveError] = useState(false)

  // Seeding the form from the fetched merchant during render — rather than in an
  // effect — means the inputs are never briefly blank after the data arrives.
  if (merchant && merchant.id !== loadedMerchantId) {
    setLoadedMerchantId(merchant.id)
    setWebhookUrl(merchant.webhookUrl || "")
    setEnabledMethods(merchant.enabledPaymentMethods || [])
    setAutoCapture(merchant.autoCapture ?? true)
  }

  const toggleMethod = (method: string) => {
    setEnabledMethods((prev) =>
      prev.includes(method) ? prev.filter((m) => m !== method) : [...prev, method],
    )
  }

  const handleSaveConfig = async () => {
    if (!merchant) return
    setSaving(true)
    setSaveMessage(null)
    setSaveError(false)
    try {
      const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${merchant.id}/configuration`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId: merchant.id,
          webhookUrl: webhookUrl || null,
          paymentMethods: enabledMethods,
          autoCapture,
        }),
      })
      if (res.ok) {
        setSaveMessage("Configuration updated")
        setSaveError(false)
      } else {
        const body = await res.json()
        setSaveMessage(body.detail || body.title || `Failed (${res.status})`)
        setSaveError(true)
      }
    } catch {
      setSaveMessage("Failed to save configuration")
      setSaveError(true)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="space-y-8">
        <PageHeader title="Settings" description="Merchant profile and integration settings." />
        <div className="grid gap-6 lg:grid-cols-3">
          <Skeleton className="h-96 lg:col-span-2" />
          <Skeleton className="h-64" />
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-8">
        <PageHeader title="Settings" description="Merchant profile and integration settings." />
        <ErrorPanel title="Failed to load settings" message={error} />
      </div>
    )
  }

  const locked = merchant?.status !== "Active"

  return (
    <div className="space-y-8">
      <PageHeader
        title="Settings"
        description="How PaymentSwitch behaves for your business — where events are delivered, which methods customers see, and whether authorised payments capture on their own."
      />

      {locked && (
        <Alert variant={merchant?.status === "Rejected" ? "error" : "warning"} title="Configuration is read-only">
          Your merchant account is{" "}
          <strong className="font-medium">{merchant?.status ?? "not active"}</strong>. Settings unlock
          once an administrator activates it.
          {merchant?.status === "Rejected" && merchant.rejectionReason && (
            <>
              {" "}
              <span className="block pt-1">
                Reason given: <em className="not-italic font-medium">{merchant.rejectionReason}</em>
              </span>
            </>
          )}
        </Alert>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader
              title="Integration"
              description="Where we send events and how payments are captured"
              icon={Globe}
              action={locked ? <Badge tone="warning" dot>Locked</Badge> : undefined}
            />
            <CardBody className="space-y-6">
              <Field
                label="Webhook URL"
                htmlFor="webhook"
                hint={
                  <>
                    We POST a signed JSON body here for every payment event. Leave blank to disable
                    delivery. Recent attempts are listed under{" "}
                    <Link
                      href="/webhooks"
                      className="font-medium text-primary underline underline-offset-2"
                    >
                      Webhooks
                    </Link>
                    .
                  </>
                }
              >
                <Input
                  id="webhook"
                  type="url"
                  value={webhookUrl}
                  onChange={(e) => setWebhookUrl(e.target.value)}
                  placeholder="https://example.com/webhooks/paymentswitch"
                  disabled={locked}
                  autoComplete="off"
                  spellCheck={false}
                />
              </Field>

              <div className="space-y-2">
                <p className="text-sm font-medium text-foreground">Payment methods</p>
                <p className="text-xs text-muted-foreground">
                  Only the methods selected here are offered at checkout.
                </p>
                <div className="flex flex-wrap gap-2 pt-1">
                  {paymentMethods.map((method) => {
                    const on = enabledMethods.includes(method)
                    return (
                      <button
                        key={method}
                        type="button"
                        onClick={() => toggleMethod(method)}
                        disabled={locked}
                        aria-pressed={on}
                        className={`inline-flex h-9 items-center gap-1.5 rounded-lg border px-3 text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50 ${
                          on
                            ? "border-primary bg-primary/10 text-primary"
                            : "border-input text-muted-foreground hover:bg-accent hover:text-foreground"
                        }`}
                      >
                        {on ? (
                          <Check className="h-3.5 w-3.5" aria-hidden="true" />
                        ) : (
                          <span className="h-3.5 w-3.5" aria-hidden="true" />
                        )}
                        {humanize(method)}
                      </button>
                    )
                  })}
                </div>
                {enabledMethods.length === 0 && (
                  <p className="pt-1 text-xs text-amber-600 dark:text-amber-400">
                    No methods selected — customers will have nothing to pay with.
                  </p>
                )}
              </div>

              <div className="flex items-start justify-between gap-4 rounded-lg border bg-muted/30 p-4">
                <div className="min-w-0">
                  <p className="flex items-center gap-2 text-sm font-medium">
                    <Zap className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                    Auto-capture payments
                  </p>
                  <p className="mt-1 text-xs leading-relaxed text-muted-foreground">
                    Capture authorised payments as soon as they succeed. Turn this off to authorise
                    only and capture later — useful when you ship before you charge.
                  </p>
                </div>
                <Toggle
                  checked={autoCapture}
                  onChange={setAutoCapture}
                  disabled={locked}
                  label="Auto-capture payments"
                />
              </div>

              {saveMessage && (
                <Alert variant={saveError ? "error" : "success"}>{saveMessage}</Alert>
              )}
            </CardBody>
            <CardFooter>
              <p className="text-xs text-muted-foreground">
                Changes apply to new payments straight away.
              </p>
              <Button
                variant="primary"
                icon={Check}
                onClick={handleSaveConfig}
                pending={saving}
                disabled={locked}
              >
                {saving ? "Saving…" : "Save configuration"}
              </Button>
            </CardFooter>
          </Card>

          <NotificationPreferences />
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader title="Business" description="Read-only account details" icon={Store} />
            <CardBody className="space-y-3 text-sm">
              {merchant ? (
                <dl className="space-y-3">
                  <div>
                    <dt className="text-xs text-muted-foreground">Business name</dt>
                    <dd className="mt-0.5 font-medium">{merchant.businessName}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-muted-foreground">Email</dt>
                    <dd className="mt-0.5 break-all font-medium">{merchant.email}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-muted-foreground">Account owner</dt>
                    <dd className="mt-0.5 font-medium">{user?.fullName || "—"}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-muted-foreground">Status</dt>
                    <dd className="mt-1">
                      <StatusPill status={merchant.status} />
                    </dd>
                  </div>
                </dl>
              ) : (
                <p className="text-sm text-muted-foreground">
                  No merchant profile yet.{" "}
                  <Link
                    href="/onboarding"
                    className="font-medium text-primary underline underline-offset-2"
                  >
                    Finish setup
                  </Link>
                  .
                </p>
              )}
            </CardBody>
          </Card>

          <Card>
            <CardHeader title="Related" icon={Lock} />
            <CardBody className="space-y-2 text-sm">
              <Link
                href="/api-keys"
                className="flex items-center gap-2.5 rounded-lg border px-3 py-2.5 transition-colors hover:bg-accent"
              >
                <CreditCard className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                <span className="min-w-0">
                  <span className="block font-medium">API keys</span>
                  <span className="block text-xs text-muted-foreground">
                    Generate and revoke secret keys
                  </span>
                </span>
              </Link>
              <Link
                href="/webhooks"
                className="flex items-center gap-2.5 rounded-lg border px-3 py-2.5 transition-colors hover:bg-accent"
              >
                <Webhook className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                <span className="min-w-0">
                  <span className="block font-medium">Webhook deliveries</span>
                  <span className="block text-xs text-muted-foreground">
                    Inspect payloads and replay failures
                  </span>
                </span>
              </Link>
            </CardBody>
          </Card>
        </div>
      </div>
    </div>
  )
}
