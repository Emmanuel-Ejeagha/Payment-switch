"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { Key, KeyRound, Plus, ShieldAlert, Trash2, X } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { ApiKeyDto } from "@paymentswitch/shared"
import {
  Alert,
  Badge,
  Button,
  Card,
  CardBody,
  CardHeader,
  CopyButton,
  EmptyState,
  Field,
  PageHeader,
  Select,
  Skeleton,
  useConfirm,
} from "@/components/ui"
import { formatDate } from "@/lib/format"

export default function ApiKeysPage() {
  const { merchantId, user, loading: merchantLoading, error: merchantError } = useMerchant()
  const { confirm, dialog } = useConfirm()
  const [apiKeys, setApiKeys] = useState<ApiKeyDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [environment, setEnvironment] = useState("test")
  const [generating, setGenerating] = useState(false)
  const [revoking, setRevoking] = useState<string | null>(null)
  const [resending, setResending] = useState(false)
  const [resendMsg, setResendMsg] = useState<string | null>(null)
  // Held in memory only, never persisted or logged — the server will not return it again.
  const [newKey, setNewKey] = useState<string | null>(null)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const emailUnverified = !!user && !user.emailConfirmed

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    const mid = merchantId
    async function loadKeys() {
      try {
        const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${mid}/apikeys`)
        if (!res.ok) { setError("Failed to load API keys"); return }
        setApiKeys(await res.json())
      } catch {
        setError("Failed to load API keys")
      } finally {
        setDataReady(true)
      }
    }
    loadKeys()
  }, [merchantLoading, merchantId])

  const handleGenerateKey = async () => {
    if (!merchantId) return
    setGenerating(true)
    setNewKey(null)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${merchantId}/apikeys`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ environment }),
      })
      if (res.ok) {
        const data = await res.json()
        setNewKey(data.plainTextKey)
        setApiKeys((prev) => [
          { keyId: data.keyId, environment: data.environment, createdAt: data.createdAt, revokedAt: null },
          ...prev,
        ])
      } else {
        const body = await res.json()
        setError(body.detail || "Failed to generate key")
      }
    } catch {
      setError("Failed to generate key")
    } finally {
      setGenerating(false)
    }
  }

  const resendVerification = async () => {
    if (!user?.email) return
    setResending(true)
    setResendMsg(null)
    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/resend-verification`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email: user.email }),
        }
      )
      if (!res.ok) {
        const body = await res.json()
        setResendMsg(body.message ?? body.detail ?? "Could not resend the email. Try again shortly.")
        return
      }
      setResendMsg("A new verification email is on its way. Check your inbox.")
    } catch {
      setResendMsg("Backend unreachable. Please try again later.")
    } finally {
      setResending(false)
    }
  }

  const handleRevokeKey = async (key: ApiKeyDto) => {
    if (!merchantId) return
    const isLive = key.environment === "live"
    const confirmed = await confirm({
      title: "Revoke this key",
      message: (
        <>
          Any request still signing with this{" "}
          <strong className="font-medium text-foreground">{isLive ? "live" : "test"}</strong> key
          starts failing immediately.
          {isLive && " Check nothing in production is using it before you continue."} This cannot be
          undone.
        </>
      ),
      confirmLabel: "Revoke key",
      cancelLabel: "Keep key",
      destructive: true,
    })
    if (!confirmed) return

    setRevoking(key.keyId)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/merchant/api/v1/merchants/${merchantId}/apikeys/${key.keyId}`, {
        method: "DELETE",
      })
      if (res.ok) {
        setApiKeys((prev) =>
          prev.map((k) => (k.keyId === key.keyId ? { ...k, revokedAt: new Date().toISOString() } : k)),
        )
      } else {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Failed to revoke key")
      }
    } catch {
      setError("Failed to revoke key")
    } finally {
      setRevoking(null)
    }
  }

  const activeKeys = apiKeys.filter((k) => !k.revokedAt)
  const revokedKeys = apiKeys.filter((k) => k.revokedAt)

  if (loading) {
    return (
      <div className="space-y-8">
        <PageHeader title="API keys" description="Secret keys that authenticate your server-side requests." />
        <Skeleton className="h-48" />
        <Skeleton className="h-64" />
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="API keys"
        description={
          <>
            Secret keys authenticate your server-side calls to the Payments API — send one as{" "}
            <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
              Authorization: Bearer …
            </code>
            . Treat them like passwords: keep them on your server, never in browser or mobile code.
          </>
        }
      />

      {(error || merchantError) && (
        <Alert variant="error" title="Something went wrong">
          {error || merchantError}
        </Alert>
      )}

      {emailUnverified && (
        <Alert variant="warning" title="Verify your email to generate keys">
          <div className="space-y-2">
            <p>
              Your email has not been verified yet, so API key generation is disabled. Confirm the
              address in the email we sent you to unlock keys.
            </p>
            {resendMsg && <p className="text-xs">{resendMsg}</p>}
            <button
              type="button"
              onClick={resendVerification}
              disabled={resending}
              className="rounded-md border border-amber-600/40 bg-amber-600/10 px-3 py-1.5 text-xs font-medium text-amber-700 transition-colors hover:bg-amber-600/20 disabled:opacity-60 dark:text-amber-400"
            >
              {resending ? "Sending…" : "Resend verification email"}
            </button>
          </div>
        </Alert>
      )}

      {newKey && (
        <Card className="border-emerald-500/40 bg-emerald-500/5">
          <CardBody className="space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="text-sm font-semibold text-emerald-700 dark:text-emerald-300">
                  Copy this key now
                </p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  This is the only time it is shown. We store a hash, not the key — if you lose it
                  you will have to generate a new one.
                </p>
              </div>
              <button
                type="button"
                onClick={() => setNewKey(null)}
                className="shrink-0 rounded-md p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <X className="h-4 w-4" aria-hidden="true" />
                <span className="sr-only">Dismiss</span>
              </button>
            </div>
            <div className="flex items-center gap-2">
              <code className="min-w-0 flex-1 truncate rounded-lg border bg-background px-3 py-2 font-mono text-xs">
                {newKey}
              </code>
              <CopyButton value={newKey} label="Copy key" />
            </div>
          </CardBody>
        </Card>
      )}

      <Card>
        <CardHeader
          title="Generate a new key"
          description="Test keys hit the sandbox. Live keys move real money."
          icon={KeyRound}
        />
        <CardBody>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
            <Field label="Environment" htmlFor="key-environment" className="sm:max-w-xs sm:flex-1">
              <Select
                id="key-environment"
                value={environment}
                onChange={(e) => setEnvironment(e.target.value)}
              >
                <option value="test">Test</option>
                <option value="live">Live</option>
              </Select>
            </Field>
            <Button
              variant="primary"
              icon={Plus}
              onClick={handleGenerateKey}
              pending={generating}
              disabled={!merchantId || emailUnverified}
              className="sm:mb-0"
            >
              {generating ? "Generating…" : "Generate key"}
            </Button>
          </div>
          {environment === "live" && (
            <Alert variant="warning" className="mt-4" title="Live key">
              A live key charges real cards. Store it in your server&apos;s secret manager, not in a
              repository or a client bundle.
            </Alert>
          )}
        </CardBody>
      </Card>

      <Card className="overflow-hidden">
        <CardHeader
          title="Your keys"
          description={`${activeKeys.length} active${revokedKeys.length ? `, ${revokedKeys.length} revoked` : ""}`}
          icon={Key}
        />
        {apiKeys.length === 0 ? (
          <EmptyState
            icon={Key}
            title="No API keys yet"
            description="Generate a test key to start calling the Payments API from your server. You can revoke it at any time."
          />
        ) : (
          <div className="divide-y">
            {activeKeys.length === 0 ? (
              <div className="flex items-start gap-3 px-5 py-5 sm:px-6">
                <ShieldAlert
                  className="mt-0.5 h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400"
                  aria-hidden="true"
                />
                <p className="text-sm text-muted-foreground">
                  Every key has been revoked, so API requests are currently rejected. Generate a new
                  one above to restore access.
                </p>
              </div>
            ) : (
              activeKeys.map((k) => (
                <div
                  key={k.keyId}
                  className="flex flex-wrap items-center justify-between gap-3 px-5 py-4 sm:px-6"
                >
                  <div className="min-w-0 space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone={k.environment === "live" ? "brand" : "neutral"}>
                        {k.environment === "live" ? "Live" : "Test"}
                      </Badge>
                      <Badge tone="success" dot>
                        Active
                      </Badge>
                    </div>
                    <p className="truncate font-mono text-xs text-muted-foreground">{k.keyId}</p>
                    <p className="text-xs text-muted-foreground">
                      Created {formatDate(k.createdAt)}
                    </p>
                  </div>
                  <Button
                    size="sm"
                    variant="dangerGhost"
                    icon={Trash2}
                    pending={revoking === k.keyId}
                    disabled={revoking !== null}
                    onClick={() => handleRevokeKey(k)}
                  >
                    Revoke
                  </Button>
                </div>
              ))
            )}

            {revokedKeys.length > 0 && (
              <details className="group px-5 py-3 sm:px-6">
                <summary className="cursor-pointer list-none text-xs font-medium text-muted-foreground transition-colors hover:text-foreground">
                  {revokedKeys.length} revoked key{revokedKeys.length !== 1 ? "s" : ""}
                  <span className="ml-1 text-muted-foreground group-open:hidden">— show</span>
                  <span className="ml-1 hidden text-muted-foreground group-open:inline">— hide</span>
                </summary>
                <div className="mt-3 space-y-2">
                  {revokedKeys.map((k) => (
                    <div
                      key={k.keyId}
                      className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-dashed px-4 py-3"
                    >
                      <div className="min-w-0 space-y-0.5">
                        <div className="flex items-center gap-2">
                          <Badge tone="neutral">
                            {k.environment === "live" ? "Live" : "Test"}
                          </Badge>
                          <span className="text-xs text-muted-foreground">Revoked</span>
                        </div>
                        <p className="truncate font-mono text-xs text-muted-foreground">{k.keyId}</p>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        {k.revokedAt ? formatDate(k.revokedAt) : "—"}
                      </p>
                    </div>
                  ))}
                </div>
              </details>
            )}
          </div>
        )}
      </Card>

      <p className="text-xs text-muted-foreground">
        Rotating a key? Generate the replacement first, deploy it, then revoke the old one from this
        page. Webhook signing is configured separately under{" "}
        <Link href="/settings" className="font-medium text-primary underline underline-offset-2">
          Settings
        </Link>
        .
      </p>

      {dialog}
    </div>
  )
}
