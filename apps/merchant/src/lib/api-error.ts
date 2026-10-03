/**
 * Reads a human-friendly message out of API error responses.
 *
 * The backend returns RFC 7807 problem details (`{ title, status, errors[] }`
 * with camel-cased names), while older paths may return `{ message }` or
 * `{ detail }`. This helper checks every known shape so pages never fall back
 * to a generic message when the server actually explained itself.
 */

export function apiErrorCode(body: unknown): string | null {
  if (body && typeof body === "object") {
    const errors = (body as Record<string, unknown>).errors
    if (Array.isArray(errors) && errors.length > 0) {
      const code = (errors[0] as Record<string, unknown> | null)?.code
      if (typeof code === "string" && code.trim()) return code
    }
  }
  return null
}

export function apiErrorMessage(body: unknown, fallback: string): string {
  if (typeof body === "string") return body.trim() || fallback
  if (body && typeof body === "object") {
    const record = body as Record<string, unknown>
    const errors = record.errors
    const first = Array.isArray(errors) ? (errors[0] as Record<string, unknown> | null) : null
    const candidates = [first?.message, record.title, record.detail, record.message]
    for (const candidate of candidates) {
      if (typeof candidate === "string" && candidate.trim()) return candidate
    }
  }
  return fallback
}
