import { NextResponse } from "next/server"
import type { NextRequest } from "next/server"

const publicPaths = ["/login", "/register", "/verify-email", "/checkout"]
const HOME = "/dashboard"

export function middleware(request: NextRequest) {
  const accessToken = request.cookies.get("access_token")?.value
  const { pathname } = request.nextUrl

  // "/" is the public landing page. It must match exactly — a startsWith("/")
  // check would make every route public.
  const isPublic = pathname === "/" || publicPaths.some((p) => pathname.startsWith(p))

  if (isPublic) {
    if (accessToken) {
      return NextResponse.redirect(new URL(HOME, request.url))
    }
    return NextResponse.next()
  }

  if (!accessToken) {
    return NextResponse.redirect(new URL("/login", request.url))
  }

  return NextResponse.next()
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico).*)"],
}
