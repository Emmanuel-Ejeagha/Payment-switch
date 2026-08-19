"use client"

import { useEffect, useState } from "react"
import { useParams } from "next/navigation"
import Link from "next/link"
import { CheckCircle2, CreditCard, Lock, ShieldCheck, XCircle } from "lucide-react"
import { BrandMark } from "@/components/landing/brand-mark"
import { Alert, Button, CopyButton, Field, Input, Skeleton } from "@/components/ui"
import { formatFigure } from "@/lib/format"

interface CheckoutLink {
  amount: number
  currency: string
  description?: string
  active: boolean
}

/** Groups digits in fours so a long PAN stays readable while it is typed. */
function formatCardNumber(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 19)
  return digits.replace(/(.{4})/g, "$1 ").trim()
}

/** Accepts MM/YY and MM/YYYY — the submit handler resolves the century either way. */
function formatExpiry(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 6)
  if (digits.length <= 2) return digits
  return `${digits.slice(0, 2)}/${digits.slice(2)}`
}

/** Display-only brand hint from the IIN range. The acquirer decides the real one. */
function brandOf(value: string) {
  const d = value.replace(/\D/g, "")
  if (!d) return null
  if (/^4/.test(d)) return "Visa"
  if (/^(5[1-5]|2[2-7])/.test(d)) return "Mastercard"
  if (/^3[47]/.test(d)) return "Amex"
  if (/^(6011|65)/.test(d)) return "Discover"
  if (/^(5061|5078|6500)/.test(d)) return "Verve"
  return null
}

function CheckoutShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center overflow-hidden bg-surface px-4 py-10">
      {/* Decorative wash. Pure CSS, so the page never waits on an image. */}
      <div
        className="pointer-events-none absolute inset-x-0 top-0 h-64 bg-gradient-to-b from-primary/10 to-transparent"
        aria-hidden="true"
      />
      <div className="relative w-full max-w-md space-y-5">
        <Link
          href="/"
          className="flex items-center justify-center gap-2.5 transition-opacity hover:opacity-80"
        >
          <BrandMark className="h-8 w-8" />
          <span className="text-lg font-semibold tracking-tight">PaymentSwitch</span>
        </Link>
        {children}
        <p className="flex items-center justify-center gap-1.5 text-xs text-muted-foreground">
          <Lock className="h-3 w-3" aria-hidden="true" />
          Secure checkout powered by PaymentSwitch
        </p>
      </div>
    </div>
  )
}

