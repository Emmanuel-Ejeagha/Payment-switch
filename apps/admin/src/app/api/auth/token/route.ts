import { NextResponse } from "next/server"
import { cookies } from "next/headers"

function isExpired(token: string): boolean {
  try {
    const payload = JSON.parse(
      Buffer.from(token.split(".")[1] ?? "", "base64url").toString("utf-8")
    )
    return typeof payload.exp !== "number" || payload.exp * 1000 <= Date.now()
  } catch {
    return true
  }
}

export async function GET() {
  const cookieStore = await cookies()
  const accessToken = cookieStore.get("access_token")?.value ?? ""

  if (accessToken && !isExpired(accessToken)) {
    return NextResponse.json({ accessToken })
  }

  const refreshToken = cookieStore.get("refresh_token")?.value
  if (refreshToken) {
    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/identity/api/v1/auth/refresh`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken }),
        }
      )
      if (res.ok) {
        const data = await res.json()
        const isSecure = process.env.NODE_ENV === "production"
        const response = NextResponse.json({ accessToken: data.accessToken })
        response.cookies.set("access_token", data.accessToken, {
          httpOnly: true,
          secure: isSecure,
          sameSite: "lax",
          path: "/",
          maxAge: 60 * 60,
        })
        response.cookies.set("refresh_token", data.refreshToken, {
          httpOnly: true,
          secure: isSecure,
          sameSite: "lax",
          path: "/",
          maxAge: 60 * 60 * 24 * 7,
        })
        return response
      }
    } catch {
      // fall through; treat as unauthenticated
    }
  }

  return NextResponse.json({ accessToken: "" })
}