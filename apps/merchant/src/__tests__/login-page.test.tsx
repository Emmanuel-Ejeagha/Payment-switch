import { render, screen, fireEvent, waitFor } from "@testing-library/react"
import { describe, expect, it, vi, afterEach } from "vitest"
import LoginPage from "@/app/(auth)/login/page"

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() }),
}))

describe("Merchant LoginPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("shows network error when fetch throws", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => {
      throw new Error("network down")
    }))

    render(<LoginPage />)
    fireEvent.change(screen.getByPlaceholderText("name@example.com"), { target: { value: "a@example.com" } })
    fireEvent.change(screen.getByPlaceholderText("Enter your password"), { target: { value: "secret123" } })
    fireEvent.click(screen.getByRole("button", { name: /Sign in/i }))

    expect(await screen.findByText(/Network error/i)).toBeInTheDocument()
  })
})
