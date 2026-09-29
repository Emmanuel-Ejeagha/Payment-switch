import { describe, expect, it } from "vitest"
import { luhnValid, isExpiryValid } from "@/app/checkout/[code]/page"

describe("checkout card validation", () => {
  it("validates Luhn checksum", () => {
    expect(luhnValid("4242424242424242")).toBe(true)
    expect(luhnValid("4242 4242 4242 4242")).toBe(true)
    expect(luhnValid("4000000000000002")).toBe(true)
    expect(luhnValid("4242424242424241")).toBe(false)
    expect(luhnValid("1234567890123")).toBe(false)
  })

  it("rejects wrong length", () => {
    expect(luhnValid("4242")).toBe(false)
    expect(luhnValid("42424242424242424242")).toBe(false) // 20 digits
    expect(luhnValid("")).toBe(false)
  })

  it("validates expiry not in past", () => {
    const now = new Date()
    const futureYear = now.getFullYear() + 2
    expect(isExpiryValid(12, futureYear)).toBe(true)
    expect(isExpiryValid(now.getMonth() + 1, now.getFullYear())).toBe(true)
    expect(isExpiryValid(1, 2000)).toBe(false)
    expect(isExpiryValid(13, 2030)).toBe(false)
    expect(isExpiryValid(0, 2030)).toBe(false)
  })

  it("handles two-digit year", () => {
    const now = new Date()
    const currentYear = now.getFullYear()
    const futureTwoDigit = (currentYear + 1) % 100
    expect(isExpiryValid(12, futureTwoDigit)).toBe(true)
    expect(isExpiryValid(1, 10)).toBe(false) // 2010 is past
  })
})

describe("payment-link amount guard", () => {
  function parseAmountGuard(amount: string): number | null {
    const parsed = parseFloat(amount)
    if (!Number.isFinite(parsed) || parsed <= 0) return null
    const minor = Math.round(parsed * 100)
    if (!Number.isFinite(minor) || minor <= 0) return null
    return minor
  }

  it("rejects empty, zero, negative, NaN, Infinity", () => {
    expect(parseAmountGuard("")).toBeNull()
    expect(parseAmountGuard("0")).toBeNull()
    expect(parseAmountGuard("-5")).toBeNull()
    expect(parseAmountGuard("NaN")).toBeNull()
    expect(parseAmountGuard("Infinity")).toBeNull()
    expect(parseAmountGuard("abc")).toBeNull()
  })

  it("accepts positive finite amounts", () => {
    expect(parseAmountGuard("100")).toBe(10000)
    expect(parseAmountGuard("100.00")).toBe(10000)
    expect(parseAmountGuard("0.01")).toBe(1)
    expect(parseAmountGuard("10.5")).toBe(1050)
  })
})
