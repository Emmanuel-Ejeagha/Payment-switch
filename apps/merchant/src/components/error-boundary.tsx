"use client"

import { Component, type ErrorInfo, type ReactNode } from "react"
import { RefreshCw, RotateCcw } from "lucide-react"
import { Button, ErrorPanel } from "@/components/ui"

interface Props {
  children: ReactNode
  fallback?: ReactNode
}

interface State {
  hasError: boolean
  error?: Error
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("Dashboard render failed:", error, info.componentStack)
  }

  // Clearing the captured error re-renders the subtree. A transient failure
  // (a dropped fetch, a bad websocket frame) recovers without dropping the
  // rest of the app state the way a full reload would.
  reset = () => this.setState({ hasError: false, error: undefined })

  render() {
    if (this.state.hasError) {
      return (
        this.props.fallback || (
          <ErrorPanel
            title="Something went wrong"
            message={this.state.error?.message || "An unexpected error occurred while rendering this view."}
            action={
              <div className="flex flex-wrap items-center justify-center gap-2">
                <Button variant="primary" icon={RotateCcw} onClick={this.reset}>
                  Try again
                </Button>
                <Button icon={RefreshCw} onClick={() => window.location.reload()}>
                  Reload page
                </Button>
              </div>
            }
          />
        )
      )
    }

    return this.props.children
  }
}
