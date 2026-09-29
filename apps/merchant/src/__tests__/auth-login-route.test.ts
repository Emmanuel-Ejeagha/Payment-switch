import { afterEach, describe, expect, it, vi } from "vitest"
import { POST } from "@/app/api/auth/login/route"

describe("POST /api/auth/login", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("sets httpOnly cookies and returns ok without leaking refreshToken", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () =>
        Response.json({
          accessToken: "access-123",
          refreshToken: "refresh-123",
          expiresIn: 3600,
          emailConfirmed: true,
        })
      )
    )

    const req = new Request("http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "a@example.com", password: "x" }),
      headers: { "Content-Type": "application/json" },
    })

    const res = await POST(req)
    expect(res.status).toBe(200)
    const body = await res.json()
    expect(body).toEqual({ ok: true })
    expect(body).not.toHaveProperty("refreshToken")
    expect(body).not.toHaveProperty("accessToken")
    expect(body).not.toHaveProperty("refresh_token")
    const cookies = res.headers.getSetCookie().join(";")
    expect(cookies).toContain("access_token=access-123")
    expect(cookies).toContain("refresh_token=refresh-123")
    expect(cookies).toContain("HttpOnly")
  })

  it("propagates backend error without setting cookies", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => Response.json({ message: "Invalid credentials" }, { status: 401 }))
    )

    const req = new Request("http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "a@example.com", password: "x" }),
      headers: { "Content-Type": "application/json" },
    })

    const res = await POST(req)
    expect(res.status).toBe(401)
    expect(res.headers.getSetCookie().length).toBe(0)
  })

  it("returns 502 when identity is unreachable", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        throw new Error("ECONNREFUSED")
      })
    )

    const req = new Request("http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "a@example.com", password: "x" }),
      headers: { "Content-Type": "application/json" },
    })

    const res = await POST(req)
    expect(res.status).toBe(502)
  })
})
