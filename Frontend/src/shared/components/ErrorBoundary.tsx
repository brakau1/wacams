import { Component, type ErrorInfo, type ReactNode } from 'react'
import { AlertTriangle, RefreshCw } from 'lucide-react'

interface Props {
  children: ReactNode
  fallback?: ReactNode
}

interface State {
  hasError: boolean
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('ErrorBoundary caught:', error, errorInfo)
  }

  render(): ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback
      }

      return (
        <div className="min-h-[400px] flex flex-col items-center justify-center p-8 text-center">
          <AlertTriangle className="w-16 h-16 text-destructive mb-4" />
          <h2 className="text-xl font-bold mb-2">Something went wrong</h2>
          <p className="text-muted-foreground mb-4 max-w-md">
            An unexpected error occurred while rendering this page. Please try refreshing or navigating elsewhere.
          </p>
          <details className="text-xs text-muted-foreground mb-4 bg-muted/50 rounded p-3 w-full max-w-lg">
            <summary className="cursor-pointer font-medium">Error details</summary>
            <pre className="mt-2 overflow-auto whitespace-pre-wrap">{this.state.error?.message}</pre>
          </details>
          <button
            onClick={() => window.location.reload()}
            className="flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <RefreshCw className="w-4 h-4" />
            Refresh Page
          </button>
        </div>
      )
    }

    return this.props.children
  }
}
