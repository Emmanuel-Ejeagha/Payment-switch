import { render, screen } from "@testing-library/react"
import { afterEach, describe, expect, it, vi } from "vitest"
import type { MerchantDto } from "@paymentswitch/shared"
import PaymentsPage from "@/app/(dashboard)/payments/page"
import * as useMerchantModule from "@/hooks/use-merchant"

vi.mock("@/hooks/use-merchant", () => ({
  useMerchant: vi.fn(),
}))
vi.mock("@/components/toast", () => ({
  useToast: () => ({ showToast: vi.fn() }),
}))

const mockedUseMerchant = vi.mocked(useMerchantModule.useMerchant)

function mockSignedInMerchant() {
  mockedUseMerchant.mockReturnValue({
    user: null,
    merchant: { id: "m-1" } as MerchantDto,
    merchantId: "m-1",
    loading: false,
    error: null,
  })
}

describe("PaymentsPage list loading", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("shows an error panel when the payments request fails", async () => {
    mockSignedInMerchant()
    vi.stubGlobal("fetch", vi.fn(async () => {
      throw new Error("network down")
    }))

    render(<PaymentsPage />)

    expect(await screen.findByRole("alert")).toHaveTextContent("Failed to load payments")
    expect(screen.queryByText("No payments yet")).not.toBeInTheDocument()
  })

  it("shows an error panel on a non-ok response", async () => {
    mockSignedInMerchant()
    vi.stubGlobal("fetch", vi.fn(async () => new Response(null, { status: 500 })))

    render(<PaymentsPage />)

    const alert = await screen.findByRole("alert")
    expect(alert).toHaveTextContent("Failed to load payments")
    expect(alert).toHaveTextContent("500")
  })

  it("shows the empty state when the request succeeds with no payments", async () => {
    mockSignedInMerchant()
    vi.stubGlobal("fetch", vi.fn(async () => Response.json([])))

    render(<PaymentsPage />)

    expect(await screen.findByText("No payments yet")).toBeInTheDocument()
    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
  })
})
