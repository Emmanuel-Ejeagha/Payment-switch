import { NextResponse } from "next/server"
import { cookies, headers } from "next/headers"
import { isSecureRequest, serverApiUrl } from "@/lib/server-url"

export async function POST() {
  const cookieStore = await cookies()
  const accessToken = cookieStore.get("admin_access_token")?.value
  const refreshToken = cookieStore.get("admin_refresh_token")?.value

  // Revoke the server-side refresh token so it cannot be used after sign-out.
  if (accessToken && refreshToken) {
    try {
      await fetch(serverApiUrl("/identity/api/v1/auth/revoke"), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ refreshToken }),
      })
    } catch {
      // Best effort: the cookies are cleared regardless so the client can sign out.
    }
  }

  const isSecure = isSecureRequest({ headers: await headers() })
  const response = NextResponse.json({ ok: true })
  response.cookies.set("admin_access_token", "", {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  })
  response.cookies.set("admin_refresh_token", "", {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  })
  return response
}
