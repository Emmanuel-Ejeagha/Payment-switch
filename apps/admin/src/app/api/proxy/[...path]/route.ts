import { NextRequest } from "next/server"
import { POST as refreshTokens } from "../../auth/token/route"

async function handler(request: NextRequest, { params }: { params: Promise<{ path: string[] }> }) {
  const { path } = await params
  const baseUrl = process.env.NEXT_PUBLIC_API_URL
  if (!baseUrl) {
    return Response.json({ error: "NEXT_PUBLIC_API_URL not set" }, { status: 500 })
  }

  const url = `${baseUrl}/${path.join("/")}${request.nextUrl.search}`
  const cookie = request.headers.get("cookie")

  function getCookieValue(header: string | null, name: string): string | undefined {
    if (!header) return undefined
    const found = header
      .split(";")
      .map((c) => c.trim())
      .find((c) => c.startsWith(name + "="))
    return found ? found.split("=").slice(1).join("=") : undefined
  }

  const initialAccessToken = getCookieValue(cookie, "access_token")
  const hasRefreshToken = !!getCookieValue(cookie, "refresh_token")

  const headers: Record<string, string> = {}
  const contentType = request.headers.get("content-type")
  if (contentType) headers["content-type"] = contentType
  const accept = request.headers.get("accept")
  if (accept) headers["accept"] = accept
  const acceptLanguage = request.headers.get("accept-language")
  if (acceptLanguage) headers["accept-language"] = acceptLanguage

  // Payment commands dedupe on this header. Dropping it turns every retry, and every
  // double-click, into a second authorization, capture or refund.
  const idempotencyKey = request.headers.get("idempotency-key")
  if (idempotencyKey) headers["idempotency-key"] = idempotencyKey
  const incomingAuth = request.headers.get("authorization")
  if (incomingAuth) headers["authorization"] = incomingAuth

  const body = request.method === "GET" || request.method === "HEAD" ? undefined : await request.text()

  async function callUpstream(bearer?: string): Promise<Response> {
    const upstreamHeaders: Record<string, string> = { ...headers }
    const token = bearer ?? initialAccessToken
    if (token && !upstreamHeaders["authorization"]) upstreamHeaders["authorization"] = `Bearer ${token}`

    const res = await fetch(url, { method: request.method, headers: upstreamHeaders, body })
    if (res.status === 204) {
      const headers204: Record<string, string> = {}
      const total = res.headers.get("x-total-count")
      if (total) headers204["x-total-count"] = total
      return new Response(null, { status: 204, headers: headers204 })
    }
    const respHeaders: Record<string, string> = {}
    const upstreamCT = res.headers.get("content-type")
    if (upstreamCT) respHeaders["content-type"] = upstreamCT
    else respHeaders["content-type"] = "application/json"
    const total = res.headers.get("x-total-count")
    if (total) respHeaders["x-total-count"] = total
    return new Response(await res.text(), {
      status: res.status,
      headers: respHeaders,
    })
  }

  let response = await callUpstream()

  // The access token may have expired between page renders. Rotate it once and
  // retry before surfacing a 401 to the browser. Use refresh cookie presence
  // so sessions with only a refresh token (access expired/removed) still retry.
  if (response.status === 401 && hasRefreshToken) {
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
