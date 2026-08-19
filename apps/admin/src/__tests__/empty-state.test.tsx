import { render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { EmptyState } from "@/components/empty-state"

describe("EmptyState", () => {
  it("shows custom title and description", () => {
    render(<EmptyState title="No merchants" description="Onboard one first" />)

    expect(screen.getByText("No merchants")).toBeInTheDocument()
    expect(screen.getByText("Onboard one first")).toBeInTheDocument()
  })

  it("falls back to sensible defaults", () => {
    render(<EmptyState />)

    expect(screen.getByText("No data")).toBeInTheDocument()
    expect(screen.getByText("Nothing to show yet")).toBeInTheDocument()
  })
})