import { render, screen, fireEvent, waitFor } from "@testing-library/react"
import { describe, expect, it, vi, afterEach } from "vitest"
import OnboardingPage from "@/app/(dashboard)/onboarding/page"

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
}))

function mockApi() {
  return vi.fn(async (url: string | URL | Request) => {
    const target = typeof url === "string" ? url : url instanceof URL ? url.href : url.url
    if (target.endsWith("/users/me")) {
      return Response.json({ id: "user-1", email: "taken@example.com", fullName: "Acme Ltd" })
    }
    if (target.includes("/merchants/by-email/")) {
      return Response.json({ message: "Not found" }, { status: 404 })
    }
    if (target.endsWith("/merchants") || target.endsWith("/api/v1/merchants")) {
      return Response.json(
        {
          title: "Please verify your email address before proceeding.",
          status: 403,
          errors: [{ code: "Identity.EmailNotVerified", message: "Please verify your email address before proceeding." }],
        },
        { status: 403 }
      )
    }
    if (target.includes("/resend-verification")) {
      return Response.json({}, { status: 200 })
    }
    throw new Error(`unexpected fetch: ${target}`)
  })
}

describe("Merchant OnboardingPage unverified gate", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("explains verification is required and offers an inline resend", async () => {
    vi.stubGlobal("fetch", mockApi())

    render(<OnboardingPage />)

    // Wait for the form (user loaded, no merchant yet).
    const createButton = await screen.findByRole("button", { name: /Create merchant profile/i })
    fireEvent.click(createButton)

    expect(await screen.findByText(/verify your email address first/i)).toBeInTheDocument()
    expect(screen.queryByText("Onboarding failed")).not.toBeInTheDocument()

    const resendButton = await screen.findByRole("button", { name: /Resend verification email/i })
    fireEvent.click(resendButton)

    expect(await screen.findByText(/If this address is registered and still unverified/i)).toBeInTheDocument()
  })
})
