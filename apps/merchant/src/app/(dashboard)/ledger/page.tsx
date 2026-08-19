"use client"

import { useEffect, useState } from "react"
import { BookOpen, ChevronLeft, ChevronRight, Clock, Lock, Wallet } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { BalanceDto, LedgerTransactionDto } from "@paymentswitch/shared"
import {
  Badge,
  Button,
  Card,
  CardFooter,
  CardHeader,
  EmptyState,
  ErrorPanel,
  PageHeader,
  SectionLabel,
  StatSkeleton,
  StatTile,
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
  type Tone,
} from "@/components/ui"
import { formatFigure, formatDate, humanize } from "@/lib/format"

// Ledger movements read as four distinct things, so they get four distinct tones
// rather than a single neutral pill: money in, money out, money held, money freed.
const TX_TONES: Record<string, Tone> = {
  Credit: "success",
  Debit: "danger",
  Reserve: "warning",
  Release: "info",
}

export default function MerchantLedgerPage() {
  const { merchant, loading: merchantLoading, error: merchantError } = useMerchant()
  const [balances, setBalances] = useState<BalanceDto[]>([])
  const [transactions, setTransactions] = useState<LedgerTransactionDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [fetching, setFetching] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [skip, setSkip] = useState(0)
  const take = 10
  const loading = merchantLoading || (merchant !== null && !dataReady)

  useEffect(() => {
    if (merchantLoading) return
    if (!merchant) return
    const m = merchant
    let cancelled = false
    async function load() {
      setFetching(true)
      try {
        const [balRes, txRes] = await Promise.all([
          fetch(`/api/proxy/ledger/api/v1/ledger/balances?merchantId=${m.id}`),
          fetch(`/api/proxy/ledger/api/v1/ledger/transactions?merchantId=${m.id}&skip=${skip}&take=${take}`),
        ])
        if (cancelled) return
        if (balRes.ok) setBalances(await balRes.json())
        if (txRes.ok) setTransactions(await txRes.json())
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load ledger")
      } finally {
        if (!cancelled) {
          setDataReady(true)
          setFetching(false)
        }
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [merchantLoading, merchant, skip])

  if (loading) {
    return (
      <div className="space-y-8">
        <PageHeader title="Ledger" description="Balance and transactions" />
        <StatSkeleton count={3} />
        <Card className="overflow-hidden">
          <CardHeader title="Transaction history" icon={BookOpen} />
          <TableSkeleton rows={5} columns={4} />
        </Card>
      </div>
    )
  }

  if (error || merchantError) {
    return (
      <div className="space-y-8">
        <PageHeader title="Ledger" description="Balance and transactions" />
        <ErrorPanel title="Failed to load ledger" message={error || merchantError} />
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="Ledger"
        description={
          merchant
            ? `Every movement of money for ${merchant.businessName}, split by currency. Available funds are yours to pay out; pending and reserved are held until settlement clears.`
            : "Every movement of money, split by currency."
        }
      />

      {balances.length === 0 ? (
        <Card>
          <EmptyState
            icon={Wallet}
            title="No balances yet"
            description="Balances appear here once your first payment is captured. Nothing has settled into your ledger so far."
          />
        </Card>
      ) : (
        <div className="space-y-6">
          {balances.map((b) => (
            <section key={b.currency} className="space-y-3">
              <SectionLabel>{b.currency}</SectionLabel>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <StatTile
                  label="Available"
                  value={formatFigure(b.available)}
                  unit={b.currency}
                  icon={Wallet}
                  tone="success"
                  hint="Clear to pay out"
                />
                <StatTile
                  label="Pending"
                  value={formatFigure(b.pending)}
                  unit={b.currency}
                  icon={Clock}
                  tone="warning"
                  hint="Captured, awaiting settlement"
                />
                <StatTile
                  label="Reserved"
                  value={formatFigure(b.reserved)}
                  unit={b.currency}
                  icon={Lock}
                  tone="info"
                  hint="Held against risk or disputes"
                />
              </div>
            </section>
          ))}
        </div>
      )}

      <Card className="overflow-hidden">
        <CardHeader
          title="Transaction history"
          description={
            transactions.length === 0
              ? "Nothing on this page"
              : `Showing ${skip + 1}–${skip + transactions.length}`
          }
          icon={BookOpen}
        />
        {transactions.length === 0 ? (
          <EmptyState
            icon={BookOpen}
            title={skip === 0 ? "No transactions yet" : "Nothing on this page"}
            description={
              skip === 0
                ? "Credits, debits, reserves, and releases all land here as soon as money starts moving."
                : "You have paged past the end of your history."
            }
            action={
              skip > 0 ? (
                <Button icon={ChevronLeft} onClick={() => setSkip(Math.max(0, skip - take))}>
                  Back a page
                </Button>
              ) : undefined
            }
          />
        ) : (
          <>
            <TableWrap>
              <THead>
                <TH>Type</TH>
                <TH align="right">Amount</TH>
                <TH>Description</TH>
                <TH>Date</TH>
              </THead>
              <TBody>
                {transactions.map((t) => (
                  <TR key={t.id}>
                    <TD>
                      <Badge tone={TX_TONES[t.type] ?? "neutral"} dot>
                        {humanize(t.type)}
                      </Badge>
                    </TD>
                    <TD align="right" className="tabular whitespace-nowrap font-medium">
                      {formatFigure(t.amount)}
                      <span className="ml-1.5 text-xs font-normal text-muted-foreground">
                        {t.currency}
                      </span>
                    </TD>
                    <TD className="max-w-[20rem] truncate text-muted-foreground">
                      {t.description || "—"}
                    </TD>
                    <TD className="whitespace-nowrap text-xs text-muted-foreground">
                      {formatDate(t.timestamp)}
                    </TD>
                  </TR>
                ))}
              </TBody>
            </TableWrap>
            <CardFooter>
              <p className="text-xs text-muted-foreground">
                Showing {skip + 1}–{skip + transactions.length}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  size="sm"
                  icon={ChevronLeft}
                  onClick={() => setSkip(Math.max(0, skip - take))}
                  disabled={skip === 0 || fetching}
                >
                  Previous
                </Button>
                <Button
                  size="sm"
                  onClick={() => setSkip(skip + take)}
                  disabled={transactions.length < take || fetching}
                >
                  Next
                  <ChevronRight className="h-4 w-4" aria-hidden="true" />
                </Button>
              </div>
            </CardFooter>
          </>
        )}
      </Card>
    </div>
  )
}
