import { describe, expect, it, vi, afterEach } from "vitest"
import { render, screen, waitFor } from "@testing-library/react"
import DashboardPage from "@/app/(dashboard)/page"

describe("Dashboard totals", () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it("uses x-total-count for total merchants, not sample length", async () => {
    const merchants = Array.from({ length: 5 }, (_, i) => ({
      id: `m${i}`,
      businessName: `Biz ${i}`,
      email: `biz${i}@example.com`,
      status: i < 2 ? "Active" : "Pending",
      createdAt: new Date().toISOString(),
    }))

    const fetchMock = vi.fn(async (url: string) => {
      if (url.includes("/users/me")) {
        return new Response(JSON.stringify({ fullName: "Admin User" }), {
          status: 200,
          headers: { "content-type": "application/json" },
        })
      }
      if (url.includes("/merchants")) {
        return new Response(JSON.stringify(merchants), {
          status: 200,
          headers: { "content-type": "application/json", "x-total-count": "42" },
        })
      }
      return new Response("{}", { status: 200 })
    })
    vi.stubGlobal("fetch", fetchMock)

    render(<DashboardPage />)

    await waitFor(() => expect(screen.getByText("Total Merchants")).toBeInTheDocument())
    await waitFor(() => expect(screen.getByText("42")).toBeInTheDocument())
    // total should be 42 from header, not 5 from sample
    expect(screen.getByText("42")).toBeInTheDocument()
  })
})
