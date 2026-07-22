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

export type Customer = {
  id: number
  name: string
  contactPerson?: string
  phone?: string
  email?: string
  billingAddress?: string
  deliveryAddress?: string
  gstNumber?: string
  creditLimit: number
  balanceDue: number
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export type Supplier = {
  id: number
  name: string
  contactPerson?: string
  email?: string
  phone?: string
  address?: string
  gstNumber?: string
  isActive: boolean
  createdAt: string
}

export type StatementEntryType = 0 | 1 | 2 | 3

export type StatementEntry = {
  type: StatementEntryType
  transactionId: number
  transactionDate: string
  timestampUtc: string
  referenceNumber: string
  externalReference?: string
  note?: string
  amount: number
  balanceChange: number
  runningBalance: number
}

export type StatementResponse = {
  partyId: number
  dateFrom: string
  dateTo: string
  openingBalance: number
  closingBalance: number
  totalCharges: number
  totalPayments: number
  totalReversals: number
  entries: StatementEntry[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type StatementFilters = {
  dateFrom: string
  dateTo: string
  pageNumber: number
  pageSize: number
}

export type CustomerFormValues = {
  name: string
  contactPerson: string
  phone: string
  email: string
  billingAddress: string
  deliveryAddress: string
  gstNumber: string
  creditLimit: number
  isActive: boolean
}

export type SupplierFormValues = {
  name: string
  contactPerson: string
  email: string
  phone: string
  address: string
  gstNumber: string
  isActive: boolean
}

type DeleteResponse = {
  id: number
  message: string
}

function buildListQuery(pageNumber: number, pageSize: number, search: string, isActive: string): string {
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

function buildStatementQuery(filters: StatementFilters): string {
  const query = new URLSearchParams({
    DateFrom: filters.dateFrom,
    DateTo: filters.dateTo,
    PageNumber: filters.pageNumber.toString(),
    PageSize: filters.pageSize.toString(),
  })

  return query.toString()
}

export function getCustomers(
  pageNumber: number,
  pageSize: number,
  search: string,
  isActive: string,
): Promise<PagedResponse<Customer>> {
  return apiRequest<PagedResponse<Customer>>(
    `/api/Customers?${buildListQuery(pageNumber, pageSize, search, isActive)}`,
  )
}

export function getCustomer(id: number): Promise<Customer> {
  return apiRequest<Customer>(`/api/Customers/${id}`)
}

export function getCustomerStatement(
  id: number,
  filters: StatementFilters,
): Promise<StatementResponse> {
  return apiRequest<StatementResponse>(
    `/api/Customers/${id}/statement?${buildStatementQuery(filters)}`,
  )
}

export function createCustomer(values: Omit<CustomerFormValues, 'isActive'>): Promise<Customer> {
  return apiRequest<Customer, Omit<CustomerFormValues, 'isActive'>>('/api/Customers', {
    method: 'POST',
    body: values,
  })
}

export function updateCustomer(id: number, values: CustomerFormValues): Promise<Customer> {
  return apiRequest<Customer, CustomerFormValues>(`/api/Customers/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function deactivateCustomer(id: number): Promise<Customer> {
  return apiRequest<Customer>(`/api/Customers/${id}/deactivate`, {
    method: 'PATCH',
  })
}

export function getSuppliers(
  pageNumber: number,
  pageSize: number,
  search: string,
  isActive: string,
): Promise<PagedResponse<Supplier>> {
  return apiRequest<PagedResponse<Supplier>>(
    `/api/Suppliers?${buildListQuery(pageNumber, pageSize, search, isActive)}`,
  )
}

export function getSupplier(id: number): Promise<Supplier> {
  return apiRequest<Supplier>(`/api/Suppliers/${id}`)
}

export function getSupplierStatement(
  id: number,
  filters: StatementFilters,
): Promise<StatementResponse> {
  return apiRequest<StatementResponse>(
    `/api/Suppliers/${id}/statement?${buildStatementQuery(filters)}`,
  )
}

export function createSupplier(values: Omit<SupplierFormValues, 'isActive'>): Promise<Supplier> {
  return apiRequest<Supplier, Omit<SupplierFormValues, 'isActive'>>('/api/Suppliers', {
    method: 'POST',
    body: values,
  })
}

export function updateSupplier(id: number, values: SupplierFormValues): Promise<Supplier> {
  return apiRequest<Supplier, SupplierFormValues>(`/api/Suppliers/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function deleteSupplier(id: number): Promise<DeleteResponse> {
  return apiRequest<DeleteResponse>(`/api/Suppliers/${id}`, {
    method: 'DELETE',
  })
}
