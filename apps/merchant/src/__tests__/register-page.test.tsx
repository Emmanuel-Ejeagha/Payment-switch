import { render, screen, fireEvent, waitFor } from "@testing-library/react"
import { describe, expect, it, vi, afterEach } from "vitest"
import RegisterPage from "@/app/(auth)/register/page"

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() }),
}))

describe("Merchant RegisterPage duplicate email", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("explains that the email is already registered instead of a generic failure", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () =>
        Response.json(
          {
            title: "Email 'taken@example.com' is already registered.",
            status: 409,
            errors: [{ code: "Identity.EmailAlreadyInUse", message: "Email 'taken@example.com' is already registered." }],
          },
          { status: 409 }
        )
      )
    )

    render(<RegisterPage />)
    fireEvent.change(screen.getByPlaceholderText("Your Business Ltd."), { target: { value: "Acme Ltd" } })
    fireEvent.change(screen.getByPlaceholderText("name@example.com"), { target: { value: "taken@example.com" } })
    fireEvent.change(screen.getByPlaceholderText("Min. 10 characters"), { target: { value: "Password123" } })
    fireEvent.change(screen.getByPlaceholderText("Repeat your password"), { target: { value: "Password123" } })
    fireEvent.click(screen.getByRole("button", { name: /Create account/i }))

    expect(await screen.findByText(/already exists/i)).toBeInTheDocument()
    expect(screen.queryByText("Registration failed")).not.toBeInTheDocument()
  })

  it("offers an inline resend after a duplicate registration", async () => {
    const fetchMock = vi.fn(async (url: string | URL | Request) => {
      const target = typeof url === "string" ? url : url instanceof URL ? url.href : url.url
      if (target.includes("/auth/register")) {
        return Response.json(
          {
            title: "Email 'taken@example.com' is already registered.",
            status: 409,
            errors: [{ code: "Identity.EmailAlreadyInUse", message: "Email 'taken@example.com' is already registered." }],
          },
          { status: 409 }
        )
      }
      return Response.json({}, { status: 200 })
    })
    vi.stubGlobal("fetch", fetchMock)

    render(<RegisterPage />)
    fireEvent.change(screen.getByPlaceholderText("Your Business Ltd."), { target: { value: "Acme Ltd" } })
    fireEvent.change(screen.getByPlaceholderText("name@example.com"), { target: { value: "taken@example.com" } })
    fireEvent.change(screen.getByPlaceholderText("Min. 10 characters"), { target: { value: "Password123" } })
    fireEvent.change(screen.getByPlaceholderText("Repeat your password"), { target: { value: "Password123" } })
    fireEvent.click(screen.getByRole("button", { name: /Create account/i }))

    // The duplicate error offers the resend flow for the same address.
    const resendButton = await screen.findByRole("button", { name: /Resend verification email/i })
    expect(resendButton).toBeInTheDocument()

    fireEvent.click(resendButton)
    expect(await screen.findByText(/If this address is registered and still unverified/i)).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
