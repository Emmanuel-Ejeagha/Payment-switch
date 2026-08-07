"use client"

import { useEffect, useState, useCallback } from "react"
import { Plus, X, Edit, Trash2, Users } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { CustomerDto } from "@paymentswitch/shared"

export default function CustomersPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const [customers, setCustomers] = useState<CustomerDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const loading = merchantLoading || (merchantId !== null && !dataReady)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [email, setEmail] = useState("")
  const [name, setName] = useState("")
  const [phone, setPhone] = useState("")
  const [description, setDescription] = useState("")

  const loadCustomers = useCallback(async (merchantId: string) => {
    const res = await fetch(`/api/proxy/payment/api/v1/customers?merchantId=${merchantId}&skip=0&take=100`)
    if (res.ok) setCustomers(await res.json())
  }, [])

  useEffect(() => {
    if (merchantLoading) return
    if (!merchantId) return
    const mid = merchantId
    async function load() {
      try {
        await loadCustomers(mid)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load customers")
      } finally {
        setDataReady(true)
      }
    }
    load()
  }, [merchantLoading, merchantId, loadCustomers])

  const openForm = (customer?: CustomerDto) => {
    if (customer) {
      setEditingId(customer.id)
      setEmail(customer.email)
      setName(customer.name || "")
      setPhone(customer.phone || "")
      setDescription(customer.description || "")
    } else {
      setEditingId(null)
      setEmail("")
      setName("")
      setPhone("")
      setDescription("")
    }
    setShowForm(true)
    setError(null)
  }

  const closeForm = () => {
    setShowForm(false)
    setEditingId(null)
    setEmail("")
    setName("")
    setPhone("")
    setDescription("")
  }

  const save = async () => {
    if (!merchantId || !email) return
    setWorking(true)
    setError(null)
    try {
      if (editingId) {
        const res = await fetch(`/api/proxy/payment/api/v1/customers/${editingId}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ merchantId, email, name: name || null, phone: phone || null, description: description || null }),
        })
        if (!res.ok) {
          const body = await res.json().catch(() => ({}))
          setError(body.message ?? body.detail ?? "Update failed")
          return
        }
      } else {
        const res = await fetch("/api/proxy/payment/api/v1/customers", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ merchantId, email, name: name || null, phone: phone || null, description: description || null }),
        })
        if (!res.ok) {
          const body = await res.json().catch(() => ({}))
          setError(body.message ?? body.detail ?? "Create failed")
          return
        }
      }
      await loadCustomers(merchantId)
      closeForm()
    } finally {
      setWorking(false)
    }
  }

  const deleteCustomer = async (id: string) => {
    if (!merchantId || !confirm("Delete this customer?")) return
    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/customers/${id}?merchantId=${merchantId}`, { method: "DELETE" })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? body.detail ?? "Delete failed")
        return
      }
      await loadCustomers(merchantId)
    } finally {
      setWorking(false)
    }
  }

  if (loading) {
    return <div className="h-64 animate-pulse rounded-xl bg-muted" />
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold">Customers</h1>
          <p className="text-sm text-muted-foreground">Manage your customer directory.</p>
        </div>
        <button
          onClick={() => openForm()}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90"
        >
          <Plus className="h-4 w-4" />
          New customer
        </button>
      </div>

      {(error || merchantError) && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error || merchantError}</div>}

      {showForm && (
        <div className="rounded-xl border bg-card p-6">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-semibold">{editingId ? "Edit customer" : "New customer"}</h2>
            <button onClick={closeForm} className="rounded-lg p-1 hover:bg-accent">
              <X className="h-4 w-4" />
            </button>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium">Email <span className="text-destructive">*</span></label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="customer@example.com"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Name</label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="John Doe"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Phone</label>
              <input
                type="text"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="+1234567890"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Description</label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="VIP customer"
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>
          <div className="mt-4">
            <button
              onClick={save}
              disabled={working || !email}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {working ? "Saving..." : "Save"}
            </button>
          </div>
        </div>
      )}

      <div className="rounded-xl border bg-card">
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold">All customers</h2>
        </div>
        {customers.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
            <Users className="h-8 w-8" />
            <p className="text-sm">No customers yet</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="px-6 py-3 text-left font-medium">Code</th>
                  <th className="px-6 py-3 text-left font-medium">Email</th>
                  <th className="px-6 py-3 text-left font-medium">Name</th>
                  <th className="px-6 py-3 text-left font-medium">Phone</th>
                  <th className="px-6 py-3 text-left font-medium">Created</th>
                  <th className="px-6 py-3 text-left font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {customers.map((c) => (
                  <tr key={c.id} className="border-b last:border-0 hover:bg-muted/50">
                    <td className="px-6 py-3 font-mono text-xs">{c.code}</td>
                    <td className="px-6 py-3">{c.email}</td>
                    <td className="px-6 py-3">{c.name || "—"}</td>
                    <td className="px-6 py-3">{c.phone || "—"}</td>
                    <td className="px-6 py-3">{new Date(c.createdAt).toLocaleDateString()}</td>
                    <td className="px-6 py-3">
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => openForm(c)}
                          disabled={working}
                          className="rounded-md border px-2 py-1 text-xs hover:bg-accent disabled:opacity-50"
                        >
                          <Edit className="h-3 w-3" />
                        </button>
                        <button
                          onClick={() => deleteCustomer(c.id)}
                          disabled={working}
                          className="rounded-md border border-destructive/50 px-2 py-1 text-xs text-destructive hover:bg-destructive/10 disabled:opacity-50"
                        >
                          <Trash2 className="h-3 w-3" />
                        </button>
                      </div>
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
