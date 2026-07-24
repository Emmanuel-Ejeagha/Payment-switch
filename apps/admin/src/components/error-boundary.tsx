"use client"

import { Component, type ReactNode, type ErrorInfo } from "react"
import { AlertTriangle } from "lucide-react"

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
    console.error("ErrorBoundary caught:", error, info.componentStack)
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback
      return (
        <div className="flex flex-col items-center justify-center rounded-xl border border-destructive/50 bg-destructive/10 p-12 text-center">
          <AlertTriangle className="mb-3 h-8 w-8 text-destructive" />
          <h2 className="text-lg font-semibold text-destructive">Something went wrong</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {this.state.error?.message || "An unexpected error occurred"}
          </p>
          <button
            onClick={() => this.setState({ hasError: false, error: undefined })}
            className="mt-4 rounded-lg border bg-background px-4 py-2 text-sm font-medium hover:bg-accent"
          >
            Try again
          </button>
        </div>
      )
    }
    return this.props.children
  }
}
