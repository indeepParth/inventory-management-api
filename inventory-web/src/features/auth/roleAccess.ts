export type AppRole = 'Admin' | 'Manager' | 'Sales' | 'Inventory'

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

export const allRoles: AppRole[] = ['Admin', 'Manager', 'Sales', 'Inventory']

const policyRoles: Record<RoutePolicy, AppRole[]> = {
  allAuthenticated: allRoles,
  adminOnly: ['Admin'],
  adminOrManager: ['Admin', 'Manager'],
  readProducts: ['Admin', 'Manager', 'Sales', 'Inventory'],
  manageProducts: ['Admin', 'Manager'],
  readCustomers: ['Admin', 'Manager', 'Sales'],
  manageCustomers: ['Admin', 'Manager'],
  viewCustomerStatements: ['Admin', 'Manager', 'Sales'],
  readDrivers: ['Admin', 'Manager', 'Sales'],
  manageDrivers: ['Admin', 'Manager'],
  readSuppliers: ['Admin', 'Manager', 'Inventory'],
  manageSuppliers: ['Admin', 'Manager'],
  viewSupplierStatements: ['Admin', 'Manager'],
  managePurchases: ['Admin', 'Manager', 'Inventory'],
  manageDeliveryChallans: ['Admin', 'Manager', 'Sales'],
  manageSalesInvoices: ['Admin', 'Manager', 'Sales'],
  viewPayments: ['Admin', 'Manager'],
  viewStockMovements: ['Admin', 'Manager', 'Inventory'],
  manageCustomerReturns: ['Admin', 'Manager', 'Inventory'],
  manageSupplierReturns: ['Admin', 'Manager', 'Inventory'],
  viewReports: ['Admin', 'Manager'],
}

export function hasRouteAccess(userRoles: string[], policy: RoutePolicy): boolean {
  if (policy === 'allAuthenticated') {
    return true
  }

  if (userRoles.includes('Admin')) {
    return true
  }

  return policyRoles[policy].some((role) => userRoles.includes(role))
}
