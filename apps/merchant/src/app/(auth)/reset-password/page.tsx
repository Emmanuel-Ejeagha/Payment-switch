"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import Link from "next/link"
import { useSearchParams } from "next/navigation"
import { Suspense, useState } from "react"
import { ArrowRight, CheckCircle2, Eye, EyeOff, Loader2, AlertCircle } from "lucide-react"
import { AuthShell, inputClass, labelClass, submitClass } from "@/components/auth/auth-shell"
import { authArtwork } from "@/lib/images"

const resetSchema = z
  .object({
    password: z
      .string()
      .min(10, "Password must be at least 10 characters")
      .regex(/[A-Za-z]/, "Password must contain at least one letter")
      .regex(/[0-9]/, "Password must contain at least one digit"),
    confirmPassword: z.string(),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  })

type ResetForm = z.infer<typeof resetSchema>

function ResetPasswordContent() {
  const searchParams = useSearchParams()
  const email = searchParams.get("email") ?? ""
  const token = searchParams.get("token") ?? ""

  const [error, setError] = useState<string | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [done, setDone] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetForm>({
    resolver: zodResolver(resetSchema),
  })

  const onSubmit = async (data: ResetForm) => {
    setError(null)

    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/reset-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email, token, newPassword: data.password }),
        }
      )

      if (!res.ok) {
        const body = await res.json()
        setError(body.message ?? body.detail ?? "Could not reset your password. Please try again.")
        return
      }

      setDone(true)
    } catch {
      setError("Backend unreachable. Please try again later.")
    }
  }

  if (done) {
    return (
      <AuthShell
        image={authArtwork.login}
        panelHeading="You're all set"
        panelBody="Your password has been reset. All existing sessions have been signed out for your safety."
        heading="Password updated"
        subheading="Sign in with your new password"
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
          <div className="flex items-start gap-2.5 rounded-lg border border-green-600/30 bg-green-600/10 p-4 text-sm text-green-700 dark:text-green-400">
            <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
            <span>Your password was changed successfully.</span>
          </div>
          <Link href="/login" className={submitClass}>
            Continue to sign in
          </Link>
        </div>
      </AuthShell>
    )
  }

  if (!email || !token) {
    return (
      <AuthShell
        image={authArtwork.login}
        panelHeading="Back to business in minutes"
        panelBody="Reset your password and get straight back to managing payments, balances, and settlements."
        heading="Reset link invalid"
        subheading="This reset link is incomplete."
        footer={
          <>
            Need a new link?{" "}
            <Link href="/forgot-password" className="font-medium text-primary hover:underline">
              Request another one
            </Link>
          </>
        }
      >
        <div
          role="alert"
          className="flex items-start gap-2.5 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
        >
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
          <span>This password reset link is incomplete. Please request a new one.</span>
        </div>
      </AuthShell>
    )
  }

  return (
    <AuthShell
      image={authArtwork.login}
      panelHeading="Back to business in minutes"
      panelBody="Reset your password and get straight back to managing payments, balances, and settlements."
      heading="Set a new password"
      subheading="Choose a new password for your account"
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
          <label htmlFor="password" className={labelClass}>
            New password
          </label>
          <div className="relative">
            <input
              id="password"
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              placeholder="Min. 8 characters"
              aria-invalid={!!errors.password}
              aria-describedby={errors.password ? "password-error" : undefined}
              className={`${inputClass} pr-11`}
              {...register("password")}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? "Hide password" : "Show password"}
              className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
            >
              {showPassword ? (
                <EyeOff className="h-4 w-4" aria-hidden="true" />
              ) : (
                <Eye className="h-4 w-4" aria-hidden="true" />
              )}
            </button>
          </div>
          {errors.password && (
            <p id="password-error" className="text-xs text-destructive">
              {errors.password.message}
            </p>
          )}
        </div>

        <div className="space-y-2">
          <label htmlFor="confirmPassword" className={labelClass}>
            Confirm new password
          </label>
          <div className="relative">
            <input
              id="confirmPassword"
              type={showConfirm ? "text" : "password"}
              autoComplete="new-password"
              placeholder="Repeat your new password"
              aria-invalid={!!errors.confirmPassword}
              aria-describedby={errors.confirmPassword ? "confirmPassword-error" : undefined}
              className={`${inputClass} pr-11`}
              {...register("confirmPassword")}
            />
            <button
              type="button"
              onClick={() => setShowConfirm(!showConfirm)}
              aria-label={showConfirm ? "Hide password" : "Show password"}
              className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
            >
              {showConfirm ? (
                <EyeOff className="h-4 w-4" aria-hidden="true" />
              ) : (
                <Eye className="h-4 w-4" aria-hidden="true" />
              )}
            </button>
          </div>
          {errors.confirmPassword && (
            <p id="confirmPassword-error" className="text-xs text-destructive">
              {errors.confirmPassword.message}
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
              Updating password...
            </>
          ) : (
            <>
              Reset password
              <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </>
          )}
        </button>
      </form>
    </AuthShell>
  )
}

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={null}>
      <ResetPasswordContent />
    </Suspense>
  )
}
