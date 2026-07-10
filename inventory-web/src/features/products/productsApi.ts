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

export type Category = {
  id: number
  name: string
  description: string
  isActive: boolean
  createdAt: string
}

export type UnitOfMeasure = 1 | 2 | 3 | 4 | 5 | 6

export type Product = {
  id: number
  name: string
  sku: string
  quantity: number
  baseUnit: string
  defaultSellingPrice: number
  averageCost: number
  categoryId: number
  categoryName: string
}

export type CategoryFormValues = {
  name: string
  description: string
  isActive: boolean
}

export type ProductFormValues = {
  name: string
  sku: string
  baseUnit: UnitOfMeasure
  defaultSellingPrice: number
  categoryId: number
}

type DeleteResponse = {
  id: number
  message: string
}

export function getCategories(): Promise<Category[]> {
  return apiRequest<Category[]>('/api/Categories')
}

export function createCategory(values: Omit<CategoryFormValues, 'isActive'>): Promise<Category> {
  return apiRequest<Category, Omit<CategoryFormValues, 'isActive'>>('/api/Categories', {
    method: 'POST',
    body: values,
  })
}

export function updateCategory(id: number, values: CategoryFormValues): Promise<Category> {
  return apiRequest<Category, CategoryFormValues>(`/api/Categories/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function deleteCategory(id: number): Promise<DeleteResponse> {
  return apiRequest<DeleteResponse>(`/api/Categories/${id}`, {
    method: 'DELETE',
  })
}

export function getProducts(
  pageNumber: number,
  pageSize: number,
  search?: string,
): Promise<PagedResponse<Product>> {
  const query = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  })

  if (search) {
    query.set('Search', search)
  }

  return apiRequest<PagedResponse<Product>>(`/api/Products?${query.toString()}`)
}

export function createProduct(values: ProductFormValues): Promise<Product> {
  return apiRequest<Product, ProductFormValues>('/api/Products', {
    method: 'POST',
    body: values,
  })
}

export function updateProduct(id: number, values: ProductFormValues): Promise<Product> {
  return apiRequest<Product, ProductFormValues>(`/api/Products/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function deleteProduct(id: number): Promise<DeleteResponse> {
  return apiRequest<DeleteResponse>(`/api/Products/${id}`, {
    method: 'DELETE',
  })
}
