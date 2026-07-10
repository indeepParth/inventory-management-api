import { Navigate, Outlet } from 'react-router-dom'
import { AccessDeniedPage } from '../../pages/AccessDeniedPage'
import { useAuth } from './AuthContext'
import { hasRouteAccess, type RoutePolicy } from './roleAccess'

function AuthLoadingState() {
  return (
    <main className="auth-loading" aria-live="polite">
      Loading...
    </main>
  )
}

export function ProtectedRoute() {
  const { isAuthenticated, isAuthResolved } = useAuth()

  if (!isAuthResolved) {
    return <AuthLoadingState />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

type RoleProtectedRouteProps = {
  policy: RoutePolicy
}

export function RoleProtectedRoute({ policy }: RoleProtectedRouteProps) {
  const { currentUser, isAuthenticated, isAuthResolved, isCurrentUserLoading } = useAuth()

  if (!isAuthResolved || isCurrentUserLoading) {
    return <AuthLoadingState />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (!hasRouteAccess(currentUser?.roles ?? [], policy)) {
    return <AccessDeniedPage />
  }

  return <Outlet />
}

export function PublicOnlyRoute() {
  const { isAuthenticated, isAuthResolved } = useAuth()

  if (!isAuthResolved) {
    return <AuthLoadingState />
  }

  if (isAuthenticated) {
    return <Navigate to="/app/dashboard" replace />
  }

  return <Outlet />
}
