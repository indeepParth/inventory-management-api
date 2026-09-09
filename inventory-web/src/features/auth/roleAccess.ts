import type { CompanyRole } from './authApi'

export type AppRole = CompanyRole

export type RoutePolicy =
  | 'allAuthenticated'
  | 'adminOnly'
  | 'adminOrManager'
  | 'readProducts'
  | 'manageProducts'
  | 'readCustomers'
  | 'manageCustomers'
  | 'viewCustomerStatements'
  | 'readDrivers'
  | 'manageDrivers'
  | 'readSuppliers'
  | 'manageSuppliers'
  | 'viewSupplierStatements'
  | 'managePurchases'
  | 'manageDeliveryChallans'
  | 'manageSalesInvoices'
  | 'viewPayments'
  | 'viewStockMovements'
  | 'manageCustomerReturns'
  | 'manageSupplierReturns'
  | 'viewReports'

export const allRoles: AppRole[] = ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer']

const policyRoles: Record<RoutePolicy, AppRole[]> = {
  allAuthenticated: allRoles,
  adminOnly: ['Owner', 'Admin'],
  adminOrManager: ['Owner', 'Admin', 'Manager'],
  readProducts: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  manageProducts: ['Owner', 'Admin', 'Manager'],
  readCustomers: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  manageCustomers: ['Owner', 'Admin', 'Manager'],
  viewCustomerStatements: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  readDrivers: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  manageDrivers: ['Owner', 'Admin', 'Manager'],
  readSuppliers: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  manageSuppliers: ['Owner', 'Admin', 'Manager'],
  viewSupplierStatements: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  managePurchases: ['Owner', 'Admin', 'Manager', 'Staff'],
  manageDeliveryChallans: ['Owner', 'Admin', 'Manager', 'Staff'],
  manageSalesInvoices: ['Owner', 'Admin', 'Manager', 'Staff'],
  viewPayments: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  viewStockMovements: ['Owner', 'Admin', 'Manager', 'Staff', 'Viewer'],
  manageCustomerReturns: ['Owner', 'Admin', 'Manager', 'Staff'],
  manageSupplierReturns: ['Owner', 'Admin', 'Manager', 'Staff'],
  viewReports: ['Owner', 'Admin', 'Manager', 'Viewer'],
}

export function hasRouteAccess(companyRole: string | undefined, policy: RoutePolicy): boolean {
  if (policy === 'allAuthenticated') {
    return true
  }

  if (companyRole === 'Owner' || companyRole === 'Admin') {
    return true
  }

  return policyRoles[policy].some((role) => role === companyRole)
}
