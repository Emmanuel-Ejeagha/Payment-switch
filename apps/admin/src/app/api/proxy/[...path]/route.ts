import { NextRequest } from "next/server"

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
  if (cookie) headers["cookie"] = cookie
  if (contentType) headers["content-type"] = contentType

  const body = request.method === "GET" || request.method === "HEAD" ? undefined : await request.text()

  const res = await fetch(url, { method: request.method, headers, body })
  const data = await res.text()

  return new Response(data, {
    status: res.status,
    headers: { "content-type": res.headers.get("content-type") || "application/json" },
  })
}

export const GET = handler
export const POST = handler
export const PUT = handler
export const PATCH = handler
export const DELETE = handler
