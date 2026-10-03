import { NextRequest } from "next/server"
import { POST as refreshTokens } from "../../auth/token/route"
import { serverApiUrl } from "@/lib/server-url"

async function handler(request: NextRequest, { params }: { params: Promise<{ path: string[] }> }) {
  const { path } = await params
  if (!process.env.INTERNAL_API_URL && !process.env.NEXT_PUBLIC_API_URL) {
    return Response.json({ error: "API base URL not set" }, { status: 500 })
  }

  const url = serverApiUrl(`/${path.join("/")}${request.nextUrl.search}`)
  const cookie = request.headers.get("cookie")

  // Only these browser headers pass through. Everything else — host, cookie
  // and any other ambient header — is stripped so the upstream sees exactly
  // what the page explicitly sent. Authorization is handled separately below.
  const headers: Record<string, string> = {}
  for (const name of ["content-type", "accept", "accept-language", "idempotency-key"] as const) {
    const value = request.headers.get(name)
    // Payment commands dedupe on idempotency-key: dropping it turns every
    // retry, and every double-click, into a second authorization or capture.
    if (value) headers[name] = value
  }

  // An explicit caller bearer (service-to-service) wins over the session cookie.
  const incomingAuth = request.headers.get("authorization")
  // Split on the FIRST "=" only: base64url/JWT-ish cookie values contain "=".
  const cookieValue = (name: string): string | undefined => {
    if (!cookie) return undefined
    const prefix = `${name}=`
    for (const part of cookie.split(";")) {
      const trimmed = part.trim()
      if (trimmed.startsWith(prefix)) return trimmed.slice(prefix.length)
    }
    return undefined
  }
  const initialAccessToken = cookieValue("access_token")
  const refreshToken = cookieValue("refresh_token")

  const body = request.method === "GET" || request.method === "HEAD" ? undefined : await request.text()

  async function callUpstream(bearer?: string): Promise<Response> {
    const upstreamHeaders: Record<string, string> = { ...headers }
    if (incomingAuth) {
      upstreamHeaders["authorization"] = incomingAuth
    } else {
      const token = bearer ?? initialAccessToken
      if (token) upstreamHeaders["authorization"] = `Bearer ${token}`
    }

    const res = await fetch(url, { method: request.method, headers: upstreamHeaders, body })
    // 204/304 must not carry a body — constructing one throws.
    if (res.status === 204 || res.status === 304) {
      return new Response(null, { status: res.status })
    }
    // Pass through only safe response metadata. Inventing a default
    // content-type here would lie to the browser about empty responses.
    const outHeaders = new Headers()
    for (const name of ["content-type", "x-total-count"] as const) {
      const value = res.headers.get(name)
      if (value) outHeaders.set(name, value)
    }
    return new Response(await res.text(), { status: res.status, headers: outHeaders })
  }

  const response = await callUpstream()

  // The access token may have expired between page renders. Rotate it once and
  // retry before surfacing a 401 to the browser — but only when a refresh
  // cookie exists. Retrying on access-token alone is hopeless (nothing to
  // rotate with) and would re-read an already-consumed upstream body.
  if (response.status === 401 && refreshToken) {
    const rotated = await refreshTokens()
    if (rotated.ok) {
      const setCookies = rotated.headers.getSetCookie()
      const newToken = setCookies
        .find(c => c.startsWith("access_token="))
        ?.split(";")[0]
        .split("=")
        .slice(1)
        .join("=")
      if (newToken) {
        const retried = await callUpstream(newToken)
        if (retried.status !== 401) {
          for (const setCookie of setCookies) retried.headers.append("set-cookie", setCookie)
          return retried
        }
      }
    }
  }

  return response
}

export const GET = handler
export const POST = handler
export const PUT = handler
export const PATCH = handler
export const DELETE = handler
