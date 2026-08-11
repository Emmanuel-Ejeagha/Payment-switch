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
  const contentType = request.headers.get("content-type")

  const headers: Record<string, string> = {}
  if (contentType) headers["content-type"] = contentType

  // Payment commands dedupe on this header. Dropping it turns every retry, and every
  // double-click, into a second authorization, capture or refund.
  const idempotencyKey = request.headers.get("idempotency-key")
  if (idempotencyKey) headers["idempotency-key"] = idempotencyKey

  const initialAccessToken = cookie
    ? cookie.split("; ").find(c => c.startsWith("access_token="))?.split("=")[1]
    : undefined

  const body = request.method === "GET" || request.method === "HEAD" ? undefined : await request.text()

  async function callUpstream(bearer?: string): Promise<Response> {
    const upstreamHeaders: Record<string, string> = { ...headers }
    const token = bearer ?? initialAccessToken
    if (token) upstreamHeaders["authorization"] = `Bearer ${token}`

    const res = await fetch(url, { method: request.method, headers: upstreamHeaders, body })
    return new Response(await res.text(), {
      status: res.status,
      headers: { "content-type": res.headers.get("content-type") || "application/json" },
    })
  }

  let response = await callUpstream()

  // The access token may have expired between page renders. Rotate it once and
  // retry before surfacing a 401 to the browser.
  if (response.status === 401 && initialAccessToken) {
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
