"use client"

import { useEffect } from "react"
import Link from "next/link"
import { AlertTriangle, ArrowLeft, RefreshCw } from "lucide-react"

/**
 * Route-segment error boundary. It renders *inside* the root layout, so it must
 * not emit its own <html>/<body> — the previous version did, nesting a second
 * document inside the first.
 */
export default function AppError({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  useEffect(() => {
    // Surfacing the digest makes a production stack trace findable in the server
    // logs, where the real message lives.
    console.error(error)
  }, [error])

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface px-4 py-12">
      <div className="w-full max-w-md rounded-2xl border bg-card p-8 text-center shadow-card">
        <span
          className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-destructive/10 text-destructive"
          aria-hidden="true"
        >
          <AlertTriangle className="h-6 w-6" />
        </span>
        <h1 className="mt-5 text-xl font-semibold tracking-tight">Something went wrong</h1>
        <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
          {error.message || "An unexpected error interrupted this page."}
        </p>
        {error.digest && (
          <p className="mt-3 font-mono text-[11px] text-muted-foreground">
            Reference {error.digest}
          </p>
        )}
        <div className="mt-6 flex flex-col gap-2 sm:flex-row sm:justify-center">
          <button
            type="button"
            onClick={reset}
            className="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-subtle transition-opacity hover:opacity-90"
          >
            <RefreshCw className="h-4 w-4" aria-hidden="true" />
            Try again
          </button>
          <Link
            href="/dashboard"
            className="inline-flex h-10 items-center justify-center gap-2 rounded-lg border bg-card px-4 text-sm font-medium shadow-subtle transition-colors hover:bg-accent"
          >
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            Back to dashboard
          </Link>
        </div>
      </div>
    </div>
  )
}
