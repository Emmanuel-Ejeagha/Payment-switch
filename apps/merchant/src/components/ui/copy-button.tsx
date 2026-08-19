"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { Check, Copy } from "lucide-react"
import { cn } from "@paymentswitch/shared"

/**
 * Copies text and reports success for a moment.
 *
 * `navigator.clipboard` is undefined on insecure origins — which includes any
 * plain-HTTP deployment that is not localhost — so the previous bare
 * `navigator.clipboard.writeText(...)` threw an unhandled TypeError there and
 * the button silently did nothing. The `execCommand` path is deprecated but
 * still the only fallback that works without a secure context.
 */
export function useCopy(resetAfterMs = 1800) {
  const [copied, setCopied] = useState(false)
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Without this, copying and then navigating away sets state on an unmounted
  // component when the timer fires.
  useEffect(() => {
    return () => {
      if (timer.current) clearTimeout(timer.current)
    }
  }, [])

  const copy = useCallback(
    async (text: string) => {
      let ok = false
      try {
        if (navigator.clipboard?.writeText) {
          await navigator.clipboard.writeText(text)
          ok = true
        } else {
          const area = document.createElement("textarea")
          area.value = text
          area.setAttribute("readonly", "")
          area.style.position = "fixed"
          area.style.opacity = "0"
          document.body.appendChild(area)
          area.select()
          ok = document.execCommand("copy")
          document.body.removeChild(area)
        }
      } catch {
        ok = false
      }

      if (ok) {
        setCopied(true)
        if (timer.current) clearTimeout(timer.current)
        timer.current = setTimeout(() => setCopied(false), resetAfterMs)
      }
      return ok
    },
    [resetAfterMs],
  )

  return { copied, copy }
}

export function CopyButton({
  value,
  label = "Copy",
  copiedLabel = "Copied",
  className,
  iconOnly = false,
}: {
  value: string
  label?: string
  copiedLabel?: string
  className?: string
  iconOnly?: boolean
}) {
  const { copied, copy } = useCopy()
  const Icon = copied ? Check : Copy

  return (
    <button
      type="button"
      onClick={() => copy(value)}
      className={cn(
        "inline-flex h-8 shrink-0 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium",
        "transition-colors hover:bg-accent",
        copied && "border-emerald-500/40 text-emerald-700 dark:text-emerald-400",
        iconOnly && "w-8 justify-center px-0",
        className,
      )}
    >
      <Icon className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
      {iconOnly ? (
        <span className="sr-only">{copied ? copiedLabel : label}</span>
      ) : (
        <span>{copied ? copiedLabel : label}</span>
      )}
      {/* Announced politely so the confirmation is not silent for screen readers. */}
      <span aria-live="polite" className="sr-only">
        {copied ? `${copiedLabel} to clipboard` : ""}
      </span>
    </button>
  )
}
