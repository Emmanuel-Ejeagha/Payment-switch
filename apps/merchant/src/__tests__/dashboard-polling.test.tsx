import { describe, expect, it, vi } from "vitest"
import fs from "fs"

describe("dashboard polling visibility guard", () => {
  it("checks document.hidden before fetching", () => {
    const content = fs.readFileSync("src/app/(dashboard)/dashboard/page.tsx", "utf-8")
    expect(content).toContain("document.hidden")
    expect(content).toContain("visibilitychange")
  })

  it("surfaces poll errors", () => {
    const content = fs.readFileSync("src/app/(dashboard)/dashboard/page.tsx", "utf-8")
    expect(content).toContain("pollError")
    expect(content).toContain("Background refresh failed")
  })
})

describe("payment-links origin effect", () => {
  it("sets origin in useEffect not during render", () => {
    const content = fs.readFileSync("src/app/(dashboard)/payment-links/page.tsx", "utf-8")
    expect(content).toContain("useEffect(() => {")
    expect(content).toContain("setOrigin(window.location.origin)")
    expect(content).not.toContain('if (origin === "" && typeof window !== "undefined")')
  })
})

describe("auth artwork lazy", () => {
  it("does not use priority on hidden artwork", () => {
    const content = fs.readFileSync("src/components/auth/auth-shell.tsx", "utf-8")
    expect(content).not.toContain("priority")
    expect(content).toContain('loading="lazy"')
  })
})
