import { apiRequest } from '../../shared/api/apiClient'
import type { DeliveryChallanStatus } from '../challans/challansApi'

export type PagedResponse<T> = {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type Driver = {
  id: number
  name: string
  phone?: string
  licenseNumber?: string
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export type DriverFormValues = {
  name: string
  phone: string
  licenseNumber: string
  isActive: boolean
}

export type DriverDeliveryPaymentStatus = 'all' | 'paid' | 'unpaid'

export type DriverDeliveryRow = {
  challanId: number
  challanNumber: string
  challanDate: string
  status: DeliveryChallanStatus
  customerName: string
  deliveryFromAddress: string
  deliveryToAddress: string
  vehicleNumber?: string
  deliveryCharge: number
  isDeliveryChargePaid: boolean
  itemCount: number
}

export type DriverDeliveriesResponse = Driver & {
  deliveries: PagedResponse<DriverDeliveryRow>
}

export type DriverDeliveriesFilters = {
  pageNumber: number
  pageSize: number
  dateFrom: string
  dateTo: string
  paymentStatus: DriverDeliveryPaymentStatus
}

function buildListQuery(
  pageNumber: number,
  pageSize: number,
  search: string,
  isActive: string,
): string {
  const query = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  })

  if (search) {
    query.set('Search', search)
  }

  if (isActive) {
    query.set('IsActive', isActive)
  }

  return query.toString()
}

function buildDeliveriesQuery(filters: DriverDeliveriesFilters): string {
  const query = new URLSearchParams({
    PageNumber: filters.pageNumber.toString(),
    PageSize: filters.pageSize.toString(),
    PaymentStatus: filters.paymentStatus,
  })

  if (filters.dateFrom) {
    query.set('DateFrom', filters.dateFrom)
  }

  if (filters.dateTo) {
    query.set('DateTo', filters.dateTo)
  }

  return query.toString()
}

export function getDrivers(
  pageNumber: number,
  pageSize: number,
  search: string,
  isActive: string,
): Promise<PagedResponse<Driver>> {
  return apiRequest<PagedResponse<Driver>>(
    `/api/drivers?${buildListQuery(pageNumber, pageSize, search, isActive)}`,
  )
}

export function getDriver(id: number): Promise<Driver> {
  return apiRequest<Driver>(`/api/drivers/${id}`)
}

export function getDriverDeliveries(
  driverId: number,
  filters: DriverDeliveriesFilters,
): Promise<DriverDeliveriesResponse> {
  return apiRequest<DriverDeliveriesResponse>(
    `/api/drivers/${driverId}/deliveries?${buildDeliveriesQuery(filters)}`,
  )
}

export function createDriver(values: Omit<DriverFormValues, 'isActive'>): Promise<Driver> {
  return apiRequest<Driver, Omit<DriverFormValues, 'isActive'>>('/api/drivers', {
    method: 'POST',
    body: values,
  })
}

export function updateDriver(id: number, values: DriverFormValues): Promise<Driver> {
  return apiRequest<Driver, DriverFormValues>(`/api/drivers/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function deactivateDriver(id: number): Promise<Driver> {
  return apiRequest<Driver>(`/api/drivers/${id}/deactivate`, {
    method: 'PATCH',
  })
}
