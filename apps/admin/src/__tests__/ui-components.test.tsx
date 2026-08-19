import { render, screen } from "@testing-library/react"
import { DollarSign } from "lucide-react"
import { describe, expect, it } from "vitest"
import { StatsCard, StatusBadge } from "@paymentswitch/ui"

describe("StatsCard", () => {
  it("renders title, value, and description", () => {
    render(<StatsCard title="Volume" value="42" description="Last 30 days" icon={DollarSign} />)

    expect(screen.getByText("Volume")).toBeInTheDocument()
    expect(screen.getByText("42")).toBeInTheDocument()
    expect(screen.getByText("Last 30 days")).toBeInTheDocument()
  })
})

describe("StatusBadge", () => {
  it("renders the status text", () => {
    render(<StatusBadge status="Active" />)

    expect(screen.getByText("Active")).toBeInTheDocument()
  })
})