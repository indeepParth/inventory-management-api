import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/AuthContext'

const navItems = [
  { label: 'Dashboard', to: '/app/dashboard' },
  { label: 'Products', to: '/app/products' },
  { label: 'Purchases', to: '/app/purchases' },
  { label: 'Sales invoices', to: '/app/sales-invoices' },
  { label: 'Reports', to: '/app/reports' },
]

export function AppLayout() {
  const navigate = useNavigate()
  const { logout } = useAuth()

  function handleLogout(): void {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="app-layout">
      <aside className="app-sidebar" aria-label="Application navigation">
        <p className="app-brand">Inventory Web</p>
        <nav className="app-nav">
          {navItems.map((item) => (
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
