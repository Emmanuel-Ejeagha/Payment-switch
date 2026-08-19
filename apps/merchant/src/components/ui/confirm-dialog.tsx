"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { Modal } from "@/components/ui/modal"
import { Button } from "@/components/ui/button"

interface ConfirmOptions {
  title: string
  /** What will happen, in plain language. Say the consequence, not "Are you sure?". */
  message: React.ReactNode
  confirmLabel?: string
  cancelLabel?: string
  destructive?: boolean
}

/**
 * Replaces `window.confirm` for destructive actions.
 *
 * The native dialog blocks the main thread, cannot be styled, is suppressed
 * entirely by some browsers when a page is not user-activated, and reads the
 * page URL aloud before the question. It also gave every action the same
 * two-word prompt regardless of whether it archived a plan or deleted a
 * customer.
 *
 * Usage keeps the same shape as the call it replaces:
 *
 *   const { confirm, dialog } = useConfirm()
 *   if (!(await confirm({ title: …, message: … }))) return
 *   …
 *   return <>{dialog}…</>
 */
export function useConfirm() {
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const resolver = useRef<((value: boolean) => void) | null>(null)

  // A pending promise with no resolver left behind would hang the caller's
  // `await` forever, so anything still open when this unmounts resolves false.
  useEffect(() => {
    return () => {
      resolver.current?.(false)
      resolver.current = null
    }
  }, [])

  const confirm = useCallback((opts: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      // Two overlapping confirms would strand the first promise; settle it.
      resolver.current?.(false)
      resolver.current = resolve
      setOptions(opts)
    })
  }, [])

  const settle = useCallback((value: boolean) => {
    const resolve = resolver.current
    resolver.current = null
    setOptions(null)
    resolve?.(value)
  }, [])

  const dialog = options ? (
    <Modal
      title={options.title}
      onClose={() => settle(false)}
      footer={
        <>
          <Button onClick={() => settle(false)}>{options.cancelLabel ?? "Cancel"}</Button>
          <Button
            variant={options.destructive ? "danger" : "primary"}
            onClick={() => settle(true)}
          >
            {options.confirmLabel ?? "Confirm"}
          </Button>
        </>
      }
    >
      <p className="text-sm leading-relaxed text-muted-foreground">{options.message}</p>
    </Modal>
  ) : null

  return { confirm, dialog }
}
