import { NextResponse } from "next/server"

export async function POST() {
  const response = NextResponse.json({ ok: true })
  response.cookies.set("access_token", "", {
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  })
  response.cookies.set("refresh_token", "", {
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  })
  return response
}
