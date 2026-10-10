import { afterEach, describe, expect, it, vi } from "vitest"
import { cookies, headers } from "next/headers"
import { GET } from "@/app/api/auth/token/route"

vi.mock("next/headers", () => ({
  cookies: vi.fn(),
  headers: vi.fn(async () => new Headers()),
}))

const mockedCookies = vi.mocked(cookies)
vi.mocked(headers)

function mockCookies(refreshToken?: string) {
  mockedCookies.mockResolvedValue({
    get: (name: string) =>
      name === "admin_refresh_token" && refreshToken ? { value: refreshToken } : undefined,
  } as never)
}

describe("GET /api/auth/token", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("rotates the session and returns the new access token", async () => {
    mockCookies("old-refresh")
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => Response.json({ accessToken: "new-access", refreshToken: "new-refresh" }))
    )

    const res = await GET()
    expect(res.status).toBe(200)
    expect(await res.json()).toEqual({ accessToken: "new-access" })
    expect(res.headers.getSetCookie().join(";")).toContain("admin_access_token=new-access")
    expect(res.headers.getSetCookie().join(";")).toContain("admin_refresh_token=new-refresh")
  })

  it("returns 401 instead of crashing when identity answers non-JSON", async () => {
    mockCookies("stale-refresh")
    vi.stubGlobal("fetch", vi.fn(async () => new Response(null, { status: 404 })))

    const res = await GET()
    expect(res.status).toBe(401)
    expect(await res.json()).toEqual({ error: "Refresh failed" })
  })

  it("returns 502 when identity is unreachable", async () => {
    mockCookies("some-refresh")
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        throw new Error("ECONNREFUSED")
      })
    )

    const res = await GET()
    expect(res.status).toBe(502)
    expect(await res.json()).toEqual({ error: "Identity service unreachable" })
  })

  it("returns 401 when there is no refresh cookie", async () => {
    mockCookies(undefined)
    const fetchMock = vi.fn()
    vi.stubGlobal("fetch", fetchMock)

    const res = await GET()
    expect(res.status).toBe(401)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it("does not forward authorization header to identity", async () => {
    mockCookies("old-refresh")
    const fetchMock = vi.fn(async (_url: string, init?: RequestInit) => {
      const headers = init?.headers as Record<string, string> | undefined
      expect(headers).not.toHaveProperty("authorization")
      return Response.json({ accessToken: "new-access", refreshToken: "new-refresh" })
    })
    vi.stubGlobal("fetch", fetchMock)

    await GET()
    expect(fetchMock).toHaveBeenCalled()
  })

  it("uses unified maxAge constants", async () => {
    const { ACCESS_TOKEN_MAX_AGE, REFRESH_TOKEN_MAX_AGE } = await import("@/lib/auth")
    expect(ACCESS_TOKEN_MAX_AGE).toBe(60 * 15)
    expect(REFRESH_TOKEN_MAX_AGE).toBe(60 * 60 * 24 * 7)
  })
})