export default function HostedCheckoutPage() {
  const { code } = useParams<{ code: string }>()
  const [link, setLink] = useState<CheckoutLink | null>(null)
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
      const [rawMonth, rawYear] = expiry.split("/").map((s) => parseInt(s.trim(), 10))
      if (!rawMonth || !rawYear || rawMonth < 1 || rawMonth > 12) {
        setError("Enter expiry as MM/YY")
        return
      }
      // Accept both MM/YY and MM/YYYY; only two-digit years need the century added.
      const expiryYear = rawYear < 100 ? 2000 + rawYear : rawYear

      if (!/^\d{3,4}$/.test(cvc)) {
        setError("Enter the 3 or 4 digit security code")
        return
      }

      const tokenRes = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/payment/v1/checkout/links/${code}/tokenize`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        // No CVC here: the token vault is PCI-scoped and persists only brand, last four
        // and expiry. The CVC travels with /pay instead, where it is forwarded to the
        // acquirer for that one authorization and never stored.
        body: JSON.stringify({
          cardNumber: cardNumber.replace(/\s+/g, ""),
          expiryMonth: rawMonth,
          expiryYear,
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
        body: JSON.stringify({ cardToken: token, securityCode: cvc }),
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
      <CheckoutShell>
        <div className="space-y-4 rounded-2xl border bg-card p-6 shadow-card">
          <Skeleton className="h-4 w-28" />
          <Skeleton className="h-10 w-44" />
          <div className="space-y-3 pt-2">
            <Skeleton className="h-10" />
            <div className="grid grid-cols-2 gap-3">
              <Skeleton className="h-10" />
              <Skeleton className="h-10" />
            </div>
            <Skeleton className="h-11" />
          </div>
        </div>
      </CheckoutShell>
    )
  }

  if (error && !link) {
    return (
      <CheckoutShell>
        <div
          role="alert"
          className="rounded-2xl border bg-card p-8 text-center shadow-card"
        >
          <span
            className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-destructive/10 text-destructive"
            aria-hidden="true"
          >
            <XCircle className="h-6 w-6" />
          </span>
          <h1 className="mt-4 text-lg font-semibold">This link cannot be opened</h1>
          <p className="mt-1.5 text-sm leading-relaxed text-muted-foreground">{error}</p>
          <p className="mt-4 text-xs text-muted-foreground">
            Check the address with whoever sent it to you.
          </p>
        </div>
      </CheckoutShell>
    )
  }

  if (!link) return null

  if (result) {
    const settled = /succe|captur|author|paid/i.test(result.status)
    return (
      <CheckoutShell>
        <div className="rounded-2xl border bg-card p-8 text-center shadow-card">
          <span
            className={`mx-auto flex h-14 w-14 items-center justify-center rounded-2xl ${
              settled
                ? "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400"
                : "bg-amber-500/10 text-amber-600 dark:text-amber-400"
            }`}
            aria-hidden="true"
          >
            <CheckCircle2 className="h-7 w-7" />
          </span>
          <h1 className="mt-5 text-2xl font-semibold tracking-tight" role="status">
            Payment {result.status.toLowerCase()}
          </h1>
          <p className="tabular mt-1 text-sm text-muted-foreground">
            {formatFigure(link.amount)} {link.currency}
            {link.description ? ` · ${link.description}` : ""}
          </p>
          <div className="mt-6 rounded-xl border bg-muted/30 p-4 text-left">
            <p className="text-xs font-medium text-muted-foreground">Reference</p>
            <div className="mt-1.5 flex items-center gap-2">
              <code className="min-w-0 flex-1 break-all font-mono text-xs">{result.intentId}</code>
              <CopyButton value={result.intentId} label="Copy" />
            </div>
          </div>
          <p className="mt-4 text-xs leading-relaxed text-muted-foreground">
            Keep this reference — it identifies your payment if you need to contact the merchant.
          </p>
        </div>
      </CheckoutShell>
    )
  }

  if (link.active === false) {
    return (
      <CheckoutShell>
        <div role="alert" className="rounded-2xl border bg-card p-8 text-center shadow-card">
          <span
            className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-muted text-muted-foreground"
            aria-hidden="true"
          >
            <XCircle className="h-6 w-6" />
          </span>
          <h1 className="mt-4 text-lg font-semibold">This payment link is closed</h1>
          <p className="mt-1.5 text-sm leading-relaxed text-muted-foreground">
            The merchant has deactivated it, so no further payments can be taken. Ask them for a new
            link.
          </p>
        </div>
      </CheckoutShell>
    )
  }

  const brand = brandOf(cardNumber)
  const canPay = !submitting && Boolean(cardNumber && expiry && cvc)

  return (
    <CheckoutShell>
      <div className="overflow-hidden rounded-2xl border bg-card shadow-card">
        <div className="border-b bg-muted/30 px-6 py-5 sm:px-7">
          <p className="text-sm text-muted-foreground">{link.description || "Payment"}</p>
          <p className="tabular mt-1 text-3xl font-semibold tracking-tight sm:text-4xl">
            {formatFigure(link.amount)}
            <span className="ml-2 text-base font-medium text-muted-foreground">{link.currency}</span>
          </p>
        </div>

        <form
          className="space-y-4 px-6 py-6 sm:px-7"
          onSubmit={(e) => {
            e.preventDefault()
            pay()
          }}
        >
          {error && <Alert variant="error">{error}</Alert>}

          <Field label="Card number" htmlFor="cc-number" required>
            <div className="relative">
              <CreditCard
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
                aria-hidden="true"
              />
              <Input
                id="cc-number"
                value={cardNumber}
                onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
                placeholder="4242 4242 4242 4242"
                inputMode="numeric"
                autoComplete="cc-number"
                autoCorrect="off"
                spellCheck={false}
                className={`tabular pl-9 ${brand ? "pr-20" : ""}`}
              />
              {brand && (
                <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs font-medium text-muted-foreground">
                  {brand}
                </span>
              )}
            </div>
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label="Expiry" htmlFor="cc-exp" required>
              <Input
                id="cc-exp"
                value={expiry}
                onChange={(e) => setExpiry(formatExpiry(e.target.value))}
                placeholder="MM/YY"
                inputMode="numeric"
                autoComplete="cc-exp"
                className="tabular"
              />
            </Field>
            <Field label="Security code" htmlFor="cc-csc" required>
              <Input
                id="cc-csc"
                value={cvc}
                onChange={(e) => setCvc(e.target.value.replace(/\D/g, "").slice(0, 4))}
                placeholder="123"
                inputMode="numeric"
                autoComplete="cc-csc"
                className="tabular"
              />
            </Field>
          </div>

          <Button
            type="submit"
            variant="primary"
            size="lg"
            className="w-full"
            pending={submitting}
            disabled={!canPay}
          >
            {submitting
              ? "Processing…"
              : `Pay ${formatFigure(link.amount)} ${link.currency}`}
          </Button>

          <p className="flex items-start gap-2 pt-1 text-xs leading-relaxed text-muted-foreground">
            <ShieldCheck className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            Card details go straight to a PCI-scoped vault. The merchant receives a token, never your
            full card number.
          </p>
        </form>
      </div>
    </CheckoutShell>
  )
}
