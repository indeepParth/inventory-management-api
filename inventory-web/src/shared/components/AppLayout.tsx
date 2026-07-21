import { useEffect, useMemo, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/AuthContext'
import { hasRouteAccess, type RoutePolicy } from '../../features/auth/roleAccess'

type NavItem = {
  label: string
  to: string
  policy: RoutePolicy
}

type NavGroup = {
  id: string
  label: string
  items: NavItem[]
}

const directNavItems: NavItem[] = [
  { label: 'Dashboard', to: '/app/dashboard', policy: 'allAuthenticated' },
  { label: 'Users', to: '/app/users', policy: 'adminOnly' },
  { label: 'Reports', to: '/app/reports', policy: 'viewReports' },
]

const navGroups: NavGroup[] = [
  {
    id: 'inventory',
    label: 'Inventory',
    items: [
      { label: 'Products', to: '/app/products', policy: 'readProducts' },
      { label: 'Categories', to: '/app/categories', policy: 'readProducts' },
      { label: 'Stock Movements', to: '/app/stock-movements', policy: 'viewStockMovements' },
    ],
  },
  {
    id: 'sales',
    label: 'Sales',
    items: [
      { label: 'Customers', to: '/app/customers', policy: 'readCustomers' },
      { label: 'Invoices', to: '/app/sales-invoices', policy: 'manageSalesInvoices' },
      { label: 'Delivery Challans', to: '/app/challans', policy: 'manageDeliveryChallans' },
      { label: 'Payments', to: '/app/payments', policy: 'viewPayments' },
      { label: 'Customer Returns', to: '/app/customer-returns', policy: 'manageCustomerReturns' },
      { label: 'Drivers', to: '/app/drivers', policy: 'readDrivers' },
    ],
  },
  {
    id: 'purchase',
    label: 'Purchase',
    items: [
      { label: 'Suppliers', to: '/app/suppliers', policy: 'readSuppliers' },
      { label: 'Purchases', to: '/app/purchases', policy: 'managePurchases' },
      { label: 'Supplier Returns', to: '/app/supplier-returns', policy: 'manageSupplierReturns' },
    ],
  },
]

function isPathInNavItem(pathname: string, item: NavItem): boolean {
  return pathname === item.to || pathname.startsWith(`${item.to}/`)
}

export function AppLayout() {
  const navigate = useNavigate()
  const location = useLocation()
  const { currentUser, isCurrentUserLoading, logout } = useAuth()
  const visibleDirectNavItems = directNavItems.filter((item) =>
    hasRouteAccess(currentUser?.roles ?? [], item.policy),
  )
  const visibleNavGroups = navGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) =>
        hasRouteAccess(currentUser?.roles ?? [], item.policy),
      ),
    }))
    .filter((group) => group.items.length > 0)
  const activeGroupId = useMemo(() => {
    return visibleNavGroups.find((group) =>
      group.items.some((item) => isPathInNavItem(location.pathname, item)),
    )?.id
  }, [location.pathname, visibleNavGroups])
  const [openGroupIds, setOpenGroupIds] = useState<string[]>(['inventory'])

  useEffect(() => {
    const groupIdToOpen = activeGroupId ?? 'inventory'

    setOpenGroupIds((current) =>
      current.includes(groupIdToOpen) ? current : [...current, groupIdToOpen],
    )
  }, [activeGroupId, location.pathname])

  function handleLogout(): void {
    logout()
    navigate('/login', { replace: true })
  }

  function toggleGroup(groupId: string): void {
    setOpenGroupIds((current) =>
      current.includes(groupId)
        ? current.filter((openGroupId) => openGroupId !== groupId)
        : [...current, groupId],
    )
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
          {visibleDirectNavItems.slice(0, 1).map((item) => (
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
          {visibleNavGroups.map((group) => {
            const isOpen = openGroupIds.includes(group.id)
            const isActive = activeGroupId === group.id
            const panelId = `sidebar-${group.id}-items`

            return (
              <div className="app-nav-group" key={group.id}>
                <button
                  aria-controls={panelId}
                  aria-expanded={isOpen}
                  className={isActive ? 'app-nav-group-button active' : 'app-nav-group-button'}
                  onClick={() => toggleGroup(group.id)}
                  type="button"
                >
                  <span>{group.label}</span>
                  <span className="app-nav-chevron" aria-hidden="true">
                    {isOpen ? 'v' : '>'}
                  </span>
                </button>
                {isOpen ? (
                  <div className="app-nav-children" id={panelId}>
                    {group.items.map((item) => (
                      <NavLink
                        className={({ isActive: isChildActive }) =>
                          isChildActive ? 'app-nav-link app-nav-child-link active' : 'app-nav-link app-nav-child-link'
                        }
                        key={item.to}
                        to={item.to}
                      >
                        {item.label}
                      </NavLink>
                    ))}
                  </div>
                ) : null}
              </div>
            )
          })}
          {visibleDirectNavItems.slice(1).map((item) => (
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
