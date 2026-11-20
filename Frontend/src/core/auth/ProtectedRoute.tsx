import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './useAuth'
import type { Role } from '@/core/api/types'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'

interface ProtectedRouteProps {
  children: React.ReactNode
  requiredRole?: Role | Role[]
  redirectTo?: string
}

/**
 * Route guard that protects routes based on authentication state and role.
 * Redirects unauthenticated users to /login.
 * Redirects authenticated users without required role to their home page.
 */
export function ProtectedRoute({
  children,
  requiredRole,
  redirectTo = '/login'
}: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, hasRole } = useAuth()
  const location = useLocation()

  // Show loading spinner while checking auth state
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <LoadingSpinner size="lg" label="Loading..." />
      </div>
    )
  }

  // Not authenticated — redirect to login with return URL
  if (!isAuthenticated) {
    const searchParams = new URLSearchParams()
    searchParams.set('from', location.pathname + location.search)
    return <Navigate to={`/${redirectTo}?${searchParams}`} replace />
  }

  // Check role requirement
  if (requiredRole && !hasRole(requiredRole)) {
    // Redirect to dashboard or a forbidden page
    return <Navigate to="/dashboard" replace />
  }

  return <>{children}</>
}

/**
 * Public route wrapper — redirects authenticated users away from public pages.
 */
export function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <LoadingSpinner size="lg" label="Loading..." />
      </div>
    )
  }

  if (isAuthenticated) {
    const from = location.search
    return <Navigate to={`/dashboard${from}`} replace />
  }

  return <>{children}</>
}
