"use client"

import { useEffect, useState, useCallback } from "react"
import { ExternalLink, Link2, Plus } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { PaymentLinkDto } from "@paymentswitch/shared"
import {
  Alert,
  AmountInput,
  Badge,
  Button,
  Card,
  CardBody,
  CardHeader,
  CopyButton,
  EmptyState,
  Field,
  IdCell,
  Input,
  PageHeader,
  Select,
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
} from "@/components/ui"
import { formatAmount, formatDate } from "@/lib/format"

const SUPPORTED_CURRENCIES = ["USD", "EUR", "GBP", "NGN"]

export default function PaymentLinksPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const [links, setLinks] = useState<PaymentLinkDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const [showForm, setShowForm] = useState(false)
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("USD")
  const [description, setDescription] = useState("")

  // The checkout URL is built from the browser's own origin. Reading it during
  // render keeps the first server-rendered pass and the hydrated pass identical;
  // the browser-only origin is resolved with a render-time state adjustment.
  const [origin, setOrigin] = useState("")
  if (origin === "" && typeof window !== "undefined") {
    setOrigin(window.location.origin)
  }

  const loadLinks = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/payment-links?merchantId=${merchantId}&skip=0&take=50`)
    if (res.ok) setLinks(await res.json())
  }, [])

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    const mid = merchantId
    async function load() {
      try {
        await loadLinks(mid)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load payment links")
      } finally {
        setDataReady(true)
      }
    }
    load()
  }, [merchantLoading, merchantId, loadLinks])

  const createLink = async () => {
    if (!merchantId) return
    setCreating(true)
    setError(null)
    try {
      const res = await fetch("/api/proxy/payment/api/v1/payment-links", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          merchantId,
          amount: Math.round(parseFloat(amount) * 100),
          currency,
          description,
        }),
      })
      if (!res.ok) {
        const body = await res.json()
        setError(body.message ?? body.detail ?? "Failed to create payment link")
        return
      }
      await loadLinks(merchantId)
      setShowForm(false)
      setAmount("")
      setDescription("")
    } finally {
      setCreating(false)
    }
  }

  const checkoutUrl = (code: string) => `${origin}/checkout/${code}`

  return (
    <div className="space-y-8">
      <PageHeader
        title="Payment links"
        description="Shareable checkout pages. Send a link by email, chat, or invoice and the customer pays without you writing any integration code."
        actions={
          <Button
            variant={showForm ? "secondary" : "primary"}
            icon={showForm ? undefined : Plus}
            onClick={() => setShowForm((v) => !v)}
            disabled={!merchantId}
          >
            {showForm ? "Cancel" : "New link"}
          </Button>
        }
      />

      {(error || merchantError) && (
        <Alert variant="error" title="Something went wrong">
          {error || merchantError}
        </Alert>
      )}

      {showForm && (
        <Card>
          <CardHeader title="New payment link" description="The amount and currency are fixed once the link is created." />
          <CardBody className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <Field label="Amount" htmlFor="link-amount" required>
                <AmountInput
                  id="link-amount"
                  currency={currency}
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="100.00"
                />
              </Field>
              <Field label="Currency" htmlFor="link-currency">
                <Select id="link-currency" value={currency} onChange={(e) => setCurrency(e.target.value)}>
                  {SUPPORTED_CURRENCIES.map((c) => (
                    <option key={c} value={c}>{c}</option>
                  ))}
                </Select>
              </Field>
              <Field
                label="Description"
                htmlFor="link-description"
                hint="Shown to the customer on the checkout page."
              >
                <Input
                  id="link-description"
                  type="text"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Pro plan subscription"
                />
              </Field>
            </div>
            <div className="flex justify-end">
              <Button variant="primary" icon={Link2} onClick={createLink} pending={creating} disabled={!amount}>
                {creating ? "Creating…" : "Create link"}
              </Button>
            </div>
          </CardBody>
        </Card>
      )}

      <Card className="overflow-hidden">
        <CardHeader
          title="Your links"
          description={loading ? "Loading…" : `${links.length} total`}
          icon={Link2}
        />
        {loading ? (
          <TableSkeleton rows={5} columns={5} />
        ) : links.length === 0 ? (
          <EmptyState
            icon={Link2}
            title="No payment links yet"
            description="A payment link is the fastest way to take a payment — no integration, just a URL you can share anywhere."
            action={
              <Button variant="primary" icon={Plus} onClick={() => setShowForm(true)} disabled={!merchantId}>
                Create your first link
              </Button>
            }
          />
        ) : (
          <TableWrap>
            <THead>
              <TH>Code</TH>
              <TH align="right">Amount</TH>
              <TH>Description</TH>
              <TH>Status</TH>
              <TH>Created</TH>
              <TH align="right">Checkout link</TH>
            </THead>
            <TBody>
              {links.map((link) => (
                <TR key={link.id}>
                  <TD>
                    <IdCell>{link.code}</IdCell>
                  </TD>
                  <TD align="right" className="tabular whitespace-nowrap font-medium">
                    {formatAmount(link.amount, link.currency)}
                  </TD>
                  <TD className="max-w-[16rem] truncate">{link.description || "—"}</TD>
                  <TD>
                    <Badge tone={link.active ? "success" : "neutral"} dot>
                      {link.active ? "Active" : "Inactive"}
                    </Badge>
                  </TD>
                  <TD className="whitespace-nowrap text-xs text-muted-foreground">
                    {formatDate(link.createdAt)}
                  </TD>
                  <TD align="right">
                    <div className="flex items-center justify-end gap-1.5">
                      <CopyButton value={checkoutUrl(link.code)} label="Copy" />
                      <a
                        href={checkoutUrl(link.code)}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                      >
                        <ExternalLink className="h-3.5 w-3.5" aria-hidden="true" />
                        <span className="sr-only">Open checkout for {link.code}</span>
                      </a>
                    </div>
                  </TD>
                </TR>
              ))}
            </TBody>
          </TableWrap>
        )}
      </Card>
    </div>
  )
}
