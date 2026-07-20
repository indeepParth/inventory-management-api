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

export type SalesInvoiceStatus = 0 | 1 | 2 | 3 | 4

export type SalesInvoiceItem = {
  id: number
  productId: number
  productName: string
  productSku: string
  quantity: number
  sellingUnitPrice: number
  taxRate: number
  taxAmount: number
  lineTotal: number
  costAtSale?: number | null
  deliveryChallanItemId?: number | null
  deliveryChallanId?: number | null
  deliveryChallanNumber?: string | null
}

export type SalesInvoice = {
  id: number
  invoiceNumber: string
  customerId: number
  customerName: string
  invoiceDate: string
  status: SalesInvoiceStatus
  subtotal: number
  discount: number
  taxAmount: number
  otherCharges: number
  grandTotal: number
  amountPaid: number
  balanceDue: number
  notes?: string
  createdAtUtc: string
  updatedAtUtc: string
  postedAtUtc?: string
  cancelledAtUtc?: string
  createdBy: string
  items: SalesInvoiceItem[]
}

export type SalesInvoiceSourceChallan = {
  id: number
  challanNumber: string
}

export type SalesInvoicePayment = {
  id: number
  receiptNumber: string
  paymentDate: string
  amount: number
  method: 0 | 1 | 2 | 3 | 4
  externalReference?: string
  note?: string
  createdAtUtc: string
  createdBy: string
  reversesPaymentId?: number
  reversalPaymentId?: number
}

export type SalesInvoiceCustomerReturn = {
  id: number
  returnNumber: string
  returnDate: string
  status: 0 | 1 | 2
  subtotal: number
  taxAmount: number
  grandTotal: number
  createdAtUtc: string
  updatedAtUtc: string
  postedAtUtc?: string
  cancelledAtUtc?: string
  createdBy: string
}

export type SalesInvoiceDetail = SalesInvoice & {
  sourceChallans: SalesInvoiceSourceChallan[]
  payments: SalesInvoicePayment[]
  customerReturns: SalesInvoiceCustomerReturn[]
}

export type DirectInvoiceItemFormValues = {
  productId: number
  quantity: number
  sellingUnitPrice: number
  taxRate: number
}

export type DirectInvoiceFormValues = {
  invoiceNumber: string
  customerId: number
  invoiceDate: string
  discount: number
  otherCharges: number
  notes: string
  items: DirectInvoiceItemFormValues[]
}

export type ChallanInvoiceItemFormValues = {
  deliveryChallanItemId: number
  sellingUnitPrice: number
  taxRate: number
}

export type ChallanInvoiceFormValues = {
  invoiceNumber: string
  invoiceDate: string
  discount: number
  otherCharges: number
  notes: string
  items: ChallanInvoiceItemFormValues[]
}

export type SalesInvoiceFilters = {
  pageNumber: number
  pageSize: number
  customerId: string
  status: string
  invoiceNumber: string
  dateFrom?: string
  dateTo?: string
}

function buildInvoiceQuery(filters: SalesInvoiceFilters): string {
  const query = new URLSearchParams({
    PageNumber: filters.pageNumber.toString(),
    PageSize: filters.pageSize.toString(),
  })

  if (filters.customerId) {
    query.set('CustomerId', filters.customerId)
  }

  if (filters.status) {
    query.set('Status', filters.status)
  }

  if (filters.invoiceNumber) {
    query.set('InvoiceNumber', filters.invoiceNumber)
  }

  if (filters.dateFrom) {
    query.set('DateFrom', filters.dateFrom)
  }

  if (filters.dateTo) {
    query.set('DateTo', filters.dateTo)
  }

  return query.toString()
}

export function getSalesInvoices(
  filters: SalesInvoiceFilters,
): Promise<PagedResponse<SalesInvoice>> {
  return apiRequest<PagedResponse<SalesInvoice>>(
    `/api/sales-invoices?${buildInvoiceQuery(filters)}`,
  )
}

export function getSalesInvoice(id: number): Promise<SalesInvoiceDetail> {
  return apiRequest<SalesInvoiceDetail>(`/api/sales-invoices/${id}`)
}

export function createDirectInvoice(values: DirectInvoiceFormValues): Promise<SalesInvoice> {
  return apiRequest<SalesInvoice, DirectInvoiceFormValues>('/api/sales-invoices', {
    method: 'POST',
    body: values,
  })
}

export function updateDirectInvoice(
  id: number,
  values: DirectInvoiceFormValues,
): Promise<SalesInvoice> {
  return apiRequest<SalesInvoice, DirectInvoiceFormValues>(`/api/sales-invoices/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function createInvoiceFromChallans(
  values: ChallanInvoiceFormValues,
): Promise<SalesInvoice> {
  return apiRequest<SalesInvoice, ChallanInvoiceFormValues>(
    '/api/sales-invoices/from-challans',
    {
      method: 'POST',
      body: values,
    },
  )
}

export function postSalesInvoice(id: number): Promise<SalesInvoice> {
  return apiRequest<SalesInvoice>(`/api/sales-invoices/${id}/post`, {
    method: 'POST',
  })
}

export function cancelSalesInvoice(id: number): Promise<SalesInvoice> {
  return apiRequest<SalesInvoice>(`/api/sales-invoices/${id}/cancel`, {
    method: 'POST',
  })
}

export function getSalesInvoiceStatusLabel(status: SalesInvoiceStatus): string {
  const labels: Record<SalesInvoiceStatus, string> = {
    0: 'Draft',
    1: 'Posted',
    2: 'Partially paid',
    3: 'Paid',
    4: 'Cancelled',
  }

  return labels[status] ?? 'Unknown'
}
