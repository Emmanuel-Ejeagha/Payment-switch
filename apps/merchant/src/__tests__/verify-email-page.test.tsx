import { render, screen, fireEvent, waitFor } from "@testing-library/react"
import { describe, expect, it, vi, afterEach } from "vitest"
import VerifyEmailPage from "@/app/(auth)/verify-email/page"

vi.mock("next/navigation", () => ({
  useSearchParams: () => ({
    get: (key: string) => ({ email: "u@example.com", token: "tok123" })[key] ?? null,
  }),
}))

function mockFetchOnce(status: number, body: unknown) {
  vi.stubGlobal(
    "fetch",
    vi.fn(async () => new Response(JSON.stringify(body), { status }))
  )
}

describe("Merchant VerifyEmailPage resend flow", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("disables resend with a countdown on 429", async () => {
    mockFetchOnce(400, { message: "This verification link has expired. Request a new one." })
    render(<VerifyEmailPage />)

    // Reach the error state with the resend form.
    const resendButton = await screen.findByRole("button", { name: /Resend verification email/i })
    expect(resendButton).toBeInTheDocument()

    mockFetchOnce(429, { message: "A verification email was sent recently. Please wait 58 seconds before requesting another." })
    fireEvent.click(resendButton)

    expect(await screen.findByText(/Please wait 58 seconds/i)).toBeInTheDocument()
    const coolingButton = await screen.findByRole("button", { name: /Resend available in \d+s/i })
    expect(coolingButton).toBeDisabled()
  })

  it("shows the neutral confirmation on accepted resend", async () => {
    mockFetchOnce(400, { message: "We could not verify this link. Please try again." })
    render(<VerifyEmailPage />)

    const resendButton = await screen.findByRole("button", { name: /Resend verification email/i })

    mockFetchOnce(200, {})
    fireEvent.click(resendButton)

    expect(await screen.findByText(/If this address is registered and still unverified/i)).toBeInTheDocument()
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /Resend verification email/i })).toBeEnabled()
    })
  })
})
