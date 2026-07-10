import { apiRequest } from '../../shared/api/apiClient'

export type ReturnStatus = 0 | 1 | 2

export type CustomerReturnItem = {
  id: number
  salesInvoiceItemId: number
  productId: number
  productName: string
  productSku: string
  quantity: number
  sellingUnitPrice: number
  taxRate: number
  taxAmount: number
  lineTotal: number
  costAtSale: number
}

export type CustomerReturn = {
  id: number
  returnNumber: string
  salesInvoiceId: number
  invoiceNumber: string
  customerId: number
  customerName: string
  returnDate: string
  status: ReturnStatus
  subtotal: number
  taxAmount: number
  grandTotal: number
  notes?: string
  postedAtUtc?: string
  cancelledAtUtc?: string
  items: CustomerReturnItem[]
}

export type SupplierReturnItem = {
  id: number
  purchaseItemId: number
  productId: number
  productName: string
  productSku: string
  quantity: number
  unitCost: number
  taxRate: number
  taxAmount: number
  lineTotal: number
}

export type SupplierReturn = {
  id: number
  returnNumber: string
  purchaseId: number
  purchaseNumber: string
  supplierId: number
  supplierName: string
  returnDate: string
  status: ReturnStatus
  subtotal: number
  taxAmount: number
  grandTotal: number
  notes?: string
  postedAtUtc?: string
  cancelledAtUtc?: string
  items: SupplierReturnItem[]
}

export type CustomerReturnFormValues = {
  returnNumber: string
  salesInvoiceId: number
  returnDate: string
  notes: string
  items: Array<{
    salesInvoiceItemId: number
    quantity: number
  }>
}

export type SupplierReturnFormValues = {
  returnNumber: string
  purchaseId: number
  returnDate: string
  notes: string
  items: Array<{
    purchaseItemId: number
    quantity: number
  }>
}

export function createCustomerReturn(values: CustomerReturnFormValues): Promise<CustomerReturn> {
  return apiRequest<CustomerReturn, CustomerReturnFormValues>('/api/customer-returns', {
    method: 'POST',
    body: values,
  })
}

export function postCustomerReturn(id: number): Promise<CustomerReturn> {
  return apiRequest<CustomerReturn>(`/api/customer-returns/${id}/post`, {
    method: 'POST',
  })
}

export function cancelCustomerReturn(id: number): Promise<CustomerReturn> {
  return apiRequest<CustomerReturn>(`/api/customer-returns/${id}/cancel`, {
    method: 'POST',
  })
}

export function createSupplierReturn(values: SupplierReturnFormValues): Promise<SupplierReturn> {
  return apiRequest<SupplierReturn, SupplierReturnFormValues>('/api/supplier-returns', {
    method: 'POST',
    body: values,
  })
}

export function postSupplierReturn(id: number): Promise<SupplierReturn> {
  return apiRequest<SupplierReturn>(`/api/supplier-returns/${id}/post`, {
    method: 'POST',
  })
}

export function cancelSupplierReturn(id: number): Promise<SupplierReturn> {
  return apiRequest<SupplierReturn>(`/api/supplier-returns/${id}/cancel`, {
    method: 'POST',
  })
}

export function getReturnStatusLabel(status: ReturnStatus): string {
  const labels: Record<ReturnStatus, string> = {
    0: 'Draft',
    1: 'Posted',
    2: 'Cancelled',
  }

  return labels[status] ?? 'Unknown'
}
