import { NextResponse } from "next/server"
import { isSecureRequest, serverApiUrl } from "@/lib/server-url"

export async function POST(request: Request) {
  const body = await request.json()

  let data: Record<string, unknown>
  try {
    const res = await fetch(
      serverApiUrl("/identity/api/v1/auth/login"),
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      }
    )

    data = await res.json()

    if (!res.ok) {
      return NextResponse.json(data, { status: res.status })
    }
  } catch {
    return NextResponse.json(
      { message: "Backend unreachable or returned an invalid response" },
      { status: 502 }
    )
  }

  const isSecure = isSecureRequest(request)
  // Never echo tokens back in the response body: the browser must use the
  // httpOnly cookies. Returning backend payload verbatim would leak both
  // access and refresh tokens to page-level JavaScript.
  const response = NextResponse.json({ ok: true })
  response.cookies.set("merchant_access_token", data.accessToken as string, {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60,
  })
  response.cookies.set("merchant_refresh_token", data.refreshToken as string, {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60 * 24 * 7,
  })

  return response
}
