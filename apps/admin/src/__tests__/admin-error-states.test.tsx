import { render, screen, waitFor, fireEvent } from "@testing-library/react"
import { describe, expect, it, vi, afterEach } from "vitest"
import LedgerPage from "@/app/(dashboard)/ledger/page"
import SettlementsPage from "@/app/(dashboard)/settlements/page"
import NotificationsPage from "@/app/(dashboard)/notifications/page"

describe("Admin ledger/settlements/notifications error states", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("ledger shows error with retry when fetch throws", async () => {
    vi.stubGlobal("fetch", vi.fn(async (url: string) => {
      if (url.includes("/merchants")) {
        return Response.json([{ id: "m1", businessName: "Test", email: "a@b.com", status: "Active", createdAt: new Date().toISOString() }])
      }
      throw new Error("network down")
    }))

    render(<LedgerPage />)
    const alert = await screen.findByRole("alert")
    expect(alert).toHaveTextContent("Could not load ledger data")
    expect(alert).toHaveTextContent("network down")

    // retry should re-call fetch
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockClear()
    fetchMock.mockResolvedValueOnce(Response.json([{ id: "m1", businessName: "Test", email: "a@b.com", status: "Active", createdAt: new Date().toISOString() }]))
    // second call for ledger balances/transactions - mock success
    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify([{ available: 100, pending: 0, reserved: 0, currency: "USD" }]), { status: 200, headers: { "content-type": "application/json" } }))

    const retry = screen.getByRole("button", { name: /Retry/i })
    fireEvent.click(retry)
    // after retry, fetch should have been called again
    await waitFor(() => expect(fetchMock).toHaveBeenCalled())
  })

  it("ledger shows empty state not error when no data", async () => {
    vi.stubGlobal("fetch", vi.fn(async (url: string) => {
      if (url.includes("/merchants")) {
        return Response.json([{ id: "m1", businessName: "Test", email: "a@b.com", status: "Active", createdAt: new Date().toISOString() }])
      }
      if (url.includes("/balances")) {
        return Response.json([])
      }
      if (url.includes("/transactions")) {
        return Response.json([])
      }
      return new Response(null, { status: 404 })
    }))

    render(<LedgerPage />)
    expect(await screen.findByText("No ledger activity yet")).toBeInTheDocument()
    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
  })

  it("settlements shows error with retry on failure", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => {
      throw new Error("network down")
    }))

    render(<SettlementsPage />)
    const alert = await screen.findByRole("alert")
    expect(alert).toHaveTextContent("Failed to load settlements")
    expect(screen.getByRole("button", { name: /Retry/i })).toBeInTheDocument()
  })

  it("settlements shows empty when no batches", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => Response.json([])))

    render(<SettlementsPage />)
    expect(await screen.findByText("No settlement batches")).toBeInTheDocument()
    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
  })

  it("notifications shows error with retry on failure", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => {
      throw new Error("network down")
    }))

    render(<NotificationsPage />)
    const alert = await screen.findByRole("alert")
    expect(alert).toHaveTextContent("Failed to load notifications")
  })

  it("notifications shows empty when no data", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => Response.json([])))

    render(<NotificationsPage />)
    expect(await screen.findByText("No notifications")).toBeInTheDocument()
    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
  })
})
