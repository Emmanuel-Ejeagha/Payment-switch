"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowRight, CheckCircle2, CreditCard, KeyRound, Rocket, Store } from "lucide-react"
import type { UserDto } from "@paymentswitch/shared"
import { BrandMark } from "@/components/landing/brand-mark"
import { apiErrorCode, apiErrorMessage } from "@/lib/api-error"
import {
  NEUTRAL_RESEND_MESSAGE,
  cooldownSecondsLeft,
  parseRetryAfterSeconds,
} from "@/lib/verification"
import {
  Alert,
  Button,
  Card,
  CardBody,
  Field,
  Input,
  Skeleton,
} from "@/components/ui"

const NEXT_STEPS = [
  {
    icon: CheckCircle2,
    title: "Approval review",
    body: "Your profile starts in Pending. An administrator approves and activates it, usually within a business day.",
  },
  {
    icon: KeyRound,
    title: "Generate a test key",
    body: "Test keys work immediately, so you can integrate against the sandbox while approval is pending.",
  },
  {
    icon: CreditCard,
    title: "Take your first payment",
    body: "Create a payment link and share it — no integration code required to get started.",
  },
]

export default function OnboardingPage() {
  const router = useRouter()
  const [user, setUser] = useState<UserDto | null>(null)
  const [businessName, setBusinessName] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)
  const [showResend, setShowResend] = useState(false)
  const [resending, setResending] = useState(false)
  const [resendMsg, setResendMsg] = useState<string | null>(null)
  const [cooldownUntil, setCooldownUntil] = useState<number | null>(null)
  const [nowMs, setNowMs] = useState(() => Date.now())

  // Ticks the resend-cooldown countdown; stops once it lapses.
  useEffect(() => {
    if (cooldownUntil === null) return
    const timer = window.setInterval(() => {
      setNowMs(Date.now())
      if (Date.now() >= (cooldownUntil ?? 0)) {
        window.clearInterval(timer)
        setCooldownUntil(null)
      }
    }, 1000)
    return () => window.clearInterval(timer)
  }, [cooldownUntil])

  const cooldownLeft = cooldownSecondsLeft(cooldownUntil, nowMs)

  useEffect(() => {
    async function load() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) {
          router.replace("/login")
          return
        }
        const userData: UserDto = await userRes.json()
        setUser(userData)
        setBusinessName(userData.fullName ?? "")

        // Already onboarded? Nothing to do here — send them to the dashboard.
        const merchantRes = await fetch(
          `/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(userData.email)}`
        )
        if (merchantRes.ok) {
          router.replace("/dashboard")
          return
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load your account")
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [router])

  const onboard = async () => {
    if (!user || !businessName.trim()) return
    setSubmitting(true)
    setError(null)
    setShowResend(false)
    try {
      const res = await fetch("/api/proxy/merchant/api/v1/merchants", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          ownerId: user.id,
          businessName: businessName.trim(),
          email: user.email,
        }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        if (
          res.status === 403 &&
          apiErrorCode(body) === "Identity.EmailNotVerified"
        ) {
          // Merchant creation requires a verified email. Point at the fix
          // instead of stranding the user: a fresh link, right here.
          setError(
            "Please verify your email address first — your profile can't be created until then."
          )
          setShowResend(true)
        } else {
          setError(apiErrorMessage(body, "Onboarding failed"))
        }
        return
      }
      setDone(true)
      // The merchant lands in Pending until an admin activates it; the dashboard
      // renders that state, so there is no reason to hold the user here.
      setTimeout(() => router.replace("/dashboard"), 1200)
    } catch {
      setError("Backend unreachable. Please try again later.")
    } finally {
      setSubmitting(false)
    }
  }

  const resendVerification = async () => {
    if (!user?.email || cooldownLeft > 0) return
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
      if (res.status === 429) {
        const body = await res.json()
        const message = apiErrorMessage(body, "Please wait before requesting another email.")
        setResendMsg(message)
        setCooldownUntil(Date.now() + parseRetryAfterSeconds(message) * 1000)
        return
      }
      if (!res.ok) {
        const body = await res.json()
        setResendMsg(apiErrorMessage(body, "Could not resend the email. Try again shortly."))
        return
      }
      setResendMsg(NEUTRAL_RESEND_MESSAGE)
    } catch {
      setResendMsg("Backend unreachable. Please try again later.")
    } finally {
      setResending(false)
    }
  }

  if (loading) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <Skeleton className="h-10 w-56" />
        <Skeleton className="h-64" />
      </div>
    )
  }

  const nameTooShort = businessName.trim().length < 2

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      {/* Soft brand wash behind the header — decorative only, no network request. */}
      <div className="relative overflow-hidden rounded-2xl border bg-card p-6 shadow-subtle sm:p-8">
        <div
          className="pointer-events-none absolute -right-16 -top-16 h-48 w-48 rounded-full bg-primary/10 blur-3xl"
          aria-hidden="true"
        />
        <div className="relative flex flex-wrap items-center gap-4">
          <BrandMark className="h-10 w-10" />
          <div className="min-w-0">
            <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Step 2 of 2
            </p>
            <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">Finish setting up</h1>
            <p className="mt-1 max-w-lg text-sm leading-relaxed text-muted-foreground">
              Your sign-in account exists. One more detail — the business name customers will see on
              receipts and checkout pages.
            </p>
          </div>
        </div>
      </div>

      {error && (
        <Alert variant="error" title="We could not create your profile">
          {error}
        </Alert>
      )}

      {showResend && user && (
        <Card>
          <CardBody className="space-y-3">
            <p className="text-sm text-muted-foreground">
              No link in your inbox? Send a fresh verification email to {user.email} —
              profiles unlock as soon as the address is confirmed. After verifying,
              sign in again so your session picks up the new status.
            </p>
            {resendMsg && (
              <p role="status" className="text-xs text-primary">
                {resendMsg}
              </p>
            )}
            <Button
              type="button"
              onClick={resendVerification}
              disabled={resending || cooldownLeft > 0}
            >
              {resending
                ? "Sending…"
                : cooldownLeft > 0
                  ? `Resend available in ${cooldownLeft}s`
                  : "Resend verification email"}
            </Button>
          </CardBody>
        </Card>
      )}

      {done ? (
        <Card className="border-emerald-500/40 bg-emerald-500/5">
          <CardBody className="flex items-start gap-4">
            <span
              className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-emerald-500/15 text-emerald-600 dark:text-emerald-400"
              aria-hidden="true"
            >
              <Rocket className="h-5 w-5" />
            </span>
            <div role="status">
              <p className="font-medium">Merchant profile created</p>
              <p className="mt-1 text-sm text-muted-foreground">
                Your account is pending activation. Taking you to the dashboard…
              </p>
            </div>
          </CardBody>
        </Card>
      ) : (
        <Card>
          <CardBody className="space-y-5">
            <Field
              label="Business name"
              htmlFor="businessName"
              required
              hint="Shown to customers on checkout pages and receipts. You can ask support to change it later."
            >
              <Input
                id="businessName"
                type="text"
                value={businessName}
                onChange={(e) => setBusinessName(e.target.value)}
                placeholder="Your Business Ltd."
                autoComplete="organization"
                autoFocus
              />
            </Field>

            <Field
              label="Email"
              htmlFor="onboarding-email"
              hint="Taken from your sign-in account and cannot be changed here."
            >
              <Input
                id="onboarding-email"
                type="email"
                value={user?.email ?? ""}
                disabled
                readOnly
              />
            </Field>

            <div className="flex flex-wrap items-center justify-between gap-3 pt-1">
              <p className="text-xs text-muted-foreground">
                <Store className="mr-1.5 inline h-3.5 w-3.5 align-[-2px]" aria-hidden="true" />
                Takes a second — nothing else is required to start.
              </p>
              <Button
                variant="primary"
                size="lg"
                onClick={onboard}
                pending={submitting}
                disabled={nameTooShort}
              >
                {submitting ? "Creating…" : "Create merchant profile"}
                {!submitting && <ArrowRight className="h-4 w-4" aria-hidden="true" />}
              </Button>
            </div>
          </CardBody>
        </Card>
      )}

      <div>
        <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          What happens next
        </p>
        <ul className="mt-3 grid gap-3 sm:grid-cols-3">
          {NEXT_STEPS.map(({ icon: Icon, title, body }) => (
            <li key={title} className="rounded-xl border bg-card p-4 shadow-subtle">
              <span
                className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-primary"
                aria-hidden="true"
              >
                <Icon className="h-4 w-4" />
              </span>
              <p className="mt-3 text-sm font-medium">{title}</p>
              <p className="mt-1 text-xs leading-relaxed text-muted-foreground">{body}</p>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}
