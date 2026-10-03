import { renderHook, waitFor } from "@testing-library/react"
import { describe, expect, it, vi, afterEach, beforeEach } from "vitest"
import { useNotifications } from "@/components/use-notifications"

const startMock = vi.fn()
const stopMock = vi.fn()
const onMock = vi.fn()

vi.mock("@microsoft/signalr", () => ({
  HubConnectionBuilder: vi.fn(function (this: unknown) {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn(() => ({
        on: onMock,
        onreconnecting: vi.fn(),
        onreconnected: vi.fn(),
        onclose: vi.fn(),
        start: startMock,
        stop: stopMock,
      })),
    }
  }),
  LogLevel: { Warning: 3 },
}))

describe("useNotifications session gate", () => {
  beforeEach(() => {
    process.env.NEXT_PUBLIC_API_URL = "http://localhost"
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it("stays quiet without starting a connection when there is no session", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => new Response(JSON.stringify({ error: "No refresh token" }), { status: 401 }))
    )

    const { result } = renderHook(() => useNotifications())

    await waitFor(() => {
      expect(startMock).not.toHaveBeenCalled()
    })
    expect(result.current.connected).toBe(false)
    // No scary error state for logged-out users: realtime is enhancement-only.
    expect(result.current.error).toBeNull()
  })

  it("connects when a session exists", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => new Response(JSON.stringify({ accessToken: "abc" }), { status: 200 }))
    )
    startMock.mockResolvedValueOnce(undefined)

    const { result } = renderHook(() => useNotifications())

    await waitFor(() => {
      expect(startMock).toHaveBeenCalledTimes(1)
    })
    expect(result.current.connected).toBe(true)
  })
})
