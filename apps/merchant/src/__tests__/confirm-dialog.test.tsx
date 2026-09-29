import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { describe, expect, it } from "vitest"
import { useConfirm } from "@/components/ui/confirm-dialog"

describe("useConfirm", () => {
  it("resolves true when the user confirms", async () => {
    let result: boolean | undefined
    function Harness() {
      const { confirm, dialog } = useConfirm()
      return (
        <div>
          <button
            onClick={() =>
              void confirm({ title: "Delete customer?", message: "This removes their data." }).then(
                (r) => (result = r),
              )
            }
          >
            ask
          </button>
          {dialog}
        </div>
      )
    }

    render(<Harness />)
    await userEvent.click(screen.getByRole("button", { name: "ask" }))

    expect(screen.getByRole("dialog")).toBeInTheDocument()

    await userEvent.click(screen.getByRole("button", { name: "Confirm" }))

    expect(result).toBe(true)
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
  })

  it("resolves false when cancelled", async () => {
    let result: boolean | undefined
    function Harness() {
      const { confirm, dialog } = useConfirm()
      return (
        <div>
          <button
            onClick={() =>
              void confirm({ title: "Archive plan?", message: "It stays archived." }).then(
                (r) => (result = r),
              )
            }
          >
            ask
          </button>
          {dialog}
        </div>
      )
    }

    render(<Harness />)
    await userEvent.click(screen.getByRole("button", { name: "ask" }))

    await userEvent.click(screen.getByRole("button", { name: "Cancel" }))

    expect(result).toBe(false)
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
  })
})