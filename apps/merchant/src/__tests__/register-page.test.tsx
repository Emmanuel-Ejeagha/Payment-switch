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

    expect(await screen.findByText(/already exists.*signing in instead/i)).toBeInTheDocument()
    expect(screen.queryByText("Registration failed")).not.toBeInTheDocument()
  })
})
