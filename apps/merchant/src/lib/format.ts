/**
 * Display formatting for money, dates, and identifiers.
 *
 * Amounts cross the wire as integer minor units (cents). Every page used to
 * inline `(x / 100).toFixed(2)`, which loses the currency and the thousands
 * separators; these helpers keep one implementation instead of eleven.
 *
 * Dates are deliberately formatted with an explicit `en-GB`-style construction
 * rather than `toLocaleDateString()` with no locale. The server renders with the
 * container's locale and the browser renders with the user's, so an unqualified
 * call produces different strings on each side and React reports a hydration
 * mismatch. Every date here is rendered inside a `<ClientOnly>`-style guard or
 * after data has loaded on the client, but pinning the locale means the output
 * is stable regardless.
 */

/** Currencies whose minor unit is 1/100 of the major unit. All four supported ones are. */
const MINOR_UNIT_DIVISOR = 100

/**
 * `1234567` + `"USD"` → `"12,345.67 USD"`.
 * The currency code trails the figure so columns of mixed currencies stay aligned
 * on the decimal point.
 */
export function formatAmount(minorUnits: number, currency: string): string {
  return `${formatFigure(minorUnits)} ${currency}`
}

/** Just the figure, no currency — for cells that carry the currency in their own column. */
export function formatFigure(minorUnits: number): string {
  const major = minorUnits / MINOR_UNIT_DIVISOR
  const negative = major < 0
  const [whole, fraction] = Math.abs(major).toFixed(2).split(".")
  const grouped = whole.replace(/\B(?=(\d{3})+(?!\d))/g, ",")
  return `${negative ? "-" : ""}${grouped}.${fraction}`
}

const MONTHS = [
  "Jan", "Feb", "Mar", "Apr", "May", "Jun",
  "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
]

function parse(value: string | Date): Date | null {
  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

/** `"2026-03-14T…"` → `"14 Mar 2026"`. Returns an em dash for missing/invalid input. */
export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return "—"
  const date = parse(value)
  if (!date) return "—"
  return `${date.getDate()} ${MONTHS[date.getMonth()]} ${date.getFullYear()}`
}

/** `"2026-03-14T…"` → `"14 Mar 2026, 09:41"`. */
export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) return "—"
  const date = parse(value)
  if (!date) return "—"
  const hh = String(date.getHours()).padStart(2, "0")
  const mm = String(date.getMinutes()).padStart(2, "0")
  return `${formatDate(date)}, ${hh}:${mm}`
}

/**
 * Shortens an opaque identifier for display: `"pi_9f2c…a41d"`.
 * Keeps both ends so two ids that share a prefix stay distinguishable — the old
 * `slice(0, 12) + "..."` collapsed sequential ids into identical-looking strings.
 */
export function shortId(id: string, lead = 8, tail = 4): string {
  if (id.length <= lead + tail + 1) return id
  return `${id.slice(0, lead)}…${id.slice(-tail)}`
}

/** `"Every 3 months"` / `"Monthly"` for a plan's billing interval. */
export function formatInterval(count: number, unit: string): string {
  const lower = unit.toLowerCase()
  if (count === 1) {
    const simple: Record<string, string> = {
      day: "Daily",
      week: "Weekly",
      month: "Monthly",
      year: "Yearly",
    }
    return simple[lower] ?? `Every ${lower}`
  }
  return `Every ${count} ${lower}s`
}

/** Turns `"bank_transfer"` / `"paymentAuthorized"` into `"Bank Transfer"` / `"Payment Authorized"`. */
export function humanize(value: string): string {
  const spaced = value
    .replace(/([a-z\d])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .trim()
  return spaced
    .split(/\s+/)
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(" ")
}
