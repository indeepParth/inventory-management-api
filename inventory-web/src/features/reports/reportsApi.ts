import { apiRequest } from '../../shared/api/apiClient'

export type PagedResponse<T> = {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type RegisterSummary = {
  documentCount: number
  totalQuantity: number
  subtotal: number
  discount: number
  taxAmount: number
  otherCharges: number
  grandTotal: number
  paidAmount: number
  outstandingAmount: number
}

export type RegisterResponse<T> = PagedResponse<T> & {
  summary: RegisterSummary
}

export type CurrentStockItem = {
  productId: number
  productName: string
  category: string
  unit: number
  quantity: number
  averageCost: number
  stockValue: number
  defaultSellingPrice: number
}

export type PurchaseRegisterItem = {
  purchaseId: number
  purchaseNumber: string
  date: string
  supplierId: number
  supplierName: string
  status: number
  totalQuantity: number
  subtotal: number
  discount: number
  taxAmount: number
  otherCharges: number
  grandTotal: number
  paidAmount: number
  outstandingAmount: number
}

export type SalesRegisterItem = {
  salesInvoiceId: number
  invoiceNumber: string
  date: string
  customerId: number
  customerName: string
  sourceType: number
  status: number
  totalQuantity: number
  subtotal: number
  discount: number
  taxAmount: number
  otherCharges: number
  grandTotal: number
  paidAmount: number
  outstandingAmount: number
}

export type GrossProfitValues = {
  soldQuantity: number
  returnedQuantity: number
  netQuantity: number
  revenue: number
  returns: number
  netRevenue: number
  costOfGoodsSold: number
  returnedCost: number
  netCostOfGoodsSold: number
  grossProfit: number
  grossMarginPercentage: number
}

export type ProductGrossProfit = GrossProfitValues & {
  productId: number
  productName: string
}

export type GrossProfitReport = {
  reportName: string
  summary: GrossProfitValues
  byProduct: ProductGrossProfit[]
}

export type ReportFilters = {
  pageNumber: number
  pageSize: number
  fromDate: string
  toDate: string
}

function buildPagedQuery(filters: ReportFilters): string {
  const query = new URLSearchParams({
    PageNumber: filters.pageNumber.toString(),
    PageSize: filters.pageSize.toString(),
  })

  if (filters.fromDate) {
    query.set('FromDate', filters.fromDate)
  }

  if (filters.toDate) {
    query.set('ToDate', filters.toDate)
  }

  return query.toString()
}

export function getCurrentStock(
  pageNumber: number,
  pageSize: number,
): Promise<PagedResponse<CurrentStockItem>> {
  return apiRequest<PagedResponse<CurrentStockItem>>(
    `/api/inventory-reports/current-stock?PageNumber=${pageNumber}&PageSize=${pageSize}`,
  )
}

export function getPurchaseRegister(
  filters: ReportFilters,
): Promise<RegisterResponse<PurchaseRegisterItem>> {
  return apiRequest<RegisterResponse<PurchaseRegisterItem>>(
    `/api/inventory-reports/purchase-register?${buildPagedQuery(filters)}`,
  )
}

export function getSalesRegister(
  filters: ReportFilters,
): Promise<RegisterResponse<SalesRegisterItem>> {
  return apiRequest<RegisterResponse<SalesRegisterItem>>(
    `/api/inventory-reports/sales-register?${buildPagedQuery(filters)}`,
  )
}

export function getGrossProfit(fromDate: string, toDate: string): Promise<GrossProfitReport> {
  const query = new URLSearchParams()

  if (fromDate) {
    query.set('FromDate', fromDate)
  }

  if (toDate) {
    query.set('ToDate', toDate)
  }

  const queryString = query.toString()
  return apiRequest<GrossProfitReport>(
    `/api/inventory-reports/gross-profit${queryString ? `?${queryString}` : ''}`,
  )
}
