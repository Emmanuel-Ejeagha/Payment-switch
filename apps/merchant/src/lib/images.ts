/**
 * Remote imagery used by the marketing and auth surfaces.
 *
 * Everything is served straight from the Unsplash CDN with explicit crop
 * dimensions so the aspect ratio matches the box it renders into. Callers pass
 * `unoptimized` to next/image: the browser fetches the file directly, which
 * means these pages keep working in a locked-down container where the Next
 * server itself has no outbound internet access.
 *
 * Every photo below sits on top of a solid gradient in the markup, so a blocked
 * or slow CDN degrades to a deliberate-looking colour field rather than a hole.
 */

const CDN = "https://images.unsplash.com"

function photo(id: string, width: number, height: number, quality = 80) {
  return `${CDN}/${id}?auto=format&fit=crop&w=${width}&h=${height}&q=${quality}`
}

/** Full-bleed artwork for the auth split-screen brand panel. */
export const authArtwork = {
  login: {
    src: photo("photo-1556742049-0cfed4f6a45d", 1200, 1600),
    alt: "A customer tapping a bank card on a countertop payment terminal",
  },
  register: {
    src: photo("photo-1460925895917-afdab827c52f", 1200, 1600),
    alt: "A revenue dashboard with charts open on a laptop screen",
  },
} as const

export interface Testimonial {
  quote: string
  name: string
  role: string
  avatar: string
}

/** Customer quotes for the landing page. Avatars are 96×96 square crops. */
export const testimonials: readonly Testimonial[] = [
  {
    quote:
      "We moved off our old gateway in a weekend. Payment links alone replaced an entire internal tool, and settlement finally reconciles on the first try.",
    name: "Amara Okafor",
    role: "Head of Finance, Lumen Retail",
    avatar: photo("photo-1494790108377-be9c29b29330", 96, 96),
  },
  {
    quote:
      "The webhook log is the feature I did not know I needed. Signed deliveries, retries, and a replay button meant our billing job stopped being a source of pages.",
    name: "Daniel Mensah",
    role: "Staff Engineer, Tessellate",
    avatar: photo("photo-1507003211169-0a1dd7228f2d", 96, 96),
  },
  {
    quote:
      "Subscriptions went live in two days. Plans, invoices, and dunning came out of the box, so we shipped the pricing page instead of a billing engine.",
    name: "Priya Raman",
    role: "Co-founder, Northwind Studio",
    avatar: photo("photo-1534528741775-53994a69daeb", 96, 96),
  },
] as const
