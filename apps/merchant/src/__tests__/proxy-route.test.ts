import { describe, expect, it, vi, afterEach, beforeEach } from "vitest"
import { NextRequest } from "next/server"

vi.mock("../app/api/auth/token/route", () => ({
  POST: vi.fn(),
}))

import { POST as refreshTokens } from "../app/api/auth/token/route"
import { GET, POST, PUT, PATCH, DELETE } from "../app/api/proxy/[...path]/route"

const mockedRefresh = vi.mocked(refreshTokens)

function makeRequest(path: string[], headers: Record<string, string> = {}, method = "GET", body?: string) {
  const url = `http://localhost/api/proxy/${path.join("/")}`
  return new NextRequest(url, {
    method,
    headers: new Headers(headers),
    body: method === "GET" || method === "HEAD" ? undefined : body,
  })
}

describe("proxy hardening", () => {
  const originalEnv = process.env.NEXT_PUBLIC_API_URL
  beforeEach(() => {
    process.env.NEXT_PUBLIC_API_URL = "http://upstream.example.com"
    vi.stubGlobal("fetch", vi.fn())
  })
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    process.env.NEXT_PUBLIC_API_URL = originalEnv
  })

  it("parses cookie tolerant of = (value contains =)", async () => {
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockResolvedValue(new Response(JSON.stringify({ ok: 1 }), { status: 200, headers: { "content-type": "application/json" } }))

    const req = makeRequest(["payment", "api", "v1", "test"], {
      cookie: "access_token=a=b=c; refresh_token=r=x=y",
      "content-type": "application/json",
    })
    await GET(req, { params: Promise.resolve({ path: ["payment", "api", "v1", "test"] }) })

    expect(fetchMock).toHaveBeenCalled()
    const upstreamHeaders = fetchMock.mock.calls[0][1]?.headers as Record<string, string>
    expect(upstreamHeaders["authorization"]).toBe("Bearer a=b=c")
  })

  it("forwards accept and accept-language, strips host and cookie", async () => {
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockResolvedValue(new Response("{}", { status: 200, headers: { "content-type": "application/json" } }))

    const req = makeRequest(["payment", "api", "v1", "test"], {
      cookie: "access_token=tok",
      accept: "application/json",
      "accept-language": "en-US",
      host: "evil.com",
      "content-type": "application/json",
    })
    await GET(req, { params: Promise.resolve({ path: ["payment", "api", "v1", "test"] }) })

    const upstreamHeaders = fetchMock.mock.calls[0][1]?.headers as Record<string, string>
    expect(upstreamHeaders["accept"]).toBe("application/json")
    expect(upstreamHeaders["accept-language"]).toBe("en-US")
    expect(upstreamHeaders["authorization"]).toBe("Bearer tok")
    expect(upstreamHeaders).not.toHaveProperty("host")
    expect(upstreamHeaders).not.toHaveProperty("cookie")
  })

  it("forwards incoming authorization header over cookie", async () => {
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockResolvedValue(new Response("{}", { status: 200 }))

    const req = makeRequest(["payment", "api", "v1", "test"], {
      cookie: "access_token=cookieTok",
      authorization: "Bearer incoming",
    })
    await GET(req, { params: Promise.resolve({ path: ["payment", "api", "v1", "test"] }) })

    const upstreamHeaders = fetchMock.mock.calls[0][1]?.headers as Record<string, string>
    expect(upstreamHeaders["authorization"]).toBe("Bearer incoming")
  })

  it("passthrough content-type and x-total-count", async () => {
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify([{ id: 1 }]), {
        status: 200,
        headers: { "content-type": "application/json", "x-total-count": "42" },
      })
    )

    const req = makeRequest(["payment", "api", "v1", "payments"], {
      cookie: "access_token=tok",
    })
    const res = await GET(req, { params: Promise.resolve({ path: ["payment", "api", "v1", "payments"] }) })

    expect(res.headers.get("content-type")).toContain("application/json")
    expect(res.headers.get("x-total-count")).toBe("42")
  })

  it("handles 204 with no body and no content-type", async () => {
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    const req = makeRequest(["payment", "api", "v1", "test"], { cookie: "access_token=tok" })
    const res = await GET(req, { params: Promise.resolve({ path: ["payment", "api", "v1", "test"] }) })

    expect(res.status).toBe(204)
    expect(await res.text()).toBe("")
    expect(res.headers.get("content-type")).toBeNull()
  })

  it("retries 401 when only refresh_token present (no access_token)", async () => {
    const fetchMock = vi.mocked(fetch)
    fetchMock
      .mockResolvedValueOnce(new Response("unauthorized", { status: 401 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ ok: 1 }), { status: 200, headers: { "content-type": "application/json" } }))

    const fakeRefresh = new Response(null, {
      status: 200,
      headers: { "set-cookie": "access_token=newTok; Path=/; HttpOnly" },
    })
    mockedRefresh.mockResolvedValue(fakeRefresh as unknown as never)

    const req = makeRequest(["payment", "api", "v1", "test"], {
      cookie: "refresh_token=refreshOnly",
    })
    const res = await GET(req, { params: Promise.resolve({ path: ["payment", "api", "v1", "test"] }) })

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(res.status).toBe(200)
  })

  it("does not retry 401 when no refresh cookie", async () => {
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockResolvedValue(new Response("unauthorized", { status: 401 }))

    const req = makeRequest(["payment", "api", "v1", "test"], {
      cookie: "access_token=tok",
    })
    const res = await GET(req, { params: Promise.resolve({ path: ["payment", "api", "v1", "test"] }) })

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(res.status).toBe(401)
    expect(mockedRefresh).not.toHaveBeenCalled()
  })
})
