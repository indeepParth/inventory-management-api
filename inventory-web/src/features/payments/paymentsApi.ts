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

export type PaymentMethod = 0 | 1 | 2 | 3 | 4

export type Payment = {
  id: number
  receiptNumber: string
  customerId?: number
  customerName?: string
  salesInvoiceId?: number
  invoiceNumber?: string
  supplierId?: number
  supplierName?: string
  purchaseId?: number
  purchaseNumber?: string
  paymentDate: string
  amount: number
  method: PaymentMethod
  externalReference?: string
  note?: string
  createdAtUtc: string
  createdBy: string
  reversesPaymentId?: number
  reversalPaymentId?: number
}

export type PaymentFormValues = {
  receiptNumber: string
  customerId?: number
  salesInvoiceId?: number
  supplierId?: number
  purchaseId?: number
  paymentDate: string
  amount: number
  method: PaymentMethod
  externalReference: string
  note: string
}

export type ReversePaymentValues = {
  receiptNumber: string
  paymentDate: string
  externalReference: string
  note: string
}

export type PaymentFilters = {
  pageNumber: number
  pageSize: number
  receiptNumber: string
  method: string
}

function buildPaymentQuery(filters: PaymentFilters): string {
  const query = new URLSearchParams({
    PageNumber: filters.pageNumber.toString(),
    PageSize: filters.pageSize.toString(),
  })

  if (filters.receiptNumber) {
    query.set('ReceiptNumber', filters.receiptNumber)
  }

  if (filters.method) {
    query.set('Method', filters.method)
  }

  return query.toString()
}

export function getPayments(filters: PaymentFilters): Promise<PagedResponse<Payment>> {
  return apiRequest<PagedResponse<Payment>>(`/api/payments?${buildPaymentQuery(filters)}`)
}

export function createPayment(values: PaymentFormValues): Promise<Payment> {
  return apiRequest<Payment, PaymentFormValues>('/api/payments', {
    method: 'POST',
    body: values,
  })
}

export function reversePayment(id: number, values: ReversePaymentValues): Promise<Payment> {
  return apiRequest<Payment, ReversePaymentValues>(`/api/payments/${id}/reverse`, {
    method: 'POST',
    body: values,
  })
}

export function getPaymentMethodLabel(method: PaymentMethod): string {
  const labels: Record<PaymentMethod, string> = {
    0: 'Cash',
    1: 'Bank transfer',
    2: 'Cheque',
    3: 'UPI',
    4: 'Other',
  }

  return labels[method] ?? 'Unknown'
}
