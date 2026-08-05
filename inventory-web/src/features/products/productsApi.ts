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

export type Unit = {
  id: number
  name: string
  shortName: string | null
  factorToBaseUnit: number
  baseUnitId: number | null
  baseUnitName: string | null
  isActive: boolean
  createdAtUtc: string
}

export type Product = {
  id: number
  name: string
  sku: string
  quantity: number
  availableQuantity: number
  baseUnitId: number
  baseUnitName: string
  baseProductId: number | null
  baseProductName: string | null
  factorToBaseProduct: number | null
  isSubProduct: boolean
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

export type UnitFormValues = {
  name: string
  shortName: string
  factorToBaseUnit: number
  baseUnitId: number | null
  isActive: boolean
}

export type ProductUnitConversion = {
  id: number
  productId: number
  unitId: number
  unitName: string
  factorToBaseUnit: number
  isActive: boolean
  isBaseUnit: boolean
}

export type ProductFormValues = {
  name: string
  sku: string
  baseUnitId: number
  baseProductId: number | null
  factorToBaseProduct: number | null
  defaultSellingPrice: number
  categoryId: number
}

type DeleteResponse = {
  id: number
  message: string
}

export type ProductUnitConversionFormValues = {
  unitId: number
  factorToBaseUnit: number
}

export type ProductUnitConversionUpdateValues = {
  factorToBaseUnit: number
  isActive: boolean
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

export function getUnits(): Promise<Unit[]> {
  return apiRequest<Unit[]>('/api/Units')
}

export function createUnit(values: Omit<UnitFormValues, 'isActive'>): Promise<Unit> {
  return apiRequest<Unit, Omit<UnitFormValues, 'isActive'>>('/api/Units', {
    method: 'POST',
    body: values,
  })
}

export function updateUnit(id: number, values: UnitFormValues): Promise<Unit> {
  return apiRequest<Unit, UnitFormValues>(`/api/Units/${id}`, {
    method: 'PUT',
    body: values,
  })
}

export function deleteUnit(id: number): Promise<DeleteResponse> {
  return apiRequest<DeleteResponse>(`/api/Units/${id}`, {
    method: 'DELETE',
  })
}

export function getProductUnitConversions(productId: number): Promise<ProductUnitConversion[]> {
  return apiRequest<ProductUnitConversion[]>(`/api/Products/${productId}/unit-conversions`)
}

export function createProductUnitConversion(
  productId: number,
  values: ProductUnitConversionFormValues,
): Promise<ProductUnitConversion> {
  return apiRequest<ProductUnitConversion, ProductUnitConversionFormValues>(
    `/api/Products/${productId}/unit-conversions`,
    {
      method: 'POST',
      body: values,
    },
  )
}

export function updateProductUnitConversion(
  productId: number,
  id: number,
  values: ProductUnitConversionUpdateValues,
): Promise<ProductUnitConversion> {
  return apiRequest<ProductUnitConversion, ProductUnitConversionUpdateValues>(
    `/api/Products/${productId}/unit-conversions/${id}`,
    {
      method: 'PUT',
      body: values,
    },
  )
}

export function deactivateProductUnitConversion(
  productId: number,
  id: number,
): Promise<ProductUnitConversion> {
  return apiRequest<ProductUnitConversion>(
    `/api/Products/${productId}/unit-conversions/${id}/deactivate`,
    {
      method: 'PATCH',
    },
  )
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
