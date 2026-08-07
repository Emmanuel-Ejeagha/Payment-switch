import type { Metadata } from "next"
import "./globals.css"
import { ThemeProvider } from "@/components/theme-provider"

export const metadata: Metadata = {
  title: {
    default: "PaymentSwitch — Accept payments anywhere",
    template: "%s | PaymentSwitch",
  },
  description:
    "PaymentSwitch is a real-time payment switch with hosted checkout, subscriptions, webhooks, and daily settlement.",
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className="min-h-screen bg-background antialiased">
        <ThemeProvider>{children}</ThemeProvider>
      </body>
    </html>
  )
}
