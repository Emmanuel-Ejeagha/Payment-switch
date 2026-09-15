import { afterEach, describe, expect, it, vi } from "vitest"
import { POST } from "@/app/api/auth/login/route"

describe("POST /api/auth/login", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("sets httpOnly cookies and returns ok without leaking refreshToken", async () => {
    let call = 0
    vi.stubGlobal(
      "fetch",
      vi.fn(async (url: string) => {
        call++
        if (call === 1) {
          return Response.json({
            accessToken: "access-admin",
            refreshToken: "refresh-admin",
            expiresIn: 3600,
            emailConfirmed: true,
          })
        }
        // second call is /users/me for admin guard
        return Response.json({ roles: ["Admin"] })
      })
    )

    const req = new Request("http://localhost/admin/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "admin@example.com", password: "x" }),
      headers: { "Content-Type": "application/json" },
    })

    const res = await POST(req)
    expect(res.status).toBe(200)
    const body = await res.json()
    expect(body).toEqual({ ok: true })
    expect(body).not.toHaveProperty("refreshToken")
    const cookies = res.headers.getSetCookie().join(";")
    expect(cookies).toContain("access_token=access-admin")
    expect(cookies).toContain("refresh_token=refresh-admin")
    expect(cookies).toContain("HttpOnly")
  })

  it("rejects non-admin with 403 and does not set cookies", async () => {
    let call = 0
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        call++
        if (call === 1) {
          return Response.json({
            accessToken: "access-user",
            refreshToken: "refresh-user",
            expiresIn: 3600,
            emailConfirmed: true,
          })
        }
        return Response.json({ roles: ["Merchant"] })
      })
    )

    const req = new Request("http://localhost/admin/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "user@example.com", password: "x" }),
      headers: { "Content-Type": "application/json" },
    })

    const res = await POST(req)
    expect(res.status).toBe(403)
    expect(res.headers.getSetCookie().length).toBe(0)
  })

  it("propagates backend error without setting cookies", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => Response.json({ message: "Invalid credentials" }, { status: 401 }))
    )

    const req = new Request("http://localhost/admin/api/auth/login", {
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

    const req = new Request("http://localhost/admin/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "a@example.com", password: "x" }),
      headers: { "Content-Type": "application/json" },
    })

    const res = await POST(req)
    expect(res.status).toBe(502)
  })
})
