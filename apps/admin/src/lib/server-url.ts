/**
 * Server-side URL + cookie helpers for Next.js route handlers.
 *
 * Browsers reach the stack through nginx (`NEXT_PUBLIC_API_URL`), but route
 * handlers run *inside* containers where `localhost` means themselves. Use
 * `serverApiUrl()` for every server-to-API fetch so Docker, dev servers, and
 * tests all resolve correctly:
 * - `INTERNAL_API_URL` when set (compose points it at nginx),
 * - otherwise `NEXT_PUBLIC_API_URL` (local `next dev`, tests).
 */

export function serverApiUrl(path: string): string {
  const base = process.env.INTERNAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? ""
  return `${base}${path}`
}

/**
 * Whether auth cookies must carry the `Secure` flag. Derives from the actual
 * request scheme (`X-Forwarded-Proto` set by nginx) instead of the build-time
 * `NODE_ENV`: compose images bake `NODE_ENV=production` yet serve plain HTTP
 * locally, and `Secure` cookies over HTTP are silently dropped by browsers
 * (login looks successful, then every authenticated call 401s).
 */
export function isSecureRequest(request: { headers: Headers }): boolean {
  const proto = request.headers.get("x-forwarded-proto")?.toLowerCase()
  if (proto === "https") return true
  if (proto === "http") return false
  return process.env.NODE_ENV === "production"
}
