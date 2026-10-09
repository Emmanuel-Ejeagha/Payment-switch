import type { Metadata, Viewport } from "next"
import localFont from "next/font/local"
import "./globals.css"
import { ThemeProvider } from "@/components/theme-provider"

// Self-hosted Inter (latin 400/500/600/700) so production Docker builds never
// depend on fonts.googleapis.com egress. Same files Google serves; swap the
// variable or add weights under ./fonts if the design needs more.
const inter = localFont({
  src: [
    { path: "./fonts/inter-latin-400-normal.woff2", weight: "400", style: "normal" },
    { path: "./fonts/inter-latin-500-normal.woff2", weight: "500", style: "normal" },
    { path: "./fonts/inter-latin-600-normal.woff2", weight: "600", style: "normal" },
    { path: "./fonts/inter-latin-700-normal.woff2", weight: "700", style: "normal" },
  ],
  display: "swap",
  variable: "--font-sans",
})

export const metadata: Metadata = {
  title: {
    default: "PaymentSwitch — Accept payments anywhere",
    template: "%s | PaymentSwitch",
  },
  description:
    "PaymentSwitch is a real-time payment switch with hosted checkout, subscriptions, webhooks, and daily settlement.",
  openGraph: {
    type: "website",
    siteName: "PaymentSwitch",
    title: "PaymentSwitch — Accept payments anywhere",
    description:
      "Hosted checkout, payment links, subscriptions, signed webhooks, and a double-entry ledger — on one event-driven core.",
  },
  twitter: {
    card: "summary_large_image",
    title: "PaymentSwitch — Accept payments anywhere",
    description:
      "Hosted checkout, payment links, subscriptions, signed webhooks, and a double-entry ledger — on one event-driven core.",
  },
}

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#ffffff" },
    { media: "(prefers-color-scheme: dark)", color: "#020817" },
  ],
}

// Applies the stored theme before first paint. Default is light; the "dark"
// class is added only when the user explicitly chose dark before.
const themeScript = `(function(){try{var s=localStorage.getItem("theme");if(s==="dark")document.documentElement.classList.add("dark")}catch(e){}})()`

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={inter.variable} suppressHydrationWarning>
      <body className="min-h-screen bg-background font-sans antialiased">
        <script dangerouslySetInnerHTML={{ __html: themeScript }} />
        <ThemeProvider>{children}</ThemeProvider>
      </body>
    </html>
  )
}
