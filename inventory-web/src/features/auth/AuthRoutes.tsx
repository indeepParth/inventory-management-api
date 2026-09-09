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

function CompanySelectionState() {
  const { companies, selectCompany } = useAuth()

  if (companies.length === 0) {
    return (
      <section className="content-panel" aria-labelledby="no-company-title">
        <p className="page-kicker">Company required</p>
        <h1 id="no-company-title" className="page-title">
          No company access is available.
        </h1>
        <p className="page-copy">
          Your account is signed in, but it is not currently assigned to any company.
        </p>
      </section>
    )
  }

  return (
    <section className="content-panel" aria-labelledby="select-company-title">
      <p className="page-kicker">Company required</p>
      <h1 id="select-company-title" className="page-title">
        Select a company.
      </h1>
      <div className="company-picker">
        <label className="form-field">
          Company
          <select
            defaultValue=""
            onChange={(event) => {
              const companyId = Number(event.target.value)

              if (Number.isInteger(companyId)) {
                void selectCompany(companyId)
              }
            }}
          >
            <option value="" disabled>Choose company</option>
            {companies.map((company) => (
              <option key={company.id} value={company.id}>
                {company.name} - {company.role}
              </option>
            ))}
          </select>
        </label>
      </div>
    </section>
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
  const { activeCompany, isAuthenticated, isAuthResolved, isCurrentUserLoading } = useAuth()

  if (!isAuthResolved || isCurrentUserLoading) {
    return <AuthLoadingState />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (!hasRouteAccess(activeCompany?.role, policy)) {
    return <AccessDeniedPage />
  }

  return <Outlet />
}

export function CompanyRequiredRoute() {
  const { activeCompany, isAuthenticated, isAuthResolved, isCurrentUserLoading } = useAuth()

  if (!isAuthResolved || isCurrentUserLoading) {
    return <AuthLoadingState />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (!activeCompany) {
    return <CompanySelectionState />
  }

  return <Outlet key={activeCompany.id} />
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
