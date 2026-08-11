import { NextResponse } from "next/server"
import type { NextRequest } from "next/server"

const publicPaths = ["/login", "/forgot-password", "/reset-password"]

function isExpired(token: string): boolean {
  try {
    const payload = JSON.parse(Buffer.from(token.split(".")[1] ?? "", "base64url").toString("utf-8"))
    return typeof payload.exp !== "number" || payload.exp * 1000 <= Date.now()
  } catch {
    // Undecodable tokens are treated as expired so the flow falls through to a
    // silent refresh (or, failing that, the login page).
    return true
  }
}

export async function middleware(request: NextRequest) {
  const accessToken = request.cookies.get("access_token")?.value
  const refreshToken = request.cookies.get("refresh_token")?.value
  const { pathname } = request.nextUrl

  if (publicPaths.includes(pathname)) {
    if (accessToken && !isExpired(accessToken)) {
      return NextResponse.redirect(new URL("/", request.url))
    }
    return NextResponse.next()
  }

  // No credentials at all — unauthenticated.
  if (!accessToken && !refreshToken) {
    return NextResponse.redirect(new URL("/login", request.url))
  }

  // Access token still valid — proceed.
  if (accessToken && !isExpired(accessToken)) {
    return NextResponse.next()
  }

  // Access token missing or expired, but a refresh token exists: try a silent
  // refresh. The /api/auth/token route rotates both cookies on success.
  if (refreshToken) {
    try {
      const res = await fetch(new URL("/api/auth/token", request.url), {
        headers: { cookie: request.headers.get("cookie") ?? "" },
      })
      const data = await res.json()
      if (data?.accessToken) {
        const response = NextResponse.next()
        for (const cookie of res.headers.getSetCookie()) {
          response.headers.append("set-cookie", cookie)
        }
        return response
      }
    } catch {
      // Fall through to the login redirect below.
    }
  }

  return NextResponse.redirect(new URL("/login?reason=expired", request.url))
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico).*)"],
}
