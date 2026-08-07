import { NextResponse } from "next/server"

export async function POST(request: Request) {
  const body = await request.json()

  let data: Record<string, unknown>
  try {
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/login`,
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

    const meRes = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/users/me`,
      { headers: { authorization: `Bearer ${data.accessToken}` } }
    )
    const me = await meRes.json()
    const roles: string[] = Array.isArray(me.roles) ? me.roles : []
    if (!roles.includes("Admin")) {
      return NextResponse.json(
        { message: "Admin access required. Sign in with an admin account." },
        { status: 403 }
      )
    }
  } catch {
    return NextResponse.json(
      { message: "Backend unreachable or returned an invalid response" },
      { status: 502 }
    )
  }

  const isSecure = process.env.NODE_ENV === "production"
  const response = NextResponse.json(data)
  response.cookies.set("access_token", data.accessToken as string, {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60,
  })
  response.cookies.set("refresh_token", data.refreshToken as string, {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60 * 24 * 7,
  })

  return response
}
