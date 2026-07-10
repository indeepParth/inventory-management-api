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

export type DeliveryChallanStatus = 0 | 1 | 2 | 3

export type DeliveryChallanItem = {
  id: number
  productId: number
  productName: string
  productSku: string
  quantity: number
}

export type DeliveryChallan = {
  id: number
  challanNumber: string
  customerId: number
  customerName: string
  challanDate: string
  status: DeliveryChallanStatus
  vehicleNumber?: string
  driverName?: string
  deliveryAddress: string
  notes?: string
  createdAtUtc: string
  updatedAtUtc: string
  postedAtUtc?: string
  cancelledAtUtc?: string
  invoicedAtUtc?: string
  createdBy: string
  items: DeliveryChallanItem[]
}

export type DeliveryChallanItemFormValues = {
  productId: number
  quantity: number
}

export type DeliveryChallanFormValues = {
  challanNumber: string
  customerId: number
  challanDate: string
  vehicleNumber: string
  driverName: string
  deliveryAddress: string
  notes: string
  items: DeliveryChallanItemFormValues[]
}

export type DeliveryChallanFilters = {
  pageNumber: number
  pageSize: number
  customerId: string
  status: string
  challanNumber: string
}

function buildChallanQuery(filters: DeliveryChallanFilters): string {
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

  if (filters.challanNumber) {
    query.set('ChallanNumber', filters.challanNumber)
  }

  return query.toString()
}

export function getDeliveryChallans(
  filters: DeliveryChallanFilters,
): Promise<PagedResponse<DeliveryChallan>> {
  return apiRequest<PagedResponse<DeliveryChallan>>(
    `/api/delivery-challans?${buildChallanQuery(filters)}`,
  )
}

export function createDeliveryChallan(
  values: DeliveryChallanFormValues,
): Promise<DeliveryChallan> {
  return apiRequest<DeliveryChallan, DeliveryChallanFormValues>('/api/delivery-challans', {
    method: 'POST',
    body: values,
  })
}

export function updateDeliveryChallan(
  id: number,
  values: DeliveryChallanFormValues,
): Promise<DeliveryChallan> {
  return apiRequest<DeliveryChallan, DeliveryChallanFormValues>(
    `/api/delivery-challans/${id}`,
    {
      method: 'PUT',
      body: values,
    },
  )
}

export function postDeliveryChallan(id: number): Promise<DeliveryChallan> {
  return apiRequest<DeliveryChallan>(`/api/delivery-challans/${id}/post`, {
    method: 'POST',
  })
}

export function cancelDeliveryChallan(id: number): Promise<DeliveryChallan> {
  return apiRequest<DeliveryChallan>(`/api/delivery-challans/${id}/cancel`, {
    method: 'POST',
  })
}

export function getDeliveryChallanStatusLabel(status: DeliveryChallanStatus): string {
  const labels: Record<DeliveryChallanStatus, string> = {
    0: 'Draft',
    1: 'Posted',
    2: 'Cancelled',
    3: 'Invoiced',
  }

  return labels[status] ?? 'Unknown'
}
