import { NextResponse } from "next/server"
import { cookies } from "next/headers"

export async function GET() {
  const cookieStore = await cookies()
  const accessToken = cookieStore.get("access_token")?.value ?? ""
  return NextResponse.json({ accessToken })
}
