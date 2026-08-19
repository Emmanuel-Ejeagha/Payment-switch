import { describe, expect, it } from "vitest"
import { toneForStatus } from "@/components/ui/badge"

describe("toneForStatus", () => {
  it("maps the new merchant states to semantic tones", () => {
    expect(toneForStatus("Approved")).toBe("info")
    expect(toneForStatus("Rejected")).toBe("danger")
    expect(toneForStatus("Suspended")).toBe("danger")
    expect(toneForStatus("Pending")).toBe("warning")
    expect(toneForStatus("Active")).toBe("success")
  })

  it("is case-insensitive and ignores separators", () => {
    expect(toneForStatus("approved")).toBe("info")
    expect(toneForStatus("REJECTED")).toBe("danger")
    expect(toneForStatus(" Active ")).toBe("success")
  })

  it("falls back to neutral for unknown statuses", () => {
    expect(toneForStatus("whatever")).toBe("neutral")
  })
})