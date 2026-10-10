import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { describe, expect, it, vi } from "vitest"
import { useState } from "react"
import { Modal } from "@/components/ui/modal"

// jsdom has no layout (offsetParent is always null), which would neuter the
// dialog's visibility filter and hide the bug. Pretend everything is visible.
Object.defineProperty(HTMLElement.prototype, "offsetParent", {
  configurable: true,
  get() {
    return this.parentNode instanceof HTMLElement ? this.parentNode : null
  },
})

// Regression: page-level form state rerenders the parent on every keystroke,
// redefining inline onClose. The dialog must focus exactly once on mount —
// never yank focus back to the first element mid-typing.
function TypingHarness() {
  const [text, setText] = useState("")
  return (
    <Modal
      title="New customer"
      onClose={() => {}}
      footer={<button type="button">Save</button>}
    >
      <label htmlFor="notes">Notes</label>
      <textarea
        id="notes"
        value={text}
        onChange={(e) => setText(e.target.value)}
      />
    </Modal>
  )
}

describe("Modal focus management", () => {
  it("keeps focus in the textarea while typing across parent rerenders", async () => {
    const user = userEvent.setup()
    render(<TypingHarness />)

    const box = screen.getByLabelText("Notes")
    await user.click(box)
    await user.keyboard("Enterprise account, billed annually")

    expect(box).toHaveValue("Enterprise account, billed annually")
    expect(document.activeElement).toBe(box)
  })

  it("closes on Escape with the latest onClose", async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(
      <Modal title="T" onClose={onClose}>
        <button type="button">Save</button>
      </Modal>
    )

    await user.keyboard("{Escape}")
    expect(onClose).toHaveBeenCalledTimes(1)
  })
})
