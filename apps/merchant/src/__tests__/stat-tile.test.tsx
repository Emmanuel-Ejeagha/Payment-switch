import { render, screen } from "@testing-library/react"
import { Wallet } from "lucide-react"
import { describe, expect, it } from "vitest"
import { StatTile } from "@/components/ui/stat-tile"

describe("StatTile", () => {
  it("renders label, value, unit, and hint", () => {
    render(
      <StatTile
        label="Available balance"
        value="1,250.00"
        unit="USD"
        hint="Settles next Monday"
        icon={Wallet}
      />,
    )

    expect(screen.getByText("Available balance")).toBeInTheDocument()
    expect(screen.getByText("1,250.00")).toBeInTheDocument()
    expect(screen.getByText("USD")).toBeInTheDocument()
    expect(screen.getByText("Settles next Monday")).toBeInTheDocument()
  })
})