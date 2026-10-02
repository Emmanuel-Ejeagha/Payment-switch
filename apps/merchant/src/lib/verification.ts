/**
 * Shared helpers for the email-verification resend flow.
 *
 * The backend answers resend requests neutrally (200 for unknown and
 * already-verified addresses, so responses cannot be used to enumerate
 * accounts) and with 429 inside the per-address cooldown window.
 */

/** Default resend cooldown, in seconds (mirrors the API default). */
export const DEFAULT_RESEND_COOLDOWN_SECONDS = 60

/** Neutral confirmation shown after any accepted resend request. */
export const NEUTRAL_RESEND_MESSAGE =
  "If this address is registered and still unverified, a new verification email is on its way. Check your inbox."

/**
 * Extracts the retry delay from a 429 ProblemDetails message
 * (e.g. "Please wait 58 seconds before requesting another.").
 * Falls back when the server omits a number.
 */
export function parseRetryAfterSeconds(message: string | null | undefined, fallbackSeconds: number = DEFAULT_RESEND_COOLDOWN_SECONDS): number {
  if (!message) return fallbackSeconds
  const match = /(\d+)\s*seconds?/i.exec(message)
  if (!match) return fallbackSeconds
  const seconds = Number.parseInt(match[1]!, 10)
  return Number.isFinite(seconds) && seconds > 0 ? seconds : fallbackSeconds
}

/** Whole seconds remaining until `cooldownUntilMs` (never negative). */
export function cooldownSecondsLeft(cooldownUntilMs: number | null, nowMs: number = Date.now()): number {
  if (cooldownUntilMs === null) return 0
  return Math.max(0, Math.ceil((cooldownUntilMs - nowMs) / 1000))
}
