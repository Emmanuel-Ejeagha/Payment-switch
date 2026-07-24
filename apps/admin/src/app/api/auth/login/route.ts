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
  } catch {
    return NextResponse.json(
      { message: "Backend unreachable or returned an invalid response" },
      { status: 502 }
    )
  }

  const response = NextResponse.json(data)
  response.cookies.set("access_token", data.accessToken as string, {
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60,
  })
  response.cookies.set("refresh_token", data.refreshToken as string, {
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60 * 24 * 7,
  })

  return response
}
