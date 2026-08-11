import { NextResponse } from "next/server"
import { cookies } from "next/headers"

const isSecure = process.env.NODE_ENV === "production"

async function refresh() {
  const cookieStore = await cookies()
  const accessToken = cookieStore.get("access_token")?.value
  const refreshToken = cookieStore.get("refresh_token")?.value

  if (!refreshToken) {
    return { response: NextResponse.json({ error: "No refresh token" }, { status: 401 }), ok: false }
  }

  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/token`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
    },
    body: JSON.stringify({ refreshToken }),
  })

  const data = await res.json()
  if (!res.ok || !data?.accessToken || !data?.refreshToken) {
    return { response: NextResponse.json({ error: "Refresh failed" }, { status: 401 }), ok: false }
  }

  const response = NextResponse.json({ accessToken: data.accessToken })
  response.cookies.set("access_token", data.accessToken, {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 15,
  })
  response.cookies.set("refresh_token", data.refreshToken, {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60 * 24 * 7,
  })
  return { response, ok: true }
}

export async function GET() {
  return (await refresh()).response
}

export async function POST() {
  return (await refresh()).response
}
