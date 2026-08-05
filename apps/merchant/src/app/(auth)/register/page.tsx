"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { useState } from "react"
import { Store, ArrowRight, Check, Eye, EyeOff } from "lucide-react"

const registerSchema = z.object({
  businessName: z.string().min(2, "Business name must be at least 2 characters"),
  email: z.string().email("Invalid email address"),
  password: z.string().min(6, "Password must be at least 6 characters"),
  confirmPassword: z.string(),
}).refine((d) => d.password === d.confirmPassword, {
  message: "Passwords do not match",
  path: ["confirmPassword"],
})

type RegisterForm = z.infer<typeof registerSchema>

const benefits = [
  "Accept payments online",
  "Real-time transaction monitoring",
  "Automated settlements",
]

export default function RegisterPage() {
  const router = useRouter()
  const [error, setError] = useState<string | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
  })

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

      router.push("/login")
    } catch {
      setError("Backend unreachable. Please try again later.")
    }
  }

  return (
    <div className="flex min-h-screen">
      <div className="relative hidden w-1/2 flex-col justify-between bg-gradient-to-br from-indigo-900 via-indigo-800 to-indigo-900 p-12 text-white lg:flex">
        <div className="flex items-center gap-3">
          <div className="rounded-lg bg-white/10 p-2">
            <Store className="h-6 w-6" />
          </div>
          <span className="text-lg font-semibold">PaymentSwitch</span>
        </div>
        <div className="space-y-6">
          <h2 className="text-4xl font-bold leading-tight tracking-tight">
            Start accepting payments today
          </h2>
          <p className="max-w-md text-base text-slate-400">
            Join thousands of merchants using PaymentSwitch to power their payment infrastructure.
          </p>
          <ul className="space-y-3">
            {benefits.map((b) => (
              <li key={b} className="flex items-center gap-3 text-sm text-slate-300">
                <span className="rounded-full bg-emerald-500/20 p-0.5">
                  <Check className="h-3.5 w-3.5 text-emerald-400" />
                </span>
                {b}
              </li>
            ))}
          </ul>
        </div>
        <p className="text-xs text-slate-500">
          &copy; {new Date().getFullYear()} PaymentSwitch. All rights reserved.
        </p>
      </div>

      <div className="flex w-full items-center justify-center px-6 lg:w-1/2">
        <div className="w-full max-w-sm space-y-8">
          <div className="space-y-2">
            <h1 className="text-2xl font-semibold tracking-tight">Create your account</h1>
            <p className="text-sm text-muted-foreground">Start accepting payments in minutes</p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div className="space-y-2">
              <label htmlFor="businessName" className="text-sm font-medium">Business name</label>
              <input
                id="businessName"
                type="text"
                placeholder="Your Business Ltd."
                className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                {...register("businessName")}
              />
              {errors.businessName && <p className="text-xs text-destructive">{errors.businessName.message}</p>}
            </div>

            <div className="space-y-2">
              <label htmlFor="email" className="text-sm font-medium">Email</label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                placeholder="name@example.com"
                className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                {...register("email")}
              />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>

            <div className="space-y-2">
              <label htmlFor="password" className="text-sm font-medium">Password</label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="new-password"
                  placeholder="Min. 6 characters"
                  className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 pr-10 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  {...register("password")}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
            </div>

            <div className="space-y-2">
              <label htmlFor="confirmPassword" className="text-sm font-medium">Confirm password</label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  type={showConfirm ? "text" : "password"}
                  autoComplete="new-password"
                  placeholder="Repeat your password"
                  className="flex h-11 w-full rounded-lg border border-input bg-background px-3 py-2 pr-10 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  {...register("confirmPassword")}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirm(!showConfirm)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                >
                  {showConfirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.confirmPassword && <p className="text-xs text-destructive">{errors.confirmPassword.message}</p>}
            </div>

            {error && (
              <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
            )}

            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"
            >
              {isSubmitting ? "Creating account..." : (
                <>Create account <ArrowRight className="h-4 w-4" /></>
              )}
            </button>
          </form>

          <p className="text-center text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link href="/login" className="font-medium text-primary hover:underline">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  )
}
