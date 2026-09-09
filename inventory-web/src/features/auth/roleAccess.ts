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

export const allRoles: AppRole[] = ['Owner', 'Manager', 'Sales', 'Inventory']

const policyRoles: Record<RoutePolicy, AppRole[]> = {
  allAuthenticated: allRoles,
  adminOnly: ['Owner'],
  adminOrManager: ['Owner', 'Manager'],
  readProducts: ['Owner', 'Manager', 'Sales', 'Inventory'],
  manageProducts: ['Owner', 'Manager'],
  readCustomers: ['Owner', 'Manager', 'Sales'],
  manageCustomers: ['Owner', 'Manager'],
  viewCustomerStatements: ['Owner', 'Manager', 'Sales'],
  readDrivers: ['Owner', 'Manager', 'Sales'],
  manageDrivers: ['Owner', 'Manager'],
  readSuppliers: ['Owner', 'Manager', 'Inventory'],
  manageSuppliers: ['Owner', 'Manager'],
  viewSupplierStatements: ['Owner', 'Manager'],
  managePurchases: ['Owner', 'Manager', 'Inventory'],
  manageDeliveryChallans: ['Owner', 'Manager', 'Sales'],
  manageSalesInvoices: ['Owner', 'Manager', 'Sales'],
  viewPayments: ['Owner', 'Manager'],
  viewStockMovements: ['Owner', 'Manager', 'Inventory'],
  manageCustomerReturns: ['Owner', 'Manager', 'Inventory'],
  manageSupplierReturns: ['Owner', 'Manager', 'Inventory'],
  viewReports: ['Owner', 'Manager'],
}

export function hasRouteAccess(companyRole: string | undefined, policy: RoutePolicy): boolean {
  if (policy === 'allAuthenticated') {
    return true
  }

  if (companyRole === 'Owner') {
    return true
  }

  return policyRoles[policy].some((role) => role === companyRole)
}
