"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { useState } from "react"
import { ArrowRight, Loader2, AlertCircle, MailCheck } from "lucide-react"
import { AuthShell, inputClass, labelClass, submitClass } from "@/components/auth/auth-shell"
import { authArtwork } from "@/lib/images"

const forgotSchema = z.object({
  email: z.string().email("Invalid email address"),
})

type ForgotForm = z.infer<typeof forgotSchema>

export default function ForgotPasswordPage() {
  const router = useRouter()
  const [error, setError] = useState<string | null>(null)
  const [sent, setSent] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotForm>({
    resolver: zodResolver(forgotSchema),
  })

  const onSubmit = async (data: ForgotForm) => {
    setError(null)
    setSent(null)

    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/forgot-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email: data.email }),
        }
      )

      if (!res.ok) {
        const body = await res.json()
        setError(body.message ?? body.detail ?? "Could not send the reset link. Try again shortly.")
        return
      }

      setSent(data.email)
    } catch {
      setError("Backend unreachable. Please try again later.")
    }
  }

  if (sent) {
    return (
      <AuthShell
        image={authArtwork.login}
        panelHeading="Back to business in minutes"
        panelBody="Reset your password and get straight back to managing payments, balances, and settlements."
        heading="Check your inbox"
        subheading={`We sent a password reset link to ${sent}`}
        footer={
          <>
            Remembered your password?{" "}
            <Link href="/login" className="font-medium text-primary hover:underline">
              Back to login
            </Link>
          </>
        }
      >
        <div className="space-y-5">
          <div className="rounded-lg border border-muted bg-muted/40 p-6 text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-primary/10">
              <MailCheck className="h-7 w-7 text-primary" aria-hidden="true" />
            </div>
            <p className="text-sm leading-relaxed text-muted-foreground">
              The link expires in 1 hour. If it doesn&apos;t arrive, check your spam folder or try
              again.
            </p>
          </div>

          <button
            type="button"
            onClick={() => router.push("/login")}
            className="w-full rounded-lg border border-muted-foreground/20 p-3 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted"
          >
            Continue to sign in
          </button>
        </div>
      </AuthShell>
    )
  }

  return (
    <AuthShell
      image={authArtwork.login}
      panelHeading="Back to business in minutes"
      panelBody="Reset your password and get straight back to managing payments, balances, and settlements."
      heading="Forgot your password?"
      subheading="Enter your email and we'll send you a reset link"
      footer={
        <>
          Remembered your password?{" "}
          <Link href="/login" className="font-medium text-primary hover:underline">
            Back to login
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">
        <div className="space-y-2">
          <label htmlFor="email" className={labelClass}>
            Email
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            placeholder="name@example.com"
            aria-invalid={!!errors.email}
            aria-describedby={errors.email ? "email-error" : undefined}
            className={inputClass}
            {...register("email")}
          />
          {errors.email && (
            <p id="email-error" className="text-xs text-destructive">
              {errors.email.message}
            </p>
          )}
        </div>

        {error && (
          <div
            role="alert"
            className="flex items-start gap-2.5 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
          >
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
            <span>{error}</span>
          </div>
        )}

        <button type="submit" disabled={isSubmitting} className={submitClass}>
          {isSubmitting ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
              Sending reset link...
            </>
          ) : (
            <>
              Send reset link
              <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </>
          )}
        </button>
      </form>
    </AuthShell>
  )
}
