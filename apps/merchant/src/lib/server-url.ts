/**
 * Base URL for server-to-server fetch calls (Route Handlers, proxy routes).
 *
 * Browsers reach the stack through nginx (`NEXT_PUBLIC_API_URL`), but that
 * origin is unreachable from *inside* containers (localhost there is the app
 * container itself). `INTERNAL_API_URL` (e.g. `http://nginx` in compose) is
 * used when set; otherwise we fall back to the public URL, which is correct
 * for host-run dev servers.
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
