import { render, screen, waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { afterEach, describe, expect, it, vi } from "vitest"
import {
  NotificationPreferences,
  NOTIFICATION_CHANNELS,
  NOTIFICATION_EVENT_TYPES,
} from "@/components/notifications/notification-preferences"

const prefsUrl = "/api/proxy/notification/api/v1/notifications/preferences"

function mockFetch(overrides: { get?: Response; put?: Response } = {}) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? "GET"
    if (url === prefsUrl && method === "GET") {
      return overrides.get ?? Response.json([])
    }
    if (url === prefsUrl && method === "PUT") {
      return overrides.put ?? Response.json({})
    }
    return new Response(null, { status: 404 })
  })
  vi.stubGlobal("fetch", fetchMock)
  return fetchMock
}

describe("NotificationPreferences", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it("renders a channel x event-type matrix, defaulting to enabled", async () => {
    mockFetch()

    render(<NotificationPreferences />)

    expect(await screen.findByRole("heading", { name: /Notifications/ })).toBeInTheDocument()

    expect(screen.getByText("Email")).toBeInTheDocument()
    expect(screen.getByText("Sms")).toBeInTheDocument()
    expect(screen.getByText("Webhook")).toBeInTheDocument()

    const labels: Record<string, string> = {
      PaymentAuthorizedDomainEvent: "Payment Authorized Domain Event",
      PaymentCapturedDomainEvent: "Payment Captured Domain Event",
      PaymentRefundedDomainEvent: "Payment Refunded Domain Event",
      PaymentIntentCreatedDomainEvent: "Payment Intent Created Domain Event",
      PaymentVoidedDomainEvent: "Payment Voided Domain Event",
    }
    for (const eventType of NOTIFICATION_EVENT_TYPES) {
      expect(screen.getByText(labels[eventType])).toBeInTheDocument()
    }

    const switches = screen.getAllByRole("switch")
    expect(switches).toHaveLength(NOTIFICATION_CHANNELS.length * NOTIFICATION_EVENT_TYPES.length)
    expect(switches.every((s) => s.getAttribute("aria-checked") === "true")).toBe(true)
  })

  it("applies saved preferences from the server", async () => {
    mockFetch({
      get: Response.json([
        {
          id: "1",
          recipient: "owner@example.com",
          channel: "sms",
          eventType: "PaymentCapturedDomainEvent",
          enabled: false,
        },
      ]),
    })

    render(<NotificationPreferences />)

    const smsCaptured = await screen.findByRole("switch", {
      name: "Payment Captured Domain Event via Sms",
    })
    await waitFor(() => expect(smsCaptured.getAttribute("aria-checked")).toBe("false"))
  })

  it("toggles a preference off via PUT and updates the UI", async () => {
    const fetchMock = mockFetch()

    render(<NotificationPreferences />)

    const switchEl = await screen.findByRole("switch", {
      name: "Payment Authorized Domain Event via Email",
    })
    expect(switchEl.getAttribute("aria-checked")).toBe("true")

    await userEvent.click(switchEl)

    await waitFor(() => expect(switchEl.getAttribute("aria-checked")).toBe("false"))

    const putCall = fetchMock.mock.calls.find(([, init]) => init?.method === "PUT")!
    expect(putCall).toBeDefined()
    expect(JSON.parse(String(putCall[1]?.body))).toEqual({
      channel: "email",
      eventType: "PaymentAuthorizedDomainEvent",
      enabled: false,
    })
    expect(await screen.findByText("Preference saved")).toBeInTheDocument()
  })
})