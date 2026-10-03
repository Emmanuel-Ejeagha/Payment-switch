import { describe, expect, it, vi, afterEach } from "vitest"
import { isSecureRequest, serverApiUrl } from "@/lib/server-url"

function requestWith(proto: string | null): Request {
  const headers = new Headers()
  if (proto !== null) headers.set("x-forwarded-proto", proto)
  return { headers } as Request
}

describe("server-url helpers", () => {
  const originalInternal = process.env.INTERNAL_API_URL
  const originalPublic = process.env.NEXT_PUBLIC_API_URL

  afterEach(() => {
    vi.unstubAllGlobals()
    process.env.INTERNAL_API_URL = originalInternal
    process.env.NEXT_PUBLIC_API_URL = originalPublic
  })

  it("prefers INTERNAL_API_URL for server-side fetches", () => {
    process.env.INTERNAL_API_URL = "http://nginx"
    process.env.NEXT_PUBLIC_API_URL = "http://localhost"
    expect(serverApiUrl("/identity/api/v1/auth/login")).toBe("http://nginx/identity/api/v1/auth/login")
  })

  it("falls back to the browser-facing URL (dev servers, tests)", () => {
    delete process.env.INTERNAL_API_URL
    process.env.NEXT_PUBLIC_API_URL = "http://localhost"
    expect(serverApiUrl("/identity/api/v1/auth/login")).toBe("http://localhost/identity/api/v1/auth/login")
  })

  it("marks cookies Secure only on real https traffic", () => {
    expect(isSecureRequest(requestWith("https"))).toBe(true)
    expect(isSecureRequest(requestWith("http"))).toBe(false)
  })

  it("falls back to NODE_ENV without a forwarded proto", () => {
    vi.stubEnv("NODE_ENV", "production")
    expect(isSecureRequest(requestWith(null))).toBe(true)
    vi.stubEnv("NODE_ENV", "development")
    expect(isSecureRequest(requestWith(null))).toBe(false)
  })
})
