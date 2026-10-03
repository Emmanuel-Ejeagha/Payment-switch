import { describe, expect, it } from "vitest"
import { apiErrorCode, apiErrorMessage } from "@/lib/api-error"

describe("api-error helpers", () => {
  it("reads RFC 7807 problem details (title + errors array)", () => {
    const body = {
      title: "Email 'a@example.com' is already registered.",
      status: 409,
      errors: [{ code: "Identity.EmailAlreadyInUse", message: "Email 'a@example.com' is already registered." }],
    }
    expect(apiErrorMessage(body, "fallback")).toBe("Email 'a@example.com' is already registered.")
    expect(apiErrorCode(body)).toBe("Identity.EmailAlreadyInUse")
  })

  it("prefers the first error message, then title, detail, message", () => {
    expect(apiErrorMessage({ title: "T" }, "fallback")).toBe("T")
    expect(apiErrorMessage({ detail: "D" }, "fallback")).toBe("D")
    expect(apiErrorMessage({ message: "M" }, "fallback")).toBe("M")
    expect(apiErrorMessage({ errors: [{ message: "E" }], title: "T" }, "fallback")).toBe("E")
  })

  it("falls back on empty, blank, or unknown shapes", () => {
    expect(apiErrorMessage(null, "fallback")).toBe("fallback")
    expect(apiErrorMessage({}, "fallback")).toBe("fallback")
    expect(apiErrorMessage({ title: "  " }, "fallback")).toBe("fallback")
    expect(apiErrorMessage("", "fallback")).toBe("fallback")
    expect(apiErrorCode({})).toBeNull()
    expect(apiErrorCode(null)).toBeNull()
  })

  it("passes plain strings through", () => {
    expect(apiErrorMessage("plain failure", "fallback")).toBe("plain failure")
  })
})
