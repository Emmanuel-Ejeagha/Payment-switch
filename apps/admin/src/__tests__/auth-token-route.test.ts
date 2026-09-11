import { afterEach, describe, expect, it, vi } from "vitest"
import { cookies } from "next/headers"
import { GET } from "@/app/api/auth/token/route"

vi.mock("next/headers", () => ({
  cookies: vi.fn(),
}))

const mockedCookies = vi.mocked(cookies)

function mockCookies(refreshToken?: string) {
  mockedCookies.mockResolvedValue({
    get: (name: string) =>
      name === "refresh_token" && refreshToken ? { value: refreshToken } : undefined,
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
    expect(res.headers.getSetCookie().join(";")).toContain("access_token=new-access")
    expect(res.headers.getSetCookie().join(";")).toContain("refresh_token=new-refresh")
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
})
