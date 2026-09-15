import { describe, expect, it, vi, beforeEach, afterEach } from "vitest"
import { render, screen, fireEvent, act, waitFor } from "@testing-library/react"
import MerchantsPage from "@/app/(dashboard)/merchants/page"

describe("MerchantsPage search", () => {
  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it("debounces search input and sends search param", async () => {
    vi.useFakeTimers()
    const fetchMock = vi.fn(async () => {
      return new Response(JSON.stringify([]), {
        status: 200,
        headers: { "content-type": "application/json", "x-total-count": "0" },
      })
    })
    vi.stubGlobal("fetch", fetchMock)

    render(<MerchantsPage />)

    // wait for initial load
    await act(async () => {
      vi.advanceTimersByTime(10)
      await Promise.resolve()
    })
    expect(fetchMock).toHaveBeenCalled()
    const firstUrl = fetchMock.mock.calls[0][0] as string
    expect(firstUrl).not.toContain("search=")

    fetchMock.mockClear()
    const input = screen.getByPlaceholderText("Search merchants...")
    fireEvent.change(input, { target: { value: "acme" } })

    // should not fetch immediately
    expect(fetchMock).not.toHaveBeenCalled()

    await act(async () => {
      vi.advanceTimersByTime(300)
      await Promise.resolve()
    })
    await act(async () => {
      await Promise.resolve()
    })
    expect(fetchMock).toHaveBeenCalled()
    const url = fetchMock.mock.calls[0][0] as string
    expect(url).toContain("search=acme")
  })

  it("reads x-total-count for pagination total", async () => {
    const merchants = [
      { id: "1", businessName: "A", email: "a@example.com", status: "Active", createdAt: new Date().toISOString() },
      { id: "2", businessName: "B", email: "b@example.com", status: "Pending", createdAt: new Date().toISOString() },
    ]
    const fetchMock = vi.fn(async () => {
      return new Response(JSON.stringify(merchants), {
        status: 200,
        headers: { "content-type": "application/json", "x-total-count": "42" },
      })
    })
    vi.stubGlobal("fetch", fetchMock)

    render(<MerchantsPage />)

    await waitFor(() => expect(fetchMock).toHaveBeenCalled())
    await waitFor(() => expect(screen.getByText(/of 42/)).toBeInTheDocument())
  })
})
