import { describe, expect, it } from "vitest"
import {
  DEFAULT_RESEND_COOLDOWN_SECONDS,
  NEUTRAL_RESEND_MESSAGE,
  cooldownSecondsLeft,
  parseRetryAfterSeconds,
} from "@/lib/verification"

describe("verification resend helpers", () => {
  it("parses the retry delay from a 429 message", () => {
    expect(parseRetryAfterSeconds("A verification email was sent recently. Please wait 58 seconds before requesting another.")).toBe(58)
    expect(parseRetryAfterSeconds("Please wait 1 second.")).toBe(1)
  })

  it("falls back to the default cooldown when no number is present", () => {
    expect(parseRetryAfterSeconds("Too many requests.")).toBe(DEFAULT_RESEND_COOLDOWN_SECONDS)
    expect(parseRetryAfterSeconds(null)).toBe(DEFAULT_RESEND_COOLDOWN_SECONDS)
    expect(parseRetryAfterSeconds(undefined)).toBe(DEFAULT_RESEND_COOLDOWN_SECONDS)
    expect(parseRetryAfterSeconds("wait 0 seconds")).toBe(DEFAULT_RESEND_COOLDOWN_SECONDS)
  })

  it("computes whole seconds left, never negative", () => {
    const now = 1_700_000_000_000
    expect(cooldownSecondsLeft(now + 58_400, now)).toBe(59)
    expect(cooldownSecondsLeft(now, now)).toBe(0)
    expect(cooldownSecondsLeft(now - 1_000, now)).toBe(0)
    expect(cooldownSecondsLeft(null, now)).toBe(0)
  })

  it("uses neutral copy that discloses nothing about the address", () => {
    expect(NEUTRAL_RESEND_MESSAGE).toContain("If this address is registered")
    expect(NEUTRAL_RESEND_MESSAGE).not.toMatch(/already verified|unknown|not found/i)
  })
})
