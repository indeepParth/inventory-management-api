import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../features/auth/AuthContext'
import { ProtectedRoute, PublicOnlyRoute, RoleProtectedRoute } from '../features/auth/AuthRoutes'
import { AppLayout } from '../shared/components/AppLayout'
import { PublicLayout } from '../shared/components/PublicLayout'
import { CategoriesPage } from '../pages/CategoriesPage'
import { CustomerDetailPage } from '../pages/CustomerDetailPage'
import { CustomersPage } from '../pages/CustomersPage'
import { DashboardPage } from '../pages/DashboardPage'
import { DeliveryChallansPage } from '../pages/DeliveryChallansPage'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { PaymentsPage } from '../pages/PaymentsPage'
import { PlaceholderPage } from '../pages/PlaceholderPage'
import { ProductsPage } from '../pages/ProductsPage'
import { PurchasesPage } from '../pages/PurchasesPage'
import { SalesInvoicesPage } from '../pages/SalesInvoicesPage'
import { SupplierDetailPage } from '../pages/SupplierDetailPage'
import { SuppliersPage } from '../pages/SuppliersPage'
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
                <Route path="customers" element={<CustomersPage />} />
                <Route path="customers/:id" element={<CustomerDetailPage />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="readSuppliers" />}>
                <Route path="suppliers" element={<SuppliersPage />} />
                <Route path="suppliers/:id" element={<SupplierDetailPage />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="managePurchases" />}>
                <Route path="purchases" element={<PurchasesPage />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="manageDeliveryChallans" />}>
                <Route path="challans" element={<DeliveryChallansPage />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="manageSalesInvoices" />}>
                <Route path="sales-invoices" element={<SalesInvoicesPage />} />
              </Route>
              <Route element={<RoleProtectedRoute policy="viewPayments" />}>
                <Route path="payments" element={<PaymentsPage />} />
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
