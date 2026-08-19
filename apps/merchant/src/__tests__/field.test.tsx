import { render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { AmountInput, Field, Input } from "@/components/ui/field"

describe("Field", () => {
  it("renders the label, honours required, and shows the error over the hint", () => {
    render(
      <Field label="Amount" htmlFor="amount" required hint="In USD" error="Too high">
        <Input id="amount" />
      </Field>,
    )

    expect(screen.getByLabelText(/Amount/)).toBeInTheDocument()
    expect(screen.getByText("Too high")).toBeInTheDocument()
    expect(screen.queryByText("In USD")).not.toBeInTheDocument()
  })

  it("shows the hint when there is no error", () => {
    render(
      <Field label="Amount" htmlFor="amount" hint="In USD">
        <Input id="amount" />
      </Field>,
    )

    expect(screen.getByText("In USD")).toBeInTheDocument()
    expect(screen.queryByText("Too high")).not.toBeInTheDocument()
  })
})

describe("AmountInput", () => {
  it("pins the currency code inside the control", () => {
    render(<AmountInput currency="USD" />)

    expect(screen.getByText("USD")).toBeInTheDocument()
    expect(screen.getByRole("spinbutton")).toHaveAttribute("inputMode", "decimal")
  })
})