import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/AuthContext'
import { hasRouteAccess, type RoutePolicy } from '../../features/auth/roleAccess'

type NavItem = {
  label: string
  to: string
  policy: RoutePolicy
}

const navItems: NavItem[] = [
  { label: 'Dashboard', to: '/app/dashboard', policy: 'allAuthenticated' },
  { label: 'Users', to: '/app/users', policy: 'adminOnly' },
  { label: 'Products', to: '/app/products', policy: 'readProducts' },
  { label: 'Categories', to: '/app/categories', policy: 'readProducts' },
  { label: 'Customers', to: '/app/customers', policy: 'readCustomers' },
  { label: 'Drivers', to: '/app/drivers', policy: 'readDrivers' },
  { label: 'Suppliers', to: '/app/suppliers', policy: 'readSuppliers' },
  { label: 'Purchases', to: '/app/purchases', policy: 'managePurchases' },
  { label: 'Challans', to: '/app/challans', policy: 'manageDeliveryChallans' },
  { label: 'Invoices', to: '/app/sales-invoices', policy: 'manageSalesInvoices' },
  { label: 'Payments', to: '/app/payments', policy: 'viewPayments' },
  { label: 'Stock movements', to: '/app/stock-movements', policy: 'viewStockMovements' },
  { label: 'Customer returns', to: '/app/customer-returns', policy: 'manageCustomerReturns' },
  { label: 'Supplier returns', to: '/app/supplier-returns', policy: 'manageSupplierReturns' },
  { label: 'Reports', to: '/app/reports', policy: 'viewReports' },
]

export function AppLayout() {
  const navigate = useNavigate()
  const { currentUser, isCurrentUserLoading, logout } = useAuth()
  const visibleNavItems = navItems.filter((item) =>
    hasRouteAccess(currentUser?.roles ?? [], item.policy),
  )

  function handleLogout(): void {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="app-layout">
      <aside className="app-sidebar" aria-label="Application navigation">
        <p className="app-brand">Inventory Web</p>
        <NavLink className="app-user" to="/app/profile">
          <span className="app-user-name">
            {isCurrentUserLoading ? 'Loading user...' : currentUser?.username ?? 'Unknown user'}
          </span>
          <span className="app-user-email">{currentUser?.email ?? 'Email not available'}</span>
        </NavLink>
        <nav className="app-nav">
          {visibleNavItems.map((item) => (
            <NavLink
              className={({ isActive }) =>
                isActive ? 'app-nav-link active' : 'app-nav-link'
              }
              key={item.to}
              to={item.to}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <button className="logout-button" onClick={handleLogout} type="button">
          Logout
        </button>
      </aside>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
