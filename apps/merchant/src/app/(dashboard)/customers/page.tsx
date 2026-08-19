"use client"

import { useEffect, useState, useCallback, useMemo } from "react"
import { Mail, Pencil, Plus, Search, Trash2, Users } from "lucide-react"
import { useMerchant } from "@/hooks/use-merchant"
import type { CustomerDto } from "@paymentswitch/shared"
import {
  Alert,
  Button,
  Card,
  CardHeader,
  EmptyState,
  Field,
  IdCell,
  Input,
  Modal,
  PageHeader,
  TableSkeleton,
  TableWrap,
  TBody,
  TD,
  TH,
  THead,
  TR,
  useConfirm,
} from "@/components/ui"
import { formatDate } from "@/lib/format"

export default function CustomersPage() {
  const { merchantId, loading: merchantLoading, error: merchantError } = useMerchant()
  const { confirm, dialog } = useConfirm()
  const [customers, setCustomers] = useState<CustomerDto[]>([])
  const [dataReady, setDataReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const [query, setQuery] = useState("")
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

  const deleteCustomer = async (customer: CustomerDto) => {
    if (!merchantId) return
    const confirmed = await confirm({
      title: "Delete customer",
      message: (
        <>
          <strong className="font-medium text-foreground">{customer.email}</strong> will be removed
          from your directory. Payments already taken from this customer are not affected.
        </>
      ),
      confirmLabel: "Delete customer",
      destructive: true,
    })
    if (!confirmed) return

    setWorking(true)
    setError(null)
    try {
      const res = await fetch(`/api/proxy/payment/api/v1/customers/${customer.id}?merchantId=${merchantId}`, { method: "DELETE" })
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

  const visible = useMemo(() => {
    const needle = query.trim().toLowerCase()
    if (!needle) return customers
    return customers.filter((c) =>
      [c.email, c.name, c.phone, c.code].some((v) => v?.toLowerCase().includes(needle)),
    )
  }, [customers, query])

  return (
    <div className="space-y-8">
      <PageHeader
        title="Customers"
        description="Your customer directory. Customers are reusable across subscriptions, so create one before starting a recurring plan."
        actions={
          <Button variant="primary" icon={Plus} onClick={() => openForm()} disabled={!merchantId}>
            New customer
          </Button>
        }
      />

      {(error || merchantError) && (
        <Alert variant="error" title="Something went wrong">
          {error || merchantError}
        </Alert>
      )}

      <Card className="overflow-hidden">
        <CardHeader
          title="All customers"
          description={loading ? "Loading…" : `${customers.length} total`}
          icon={Users}
          action={
            customers.length > 0 ? (
              <div className="relative w-full sm:w-64">
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
                  aria-hidden="true"
                />
                <Input
                  type="search"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Search customers"
                  aria-label="Search customers"
                  className="h-9 pl-9"
                />
              </div>
            ) : undefined
          }
        />
        {loading ? (
          <TableSkeleton rows={5} columns={6} />
        ) : customers.length === 0 ? (
          <EmptyState
            icon={Users}
            title="No customers yet"
            description="Add a customer to keep their contact details on file and bill them on a recurring plan."
            action={
              <Button variant="primary" icon={Plus} onClick={() => openForm()} disabled={!merchantId}>
                Add your first customer
              </Button>
            }
          />
        ) : visible.length === 0 ? (
          <EmptyState
            icon={Search}
            title="No matches"
            description={`Nothing in your directory matches “${query}”.`}
            action={<Button onClick={() => setQuery("")}>Clear search</Button>}
          />
        ) : (
          <TableWrap>
            <THead>
              <TH>Code</TH>
              <TH>Customer</TH>
              <TH>Phone</TH>
              <TH>Created</TH>
              <TH align="right">Actions</TH>
            </THead>
            <TBody>
              {visible.map((c) => (
                <TR key={c.id}>
                  <TD>
                    <IdCell>{c.code}</IdCell>
                  </TD>
                  <TD>
                    <p className="font-medium">{c.name || "—"}</p>
                    <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <Mail className="h-3 w-3 shrink-0" aria-hidden="true" />
                      {c.email}
                    </p>
                  </TD>
                  <TD className="whitespace-nowrap text-muted-foreground">{c.phone || "—"}</TD>
                  <TD className="whitespace-nowrap text-xs text-muted-foreground">
                    {formatDate(c.createdAt)}
                  </TD>
                  <TD align="right">
                    <div className="flex items-center justify-end gap-1.5">
                      <Button
                        size="sm"
                        icon={Pencil}
                        iconOnly
                        disabled={working}
                        onClick={() => openForm(c)}
                      >
                        Edit {c.email}
                      </Button>
                      <Button
                        size="sm"
                        variant="dangerGhost"
                        icon={Trash2}
                        iconOnly
                        disabled={working}
                        onClick={() => deleteCustomer(c)}
                      >
                        Delete {c.email}
                      </Button>
                    </div>
                  </TD>
                </TR>
              ))}
            </TBody>
          </TableWrap>
        )}
      </Card>

      {showForm && (
        <Modal
          title={editingId ? "Edit customer" : "New customer"}
          description={
            editingId
              ? "Changes apply to future invoices and receipts."
              : "Only an email address is required — everything else can be filled in later."
          }
          onClose={closeForm}
          size="lg"
          footer={
            <>
              <Button onClick={closeForm}>Cancel</Button>
              <Button variant="primary" onClick={save} pending={working} disabled={!email}>
                {working ? "Saving…" : editingId ? "Save changes" : "Create customer"}
              </Button>
            </>
          }
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Email" htmlFor="customer-email" required className="sm:col-span-2">
              <Input
                id="customer-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="customer@example.com"
              />
            </Field>
            <Field label="Name" htmlFor="customer-name">
              <Input
                id="customer-name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Jane Okafor"
              />
            </Field>
            <Field label="Phone" htmlFor="customer-phone">
              <Input
                id="customer-phone"
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="+234 800 000 0000"
              />
            </Field>
            <Field
              label="Description"
              htmlFor="customer-description"
              hint="An internal note. Customers never see this."
              className="sm:col-span-2"
            >
              <Input
                id="customer-description"
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Enterprise account, billed annually"
              />
            </Field>
          </div>
        </Modal>
      )}

      {dialog}
    </div>
  )
}
