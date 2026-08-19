"use client"

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  useRef,
  type ReactNode,
} from "react"
import { CheckCircle2, X, XCircle } from "lucide-react"
import { cn } from "@paymentswitch/shared"

type ToastType = "success" | "error"

interface Toast {
  id: number
  type: ToastType
  message: string
}

interface ToastContextValue {
  showToast: (type: ToastType, message: string) => void
}

const ToastContext = createContext<ToastContextValue>({ showToast: () => {} })

export function useToast() {
  return useContext(ToastContext)
}

let nextId = 0

const TONE: Record<ToastType, { wrap: string; icon: typeof CheckCircle2; iconClass: string }> = {
  success: {
    wrap: "border-emerald-500/40 bg-card text-foreground",
    icon: CheckCircle2,
    iconClass: "text-emerald-600 dark:text-emerald-400",
  },
  error: {
    wrap: "border-destructive/40 bg-card text-foreground",
    icon: XCircle,
    iconClass: "text-destructive",
  },
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  // Dismissal timers are tracked so unmounting the provider cannot leave a
  // pending setState pointed at a component that is gone.
  const timers = useRef<Map<number, ReturnType<typeof setTimeout>>>(new Map())

  const remove = useCallback((id: number) => {
    const timer = timers.current.get(id)
    if (timer) {
      clearTimeout(timer)
      timers.current.delete(id)
    }
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const showToast = useCallback(
    (type: ToastType, message: string) => {
      const id = nextId++
      setToasts((prev) => [...prev, { id, type, message }])
      timers.current.set(
        id,
        setTimeout(() => {
          timers.current.delete(id)
          setToasts((prev) => prev.filter((t) => t.id !== id))
        }, 4000),
      )
    },
    [],
  )

  useEffect(() => {
    const pending = timers.current
    return () => {
      pending.forEach(clearTimeout)
      pending.clear()
    }
  }, [])

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div
        className="pointer-events-none fixed inset-x-4 bottom-4 z-[100] flex flex-col items-end gap-2 sm:left-auto sm:right-6 sm:bottom-6 sm:max-w-sm"
        aria-live="polite"
        aria-atomic="false"
      >
        {toasts.map((t) => {
          const { wrap, icon: Icon, iconClass } = TONE[t.type]
          return (
            <div
              key={t.id}
              role={t.type === "error" ? "alert" : "status"}
              className={cn(
                "pointer-events-auto flex w-full animate-fade-up items-start gap-3 rounded-xl border px-4 py-3 text-sm shadow-lift",
                wrap,
              )}
            >
              <Icon className={cn("mt-0.5 h-4 w-4 shrink-0", iconClass)} aria-hidden="true" />
              <span className="min-w-0 flex-1 leading-relaxed">{t.message}</span>
              <button
                type="button"
                onClick={() => remove(t.id)}
                className="-mr-1 shrink-0 rounded-md p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <X className="h-3.5 w-3.5" aria-hidden="true" />
                <span className="sr-only">Dismiss notification</span>
              </button>
            </div>
          )
        })}
      </div>
    </ToastContext.Provider>
  )
}
