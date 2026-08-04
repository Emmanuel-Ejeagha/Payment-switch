"use client"

import { useEffect, useState } from "react"
import { useParams } from "next/navigation"
import { Store, CreditCard, CheckCircle, XCircle } from "lucide-react"

export default function HostedCheckoutPage() {
  const { code } = useParams<{ code: string }>()
  const [link, setLink] = useState<{ amount: number; currency: string; description?: string; active: boolean } | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [cardNumber, setCardNumber] = useState("")
  const [expiry, setExpiry] = useState("")
  const [cvc, setCvc] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<{ intentId: string; status: string } | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/payment/v1/checkout/links/${code}`)
        if (!res.ok) {
          const body = await res.json()
          setError(body.detail ?? body.title ?? "Payment link not found")
          return
        }
        setLink(await res.json())
      } catch {
        setError("Failed to load payment link")
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [code])

  const pay = async () => {
    setSubmitting(true)
    setError(null)
    try {
      const [expMonth, expYear] = expiry.split("/").map((s) => parseInt(s.trim(), 10))
      if (!expMonth || !expYear) {
        setError("Enter expiry as MM/YY")
        return
      }

      const tokenRes = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/payment/v1/checkout/links/${code}/tokenize`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          cardNumber: cardNumber.replace(/\s+/g, ""),
          expiryMonth: expMonth,
          expiryYear: 2000 + (expYear < 100 ? expYear : expYear),
        }),
      })
      if (!tokenRes.ok) {
        const body = await tokenRes.json()
        setError(body.detail ?? body.title ?? "Card could not be tokenized")
        return
      }
      const { token } = await tokenRes.json()

      const payRes = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/payment/v1/checkout/links/${code}/pay`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Idempotency-Key": crypto.randomUUID(),
        },
        body: JSON.stringify({ cardToken: token }),
      })
      if (!payRes.ok) {
        const body = await payRes.json()
        setError(body.detail ?? body.title ?? "Payment failed")
        return
      }
      const data = await payRes.json()
      setResult({ intentId: data.intentId, status: data.status })
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
        <div className="h-96 w-full max-w-md animate-pulse rounded-2xl bg-muted" />
      </div>
    )
  }

  if (error && !link) {
    return (
      <div className="flex min-h-screen items-center justify-center p-4">
        <div className="w-full max-w-md rounded-2xl border border-destructive/50 bg-destructive/10 p-6 text-center">
          <XCircle className="mx-auto h-10 w-10 text-destructive" />
          <p className="mt-3 font-medium text-destructive">{error}</p>
        </div>
      </div>
    )
  }

  if (!link) return null

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <div className="w-full max-w-md space-y-4">
        <div className="flex items-center justify-center gap-2 pt-4 text-muted-foreground">
          <div className="rounded-lg bg-primary p-2 text-primary-foreground">
            <Store className="h-5 w-5" />
          </div>
          <span className="font-semibold text-foreground">PaymentSwitch</span>
        </div>

        {result ? (
          <div className="rounded-2xl border bg-card p-8 text-center shadow-sm">
            <CheckCircle className="mx-auto h-12 w-12 text-green-500" />
            <h1 className="mt-4 text-2xl font-semibold">Payment {result.status.toLowerCase()}</h1>
            <p className="mt-2 font-mono text-xs text-muted-foreground">{result.intentId}</p>
          </div>
        ) : (
          <div className="rounded-2xl border bg-card p-8 shadow-sm">
            <h1 className="text-xl font-semibold">{link.description || "Payment"}</h1>
            <p className="mt-2 text-4xl font-bold">
              {(link.amount / 100).toFixed(2)} <span className="text-xl font-medium text-muted-foreground">{link.currency}</span>
            </p>

            {error && <div className="mt-4 rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}

            <div className="mt-6 space-y-4">
              <div>
                <label className="mb-1 block text-sm font-medium">Card number</label>
                <div className="relative">
                  <CreditCard className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <input
                    value={cardNumber}
                    onChange={(e) => setCardNumber(e.target.value)}
                    placeholder="4242 4242 4242 4242"
                    inputMode="numeric"
                    className="w-full rounded-lg border bg-background py-2 pl-10 pr-3 text-sm outline-none focus:ring-2 focus:ring-primary"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="mb-1 block text-sm font-medium">Expiry</label>
                  <input
                    value={expiry}
                    onChange={(e) => setExpiry(e.target.value)}
                    placeholder="MM/YY"
                    inputMode="numeric"
                    className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">CVC</label>
                  <input
                    value={cvc}
                    onChange={(e) => setCvc(e.target.value)}
                    placeholder="123"
                    inputMode="numeric"
                    className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                  />
                </div>
              </div>
              <button
                onClick={pay}
                disabled={submitting || !cardNumber || !expiry}
                className="w-full rounded-lg bg-primary py-3 text-sm font-semibold text-primary-foreground hover:opacity-90 disabled:opacity-50"
              >
                {submitting ? "Processing..." : `Pay ${(link.amount / 100).toFixed(2)} ${link.currency}`}
              </button>
            </div>
          </div>
        )}

        <p className="pb-6 text-center text-xs text-muted-foreground">
          Secure checkout powered by PaymentSwitch
        </p>
      </div>
    </div>
  )
}
