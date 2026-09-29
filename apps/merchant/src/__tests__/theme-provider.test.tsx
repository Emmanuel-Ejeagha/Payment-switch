import { describe, expect, it, vi, afterEach } from "vitest"
import { render, screen, fireEvent } from "@testing-library/react"
import { ThemeProvider, useTheme } from "@/components/theme-provider"

function Probe() {
  const { theme, toggle } = useTheme()
  return <button onClick={toggle}>current:{theme}</button>
}

describe("ThemeProvider default theme", () => {
  afterEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove("dark")
    vi.unstubAllGlobals()
  })

  it("defaults to light even when the OS prefers dark", () => {
    // The OS color-scheme preference must not leak into the default theme.
    vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: true })))
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>
    )
    expect(screen.getByText("current:light")).toBeInTheDocument()
    expect(document.documentElement.classList.contains("dark")).toBe(false)
  })

  it("honors an explicitly stored dark theme and toggles back to light", () => {
    localStorage.setItem("theme", "dark")
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>
    )
    expect(screen.getByText("current:dark")).toBeInTheDocument()
    expect(document.documentElement.classList.contains("dark")).toBe(true)
    fireEvent.click(screen.getByRole("button"))
    expect(screen.getByText("current:light")).toBeInTheDocument()
    expect(localStorage.getItem("theme")).toBe("light")
  })
})
