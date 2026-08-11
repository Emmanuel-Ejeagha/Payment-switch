import { NextResponse } from "next/server"
import { cookies } from "next/headers"

const isSecure = process.env.NODE_ENV === "production"

export async function POST() {
  const cookieStore = await cookies()
  const accessToken = cookieStore.get("access_token")?.value
  const refreshToken = cookieStore.get("refresh_token")?.value

  // Revoke the server-side refresh token so it cannot be used after sign-out.
  if (accessToken && refreshToken) {
    try {
      await fetch(`${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/revoke`, {
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

  const response = NextResponse.json({ ok: true })
  response.cookies.set("access_token", "", {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  })
  response.cookies.set("refresh_token", "", {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  })
  return response
}
