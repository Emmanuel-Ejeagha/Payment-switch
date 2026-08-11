"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useState } from "react"
import { CheckCircle2, Eye, EyeOff, KeyRound, Loader2, AlertCircle } from "lucide-react"
import { Card, CardBody, CardHeader, PageHeader } from "@/components/ui"

const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Current password is required"),
    newPassword: z.string().min(8, "Password must be at least 8 characters"),
    confirmPassword: z.string(),
  })
  .refine((d) => d.newPassword === d.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  })
  .refine((d) => d.newPassword !== d.currentPassword, {
    message: "New password must be different from the current password",
    path: ["newPassword"],
  })

type ChangePasswordForm = z.infer<typeof changePasswordSchema>

const inputClass =
  "flex h-11 w-full rounded-lg border border-input bg-background px-3.5 text-sm transition-colors placeholder:text-muted-foreground/70 focus-visible:border-ring focus-visible:outline-none aria-[invalid=true]:border-destructive"

export default function SecurityPage() {
  const [show, setShow] = useState<{ current: boolean; next: boolean; confirm: boolean }>({
    current: false,
    next: false,
    confirm: false,
  })
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordForm>({
    resolver: zodResolver(changePasswordSchema),
  })

  const onSubmit = async (data: ChangePasswordForm) => {
    setError(null)
    setDone(false)

    try {
      const res = await fetch("/api/proxy/identity/api/v1/auth/change-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          currentPassword: data.currentPassword,
          newPassword: data.newPassword,
        }),
      })

      if (!res.ok) {
        const body = await res.json()
        setError(body.message ?? body.detail ?? "Could not change your password. Please try again.")
        return
      }

      setDone(true)
      reset({ currentPassword: "", newPassword: "", confirmPassword: "" })
    } catch {
      setError("Backend unreachable. Please try again later.")
    }
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="Security"
        description="Change your password. All other sessions are signed out when it changes."
      />

      <Card className="max-w-xl">
        <CardHeader
          title="Change password"
          description="Requires your current password. Every existing session is revoked afterwards."
          icon={KeyRound}
        />
        <CardBody>
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">
            <div className="space-y-2">
              <label htmlFor="currentPassword" className="block text-sm font-medium">
                Current password
              </label>
              <div className="relative">
                <input
                  id="currentPassword"
                  type={show.current ? "text" : "password"}
                  autoComplete="current-password"
                  placeholder="Enter your current password"
                  aria-invalid={!!errors.currentPassword}
                  aria-describedby={errors.currentPassword ? "currentPassword-error" : undefined}
                  className={`${inputClass} pr-11`}
                  {...register("currentPassword")}
                />
                <button
                  type="button"
                  onClick={() => setShow((s) => ({ ...s, current: !s.current }))}
                  aria-label={show.current ? "Hide password" : "Show password"}
                  className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
                >
                  {show.current ? (
                    <EyeOff className="h-4 w-4" aria-hidden="true" />
                  ) : (
                    <Eye className="h-4 w-4" aria-hidden="true" />
                  )}
                </button>
              </div>
              {errors.currentPassword && (
                <p id="currentPassword-error" className="text-xs text-destructive">
                  {errors.currentPassword.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <label htmlFor="newPassword" className="block text-sm font-medium">
                New password
              </label>
              <div className="relative">
                <input
                  id="newPassword"
                  type={show.next ? "text" : "password"}
                  autoComplete="new-password"
                  placeholder="Min. 8 characters"
                  aria-invalid={!!errors.newPassword}
                  aria-describedby={errors.newPassword ? "newPassword-error" : undefined}
                  className={`${inputClass} pr-11`}
                  {...register("newPassword")}
                />
                <button
                  type="button"
                  onClick={() => setShow((s) => ({ ...s, next: !s.next }))}
                  aria-label={show.next ? "Hide password" : "Show password"}
                  className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
                >
                  {show.next ? (
                    <EyeOff className="h-4 w-4" aria-hidden="true" />
                  ) : (
                    <Eye className="h-4 w-4" aria-hidden="true" />
                  )}
                </button>
              </div>
              {errors.newPassword && (
                <p id="newPassword-error" className="text-xs text-destructive">
                  {errors.newPassword.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <label htmlFor="confirmPassword" className="block text-sm font-medium">
                Confirm new password
              </label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  type={show.confirm ? "text" : "password"}
                  autoComplete="new-password"
                  placeholder="Repeat your new password"
                  aria-invalid={!!errors.confirmPassword}
                  aria-describedby={errors.confirmPassword ? "confirmPassword-error" : undefined}
                  className={`${inputClass} pr-11`}
                  {...register("confirmPassword")}
                />
                <button
                  type="button"
                  onClick={() => setShow((s) => ({ ...s, confirm: !s.confirm }))}
                  aria-label={show.confirm ? "Hide password" : "Show password"}
                  className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
                >
                  {show.confirm ? (
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

            {done && (
              <div
                role="status"
                className="flex items-start gap-2.5 rounded-lg border border-green-600/30 bg-green-600/10 p-3 text-sm text-green-700 dark:text-green-400"
              >
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
                <span>
                  Password updated. You will be asked to sign in again on your other devices.
                </span>
              </div>
            )}

            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:pointer-events-none disabled:opacity-60"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                  Updating password...
                </>
              ) : (
                "Update password"
              )}
            </button>
          </form>
        </CardBody>
      </Card>
    </div>
  )
}
