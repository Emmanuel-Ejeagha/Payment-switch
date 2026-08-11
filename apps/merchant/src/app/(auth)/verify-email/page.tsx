"use client"

import { useSearchParams } from "next/navigation"
import Link from "next/link"
import { Suspense, useEffect, useState } from "react"
import { AlertCircle, CheckCircle2, Loader2, MailCheck } from "lucide-react"
import { AuthShell, inputClass, labelClass, submitClass } from "@/components/auth/auth-shell"
import { authArtwork } from "@/lib/images"

type VerifyState =
  | { status: "verifying" }
  | { status: "success" }
  | { status: "error"; message: string; canResend: boolean }

function VerifyEmailContent() {
  const searchParams = useSearchParams()
  const email = searchParams.get("email") ?? ""
  const token = searchParams.get("token") ?? ""

  const [state, setState] = useState<VerifyState>(
    email && token
      ? { status: "verifying" }
      : { status: "error", message: "This verification link is incomplete. Please request a new one.", canResend: true }
  )
  const [resendEmail, setResendEmail] = useState(email)
  const [resending, setResending] = useState(false)
  const [resendMsg, setResendMsg] = useState<string | null>(null)

  useEffect(() => {
    if (!token || !email) return

    let cancelled = false
    const verify = async () => {
      try {
        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/verify-email`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, token }),
          }
        )
        if (cancelled) return

        if (res.ok) {
          setState({ status: "success" })
          return
        }

        const body = await res.json()
        const message =
          body.message ?? body.detail ?? "We could not verify this link. Please try again."
        setState({ status: "error", message, canResend: true })
      } catch {
        if (!cancelled) {
          setState({
            status: "error",
            message: "Backend unreachable. Please try again later.",
            canResend: true,
          })
        }
      }
    }

    verify()
    return () => {
      cancelled = true
    }
  }, [email, token])

  const resend = async () => {
    if (!resendEmail) return
    setResending(true)
    setResendMsg(null)
    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/resend-verification`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email: resendEmail }),
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

  return (
    <AuthShell
      image={authArtwork.register}
      panelHeading="Secure your account"
      panelBody="Email verification keeps your account and merchant data safe from unauthorised signups."
      heading={
        state.status === "success"
          ? "Email verified"
          : state.status === "verifying"
            ? "Verifying your email"
            : "Verification failed"
      }
      subheading={
        state.status === "success"
          ? "Your account is active and ready to go."
          : state.status === "verifying"
            ? "Hold on a moment…"
            : "We couldn't confirm this link."
      }
      footer={
        <>
          Want to sign in?{" "}
          <Link href="/login" className="font-medium text-primary hover:underline">
            Go to login
          </Link>
        </>
      }
    >
      <div className="space-y-5">
        {state.status === "verifying" && (
          <div className="flex flex-col items-center gap-3 rounded-lg border border-muted bg-muted/40 p-6 text-center">
            <Loader2 className="h-7 w-7 animate-spin text-primary" aria-hidden="true" />
            <p className="text-sm text-muted-foreground">Checking your verification link…</p>
          </div>
        )}

        {state.status === "success" && (
          <div className="space-y-5">
            <div className="flex items-start gap-2.5 rounded-lg border border-green-600/30 bg-green-600/10 p-4 text-sm text-green-700 dark:text-green-400">
              <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
              <span>
                Your email <strong>{email}</strong> has been confirmed. You can now sign in and
                manage your merchant account.
              </span>
            </div>
            <Link href="/login" className={submitClass}>
              Continue to sign in
            </Link>
          </div>
        )}

        {state.status === "error" && (
          <div className="space-y-5">
            <div
              role="alert"
              className="flex items-start gap-2.5 rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
              <span>{state.message}</span>
            </div>

            {state.canResend && (
              <div className="rounded-lg border border-muted bg-muted/40 p-4 space-y-3">
                <label htmlFor="resend-email" className={labelClass}>
                  Email address
                </label>
                <input
                  id="resend-email"
                  type="email"
                  value={resendEmail}
                  onChange={(e) => setResendEmail(e.target.value)}
                  className={inputClass}
                  placeholder="name@example.com"
                />
                {resendMsg && (
                  <p role="status" className="text-xs text-primary">
                    {resendMsg}
                  </p>
                )}
                <button
                  type="button"
                  onClick={resend}
                  disabled={resending || !resendEmail}
                  className={submitClass}
                >
                  {resending ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                      Sending…
                    </>
                  ) : (
                    "Resend verification email"
                  )}
                </button>
                <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <MailCheck className="h-3.5 w-3.5" aria-hidden="true" />
                  A new link will expire in 24 hours.
                </p>
              </div>
            )}
          </div>
        )}
      </div>
    </AuthShell>
  )
}

export default function VerifyEmailPage() {
  return (
    <Suspense fallback={null}>
      <VerifyEmailContent />
    </Suspense>
  )
}
