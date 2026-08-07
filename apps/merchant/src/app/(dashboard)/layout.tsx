"use client"

import { useState } from "react"
import { Menu } from "lucide-react"
import Link from "next/link"
import { Sidebar } from "@/components/layout/sidebar"
import { ErrorBoundary } from "@/components/error-boundary"
import { ToastProvider } from "@/components/toast"

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  return (
    <ToastProvider>
      <div className="flex h-screen">
        <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
        <main className="flex flex-1 flex-col overflow-hidden">
          <div className="flex h-14 items-center gap-3 border-b px-4 md:hidden">
            <button
              onClick={() => setSidebarOpen(true)}
              className="rounded-lg p-1 hover:bg-accent"
            >
              <Menu className="h-5 w-5" />
            </button>
            <Link href="/dashboard" className="font-semibold">PaymentSwitch</Link>
          </div>
          <div className="flex-1 overflow-y-auto p-4 md:p-8">
            <ErrorBoundary>{children}</ErrorBoundary>
          </div>
        </main>
      </div>
    </ToastProvider>
  )
}
