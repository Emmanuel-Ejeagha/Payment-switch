"use client"

import { useEffect, useState } from "react"
import { useParams } from "next/navigation"
import { ArrowLeft, Calendar, Building2, DollarSign, Receipt } from "lucide-react"
import Link from "next/link"
import type { SettlementBatchDto, MerchantDto } from "@paymentswitch/shared"
import { StatusBadge } from "@paymentswitch/ui"

function format(amount: number, currency = "USD") {
  return new Intl.NumberFormat("en-US", { style: "currency", currency }).format(amount)
}

export default function SettlementDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [batch, setBatch] = useState<SettlementBatchDto | null>(null)
  const [merchants, setMerchants] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function load() {
      const res = await fetch(`/api/proxy/settlement/api/v1/settlement/${id}`)
      if (res.ok) setBatch(await res.json())
      setLoading(false)
    }
    load()
  }, [id])

  useEffect(() => {
    async function loadMerchants() {
      const res = await fetch("/api/proxy/merchant/api/v1/merchants?skip=0&take=100")
      if (res.ok) {
        const list: MerchantDto[] = await res.json()
        const map: Record<string, string> = {}
        list.forEach((m) => { map[m.id] = m.businessName })
        setMerchants(map)
      }
    }
    loadMerchants()
  }, [])

  if (loading) {
    return <div className="h-64 animate-pulse rounded-xl bg-muted" />
  }

  if (!batch) {
    return (
      <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-destructive">
        Settlement batch not found
      </div>
    )
  }

  const totalGross = batch.payouts?.reduce((s, p) => s + p.grossVolume, 0) || 0
  const totalFees = batch.payouts?.reduce((s, p) => s + p.fees, 0) || 0
  const totalNet = batch.payouts?.reduce((s, p) => s + p.netAmount, 0) || 0

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/settlements" className="rounded-lg border p-2 hover:bg-accent">
          <ArrowLeft className="h-4 w-4" />
        </Link>
        <div>
          <h1 className="text-3xl font-semibold">Settlement Batch</h1>
          <p className="text-sm text-muted-foreground">{id}</p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-xl border p-6">
          <div className="flex items-center gap-3 mb-3">
            <div className="rounded-lg bg-muted p-2">
              <Calendar className="h-5 w-5 text-muted-foreground" />
            </div>
            <p className="text-sm text-muted-foreground">Batch Date</p>
          </div>
          <p className="text-2xl font-semibold">
            {new Date(batch.batchDate).toLocaleDateString()}
          </p>
        </div>
        <div className="rounded-xl border p-6">
          <div className="flex items-center gap-3 mb-3">
            <div className="rounded-lg bg-muted p-2">
              <Receipt className="h-5 w-5 text-muted-foreground" />
            </div>
            <p className="text-sm text-muted-foreground">Status</p>
          </div>
          <StatusBadge status={batch.status} />
        </div>
        <div className="rounded-xl border p-6">
          <div className="flex items-center gap-3 mb-3">
            <div className="rounded-lg bg-muted p-2">
              <DollarSign className="h-5 w-5 text-muted-foreground" />
            </div>
            <p className="text-sm text-muted-foreground">Total Amount</p>
          </div>
          <p className="text-2xl font-semibold">{format(batch.totalAmount)}</p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-xl border p-4">
          <p className="text-sm text-muted-foreground mb-1">Gross Volume</p>
          <p className="text-xl font-semibold">{format(totalGross)}</p>
        </div>
        <div className="rounded-xl border p-4">
          <p className="text-sm text-muted-foreground mb-1">Total Fees</p>
          <p className="text-xl font-semibold text-red-500">-{format(totalFees)}</p>
        </div>
        <div className="rounded-xl border p-4">
          <p className="text-sm text-muted-foreground mb-1">Net Amount</p>
          <p className="text-xl font-semibold text-emerald-600">{format(totalNet)}</p>
        </div>
      </div>

      <div className="rounded-xl border">
        <div className="flex items-center gap-2 border-b px-6 py-4">
          <Building2 className="h-5 w-5 text-muted-foreground" />
          <h2 className="font-semibold">Payouts ({batch.payouts?.length || 0})</h2>
        </div>
        {(!batch.payouts || batch.payouts.length === 0) ? (
          <div className="flex flex-col items-center py-12 text-center">
            <Receipt className="mb-2 h-6 w-6 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">No payouts in this batch</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50 text-left text-sm text-muted-foreground">
                <th className="px-6 py-3 font-medium">Merchant</th>
                <th className="px-6 py-3 font-medium">Gross Volume</th>
                <th className="px-6 py-3 font-medium">Fees</th>
                <th className="px-6 py-3 font-medium">Net Amount</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {batch.payouts.map((p, i) => (
                <tr key={i} className="text-sm hover:bg-muted/30">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <Building2 className="h-4 w-4 text-muted-foreground shrink-0" />
                      <span className="truncate max-w-[200px]">
                        {merchants[p.merchantId] || p.merchantId.slice(0, 8) + "..."}
                      </span>
                    </div>
                  </td>
                  <td className="px-6 py-4 font-medium">
                    {format(p.grossVolume, p.currency)}
                  </td>
                  <td className="px-6 py-4 text-red-500">
                    -{format(p.fees, p.currency)}
                  </td>
                  <td className="px-6 py-4 font-medium text-emerald-600">
                    {format(p.netAmount, p.currency)}
                  </td>
                </tr>
              ))}
            </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
