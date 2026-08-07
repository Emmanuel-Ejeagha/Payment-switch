"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { Store, ArrowRight, CheckCircle } from "lucide-react"
import type { UserDto } from "@paymentswitch/shared"

export default function OnboardingPage() {
  const router = useRouter()
  const [user, setUser] = useState<UserDto | null>(null)
  const [businessName, setBusinessName] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

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
        setError(body.message ?? body.detail ?? "Onboarding failed")
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

  if (loading) {
    return <div className="h-64 animate-pulse rounded-xl bg-muted" />
  }

  return (
    <div className="mx-auto max-w-lg space-y-6">
      <div className="flex items-center gap-3">
        <div className="rounded-lg bg-primary/10 p-2">
          <Store className="h-5 w-5 text-primary" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold">Finish setting up</h1>
          <p className="text-sm text-muted-foreground">
            Your account exists but has no merchant profile yet.
          </p>
        </div>
      </div>

      {error && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}

      {done ? (
        <div className="flex items-center gap-3 rounded-xl border bg-card p-6">
          <CheckCircle className="h-5 w-5 text-emerald-500" />
          <div>
            <p className="font-medium">Merchant profile created</p>
            <p className="text-sm text-muted-foreground">
              Your account is pending activation. Taking you to the dashboard.
            </p>
          </div>
        </div>
      ) : (
        <div className="space-y-4 rounded-xl border bg-card p-6">
          <div>
            <label htmlFor="businessName" className="mb-1 block text-sm font-medium">
              Business name <span className="text-destructive">*</span>
            </label>
            <input
              id="businessName"
              type="text"
              value={businessName}
              onChange={(e) => setBusinessName(e.target.value)}
              placeholder="Your Business Ltd."
              className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Email</label>
            <input
              type="email"
              value={user?.email ?? ""}
              disabled
              className="w-full rounded-lg border bg-muted px-3 py-2 text-sm text-muted-foreground"
            />
            <p className="mt-1 text-xs text-muted-foreground">
              Taken from your sign-in account and cannot be changed here.
            </p>
          </div>
          <button
            onClick={onboard}
            disabled={submitting || businessName.trim().length < 2}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            {submitting ? "Creating..." : (
              <>Create merchant profile <ArrowRight className="h-4 w-4" /></>
            )}
          </button>
        </div>
      )}
    </div>
  )
}
