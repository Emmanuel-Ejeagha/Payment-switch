"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import type { UserDto, MerchantDto } from "@paymentswitch/shared"

export function useMerchant() {
  const router = useRouter()
  const [user, setUser] = useState<UserDto | null>(null)
  const [merchant, setMerchant] = useState<MerchantDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const userRes = await fetch("/api/proxy/identity/api/v1/users/me")
        if (!userRes.ok) {
          if (!cancelled) setError("Failed to load user")
          return
        }
        const userData: UserDto = await userRes.json()
        if (cancelled) return
        setUser(userData)

        const merchantRes = await fetch(
          `/api/proxy/merchant/api/v1/merchants/by-email/${encodeURIComponent(userData.email)}`
        )
        if (merchantRes.status === 404) {
          if (!cancelled) {
            router.replace("/onboarding")
            setLoading(false)
          }
          return
        }
        if (!merchantRes.ok) {
          if (!cancelled) setError("Failed to load merchant profile")
          return
        }
        const merchantData: MerchantDto = await merchantRes.json()
        if (!cancelled) setMerchant(merchantData)
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load merchant profile")
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [router])

  return { user, merchant, merchantId: merchant?.id ?? null, loading, error }
}
