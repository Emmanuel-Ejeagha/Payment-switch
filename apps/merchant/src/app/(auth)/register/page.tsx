"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { useState } from "react"
import { ArrowRight, Eye, EyeOff, Loader2, AlertCircle, MailCheck } from "lucide-react"
import { AuthShell, inputClass, labelClass, submitClass } from "@/components/auth/auth-shell"
import { authArtwork } from "@/lib/images"

const registerSchema = z.object({
  businessName: z.string().min(2, "Business name must be at least 2 characters"),
  email: z.string().email("Invalid email address"),
  password: z
    .string()
    .min(10, "Password must be at least 10 characters")
    .regex(/[A-Za-z]/, "Password must contain at least one letter")
    .regex(/[0-9]/, "Password must contain at least one digit"),
  confirmPassword: z.string(),
}).refine((d) => d.password === d.confirmPassword, {
  message: "Passwords do not match",
  path: ["confirmPassword"],
})

type RegisterForm = z.infer<typeof registerSchema>

const benefits = [
  "Accept payments online in minutes",
  "Real-time transaction monitoring",
  "Automated daily settlements",
]

export default function RegisterPage() {
  const router = useRouter()
  const [error, setError] = useState<string | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [registered, setRegistered] = useState(false)
  const [registeredEmail, setRegisteredEmail] = useState("")
  const [resending, setResending] = useState(false)
  const [resendMsg, setResendMsg] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
  })

  const resendVerification = async () => {
    setResending(true)
    setResendMsg(null)
    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/resend-verification`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email: registeredEmail }),
        }
      )
      if (!res.ok) {
        const body = await res.json()
        setResendMsg(body.message ?? body.detail ?? "Could not resend the email. Try again shortly.")
        return
      }
      setResendMsg("A new verification email is on its way.")
    } catch {
      setResendMsg("Backend unreachable. Please try again later.")
    } finally {
      setResending(false)
    }
  }

  const onSubmit = async (data: RegisterForm) => {
    setError(null)

    try {
      const registerRes = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/register`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            email: data.email,
            password: data.password,
            fullName: data.businessName,
          }),
        }
      )

      if (!registerRes.ok) {
        const body = await registerRes.json()
        setError(body.message ?? body.detail ?? "Registration failed")
        return
      }

      const { userId } = await registerRes.json()

      const onboardRes = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/merchant/api/v1/merchants`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            ownerId: userId,
            businessName: data.businessName,
            email: data.email,
          }),
        }
      )

      if (!onboardRes.ok) {
        // The identity account exists, so signing in works — /onboarding lets them
        // retry the merchant profile instead of stranding them here.
        setError("Account created, but merchant setup did not finish. Sign in to complete it.")
        return
      }

      setRegistered(true)
      setRegisteredEmail(data.email)
    } catch {
      setError("Backend unreachable. Please try again later.")
    }
  }

  if (registered) {
    return (
      <AuthShell
        image={authArtwork.register}
        panelHeading="Start accepting payments today"
        panelBody="Join the merchants using PaymentSwitch to power their payment infrastructure — from first checkout to daily payout."
        highlights={benefits}
        heading="Check your inbox"
        subheading={`We sent a verification link to ${registeredEmail}`}
        footer={
          <>
            Already verified?{" "}
            <Link href="/login" className="font-medium text-primary hover:underline">
              Sign in
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
              Confirm your email to activate your account. The link expires in 24 hours.
              Once verified you can sign in and manage your merchant dashboard.
            </p>
          </div>

          {resendMsg && (
            <div
              role="status"
              className="flex items-start gap-2.5 rounded-lg border border-primary/30 bg-primary/10 p-3 text-sm text-primary"
            >
              <span>{resendMsg}</span>
            </div>
          )}

          <button
            type="button"
            onClick={resendVerification}
            disabled={resending}
            className={submitClass}
          >
            {resending ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                Sending...
              </>
            ) : (
              "Resend verification email"
            )}
          </button>

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
      image={authArtwork.register}
      panelHeading="Start accepting payments today"
      panelBody="Join the merchants using PaymentSwitch to power their payment infrastructure — from first checkout to daily payout."
      highlights={benefits}
      heading="Create your account"
      subheading="Start accepting payments in minutes"
      footer={
        <>
          Already have an account?{" "}
          <Link href="/login" className="font-medium text-primary hover:underline">
            Sign in
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">
        <div className="space-y-2">
          <label htmlFor="businessName" className={labelClass}>
            Business name
          </label>
          <input
            id="businessName"
            type="text"
            autoComplete="organization"
            placeholder="Your Business Ltd."
            aria-invalid={!!errors.businessName}
            aria-describedby={errors.businessName ? "businessName-error" : undefined}
            className={inputClass}
            {...register("businessName")}
          />
          {errors.businessName && (
            <p id="businessName-error" className="text-xs text-destructive">
              {errors.businessName.message}
            </p>
          )}
        </div>

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

        <div className="space-y-2">
          <label htmlFor="password" className={labelClass}>
            Password
          </label>
          <div className="relative">
            <input
              id="password"
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              placeholder="Min. 6 characters"
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
            Confirm password
          </label>
          <div className="relative">
            <input
              id="confirmPassword"
              type={showConfirm ? "text" : "password"}
              autoComplete="new-password"
              placeholder="Repeat your password"
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
              Creating account...
            </>
          ) : (
            <>
              Create account
              <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </>
          )}
        </button>

        <p className="text-center text-xs leading-relaxed text-muted-foreground">
          By creating an account you agree to our terms of service and privacy policy.
        </p>
      </form>
    </AuthShell>
  )
}
