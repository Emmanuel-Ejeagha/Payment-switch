import { NextResponse } from "next/server"

const isSecure = process.env.NODE_ENV === "production"

export async function POST() {
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
