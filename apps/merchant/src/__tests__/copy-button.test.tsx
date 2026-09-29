import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, describe, expect, it, vi } from "vitest"
import { CopyButton } from "@/components/ui/copy-button"

describe("CopyButton", () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it("copies the value via the clipboard API and confirms", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined)
    Object.defineProperty(navigator, "clipboard", {
      value: { writeText },
      configurable: true,
    })

    render(<CopyButton value="sk_test_abc" />)

    await userEvent.click(screen.getByRole("button", { name: "Copy" }))

    expect(writeText).toHaveBeenCalledWith("sk_test_abc")
    expect(await screen.findByText("Copied")).toBeInTheDocument()
  })

  it("falls back to execCommand when the clipboard API is unavailable", async () => {
    Object.defineProperty(navigator, "clipboard", {
      value: undefined,
      configurable: true,
    })
    // jsdom 30 removed execCommand entirely; re-add a working stub so the
    // component's secure-context fallback can be exercised.
    Object.defineProperty(document, "execCommand", {
      value: vi.fn().mockReturnValue(true),
      configurable: true,
    })

    render(<CopyButton value="secret" />)

    await userEvent.click(screen.getByRole("button", { name: "Copy" }))

    expect(document.execCommand).toHaveBeenCalledWith("copy")
    expect(await screen.findByText("Copied")).toBeInTheDocument()
  })
})