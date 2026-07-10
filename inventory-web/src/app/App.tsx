import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../features/auth/AuthContext'
import { ProtectedRoute, PublicOnlyRoute, RoleProtectedRoute } from '../features/auth/AuthRoutes'
import { AppLayout } from '../shared/components/AppLayout'
import { PublicLayout } from '../shared/components/PublicLayout'
import { CategoriesPage } from '../pages/CategoriesPage'
import { DashboardPage } from '../pages/DashboardPage'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { PlaceholderPage } from '../pages/PlaceholderPage'
import { ProductsPage } from '../pages/ProductsPage'
import './App.css'

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<PublicLayout />}>
            <Route path="/" element={<HomePage />} />
            <Route element={<PublicOnlyRoute />}>
              <Route path="/login" element={<LoginPage />} />
            </Route>
          </Route>

          <Route element={<ProtectedRoute />}>
            <Route path="/app" element={<AppLayout />}>
              <Route index element={<Navigate to="/app/dashboard" replace />} />
              <Route element={<RoleProtectedRoute policy="allAuthenticated" />}>
                <Route path="dashboard" element={<DashboardPage />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="readProducts" />}>
                <Route path="products" element={<ProductsPage />} />
                <Route path="categories" element={<CategoriesPage />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="readCustomers" />}>
                <Route path="customers" element={<PlaceholderPage title="Customers" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="readSuppliers" />}>
                <Route path="suppliers" element={<PlaceholderPage title="Suppliers" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="managePurchases" />}>
                <Route path="purchases" element={<PlaceholderPage title="Purchases" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="manageDeliveryChallans" />}>
                <Route path="challans" element={<PlaceholderPage title="Challans" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="manageSalesInvoices" />}>
                <Route path="sales-invoices" element={<PlaceholderPage title="Sales invoices" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="viewPayments" />}>
                <Route path="payments" element={<PlaceholderPage title="Payments" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="viewStockMovements" />}>
                <Route path="stock-movements" element={<PlaceholderPage title="Stock movements" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="manageCustomerReturns" />}>
                <Route path="customer-returns" element={<PlaceholderPage title="Customer returns" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="manageSupplierReturns" />}>
                <Route path="supplier-returns" element={<PlaceholderPage title="Supplier returns" />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="viewReports" />}>
                <Route path="reports" element={<PlaceholderPage title="Reports" />} />
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
