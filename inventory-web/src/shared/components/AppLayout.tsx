import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/AuthContext'

type Role = 'Admin' | 'Manager' | 'Sales' | 'Inventory'

type NavItem = {
  label: string
  to: string
  roles: Role[]
}

const allRoles: Role[] = ['Admin', 'Manager', 'Sales', 'Inventory']

const navItems: NavItem[] = [
  { label: 'Dashboard', to: '/app/dashboard', roles: allRoles },
  { label: 'Products', to: '/app/products', roles: ['Admin', 'Inventory'] },
  { label: 'Categories', to: '/app/categories', roles: ['Admin', 'Inventory'] },
  { label: 'Customers', to: '/app/customers', roles: ['Admin', 'Sales'] },
  { label: 'Suppliers', to: '/app/suppliers', roles: ['Admin', 'Inventory'] },
  { label: 'Purchases', to: '/app/purchases', roles: ['Admin', 'Manager', 'Inventory'] },
  { label: 'Challans', to: '/app/challans', roles: ['Admin', 'Manager', 'Sales'] },
  { label: 'Invoices', to: '/app/sales-invoices', roles: ['Admin', 'Manager', 'Sales'] },
  { label: 'Payments', to: '/app/payments', roles: ['Admin', 'Manager', 'Sales'] },
  { label: 'Stock movements', to: '/app/stock-movements', roles: ['Admin', 'Inventory'] },
  { label: 'Customer returns', to: '/app/customer-returns', roles: ['Admin', 'Sales'] },
  { label: 'Supplier returns', to: '/app/supplier-returns', roles: ['Admin', 'Inventory'] },
  { label: 'Reports', to: '/app/reports', roles: ['Admin', 'Manager'] },
]

function canShowNavItem(userRoles: string[], item: NavItem): boolean {
  if (userRoles.includes('Admin')) {
    return true
  }

  return item.roles.some((role) => userRoles.includes(role))
}

export function AppLayout() {
  const navigate = useNavigate()
  const { currentUser, isCurrentUserLoading, logout } = useAuth()
  const visibleNavItems = navItems.filter((item) =>
    canShowNavItem(currentUser?.roles ?? [], item),
  )

  function handleLogout(): void {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="app-layout">
      <aside className="app-sidebar" aria-label="Application navigation">
        <p className="app-brand">Inventory Web</p>
        <div className="app-user">
          <span className="app-user-name">
            {isCurrentUserLoading ? 'Loading user...' : currentUser?.username ?? 'Unknown user'}
          </span>
          <span className="app-user-email">{currentUser?.email ?? 'Email not available'}</span>
        </div>
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
