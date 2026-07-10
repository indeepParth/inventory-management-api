import { NavLink, Outlet } from 'react-router-dom'

const navItems = [
  { label: 'Dashboard', to: '/app/dashboard' },
  { label: 'Products', to: '/app/products' },
  { label: 'Purchases', to: '/app/purchases' },
  { label: 'Sales invoices', to: '/app/sales-invoices' },
  { label: 'Reports', to: '/app/reports' },
]

export function AppLayout() {
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
      </aside>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
