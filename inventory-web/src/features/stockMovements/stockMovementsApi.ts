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

export type StockMovementType = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7

export type StockMovement = {
  id: number
  productId: number
  productName: string
  movementType: StockMovementType
  quantityChange: number
  balanceBefore: number
  balanceAfter: number
  unitCost: number
  sourceType: string
  sourceId?: string
  reference?: string
  reason?: string
  note?: string
  occurredAtUtc: string
  createdBy: string
}

export type StockMovementFilters = {
  pageNumber: number
  pageSize: number
  productId: string
  movementType: string
  fromDate: string
  toDate: string
}

export type DamageFormValues = {
  productId: number
  quantity: number
  reason: string
  reference: string
  note: string
}

export type AdjustmentFormValues = {
  productId: number
  quantityChange: number
  reason: string
  reference: string
  note: string
}

export type ReverseManualCorrectionValues = {
  reason: string
  reference: string
  note: string
}

function buildMovementQuery(filters: StockMovementFilters): string {
  const query = new URLSearchParams({
    PageNumber: filters.pageNumber.toString(),
    PageSize: filters.pageSize.toString(),
  })

  if (filters.productId) {
    query.set('ProductId', filters.productId)
  }

  if (filters.movementType) {
    query.set('MovementType', filters.movementType)
  }

  if (filters.fromDate) {
    query.set('FromDate', filters.fromDate)
  }

  if (filters.toDate) {
    query.set('ToDate', filters.toDate)
  }

  return query.toString()
}

export function getStockMovements(
  filters: StockMovementFilters,
): Promise<PagedResponse<StockMovement>> {
  return apiRequest<PagedResponse<StockMovement>>(
    `/api/stock-movements?${buildMovementQuery(filters)}`,
  )
}

export function recordDamage(values: DamageFormValues): Promise<StockMovement> {
  return apiRequest<StockMovement, DamageFormValues>('/api/stock-movements/damage', {
    method: 'POST',
    body: values,
  })
}

export function recordAdjustment(values: AdjustmentFormValues): Promise<StockMovement> {
  return apiRequest<StockMovement, AdjustmentFormValues>(
    '/api/stock-movements/adjustment',
    {
      method: 'POST',
      body: values,
    },
  )
}

export function reverseManualCorrection(
  id: number,
  values: ReverseManualCorrectionValues,
): Promise<StockMovement> {
  return apiRequest<StockMovement, ReverseManualCorrectionValues>(
    `/api/stock-movements/${id}/reverse`,
    {
      method: 'POST',
      body: values,
    },
  )
}

export function getStockMovementTypeLabel(type: StockMovementType): string {
  const labels: Record<StockMovementType, string> = {
    0: 'Opening stock',
    1: 'Purchase',
    2: 'Sale',
    3: 'Customer return',
    4: 'Supplier return',
    5: 'Adjustment',
    6: 'Damage',
    7: 'Reversal',
  }

  return labels[type] ?? 'Unknown'
}
