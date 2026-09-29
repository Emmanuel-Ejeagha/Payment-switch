import { NextResponse } from "next/server"

// Liveness probe for the compose healthcheck (Step 9.3) and k8s probes:
// proves the standalone server is up without touching any backend.
export async function GET() {
  return NextResponse.json({ ok: true })
}
