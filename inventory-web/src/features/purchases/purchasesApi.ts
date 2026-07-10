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

export type PurchaseStatus = 0 | 1 | 2 | 3 | 4

export type PurchaseItem = {
  id: number
  productId: number
  productName: string
  productSku: string
  quantity: number
  unitCost: number
  taxRate: number
  taxAmount: number
  lineTotal: number
}

export type Purchase = {
  id: number
  purchaseNumber: string
  supplierId: number
  supplierName: string
  supplierBillNumber?: string
  billDate: string
  status: PurchaseStatus
  subtotal: number
  discount: number
  taxAmount: number
  otherCharges: number
  grandTotal: number
  amountPaid: number
  balanceDue: number
  notes?: string
  createdAtUtc: string
  postedAtUtc?: string
  cancelledAtUtc?: string
  createdBy: string
  items: PurchaseItem[]
}

export type PurchaseItemFormValues = {
  productId: number
  quantity: number
  unitCost: number
  taxRate: number
}

export type PurchaseFormValues = {
  purchaseNumber: string
  supplierId: number
  supplierBillNumber: string
  billDate: string
  discount: number
  otherCharges: number
  notes: string
  items: PurchaseItemFormValues[]
}

export type PurchaseFilters = {
  pageNumber: number
  pageSize: number
  supplierId: string
  status: string
  purchaseNumber: string
  supplierBillNumber: string
}

function buildPurchaseQuery(filters: PurchaseFilters): string {
  const query = new URLSearchParams({
    PageNumber: filters.pageNumber.toString(),
    PageSize: filters.pageSize.toString(),
  })

  if (filters.supplierId) {
    query.set('SupplierId', filters.supplierId)
  }

  if (filters.status) {
    query.set('Status', filters.status)
  }

  if (filters.purchaseNumber) {
    query.set('PurchaseNumber', filters.purchaseNumber)
  }

  if (filters.supplierBillNumber) {
    query.set('SupplierBillNumber', filters.supplierBillNumber)
  }

  return query.toString()
}

export function getPurchases(filters: PurchaseFilters): Promise<PagedResponse<Purchase>> {
  return apiRequest<PagedResponse<Purchase>>(`/api/Purchases?${buildPurchaseQuery(filters)}`)
}

export function createPurchase(values: PurchaseFormValues): Promise<Purchase> {
  return apiRequest<Purchase, PurchaseFormValues>('/api/Purchases', {
    method: 'POST',
    body: values,
  })
}

export function updatePurchase(id: number, values: PurchaseFormValues): Promise<Purchase> {
  return apiRequest<Purchase, PurchaseFormValues>(`/api/Purchases/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function postPurchase(id: number): Promise<Purchase> {
  return apiRequest<Purchase>(`/api/Purchases/${id}/post`, {
    method: 'POST',
  })
}

export function cancelPurchase(id: number): Promise<Purchase> {
  return apiRequest<Purchase>(`/api/Purchases/${id}/cancel`, {
    method: 'POST',
  })
}

export function getPurchaseStatusLabel(status: PurchaseStatus): string {
  const labels: Record<PurchaseStatus, string> = {
    0: 'Draft',
    1: 'Posted',
    2: 'Cancelled',
    3: 'Partially paid',
    4: 'Paid',
  }

  return labels[status] ?? 'Unknown'
}
