import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { getProducts, type Product } from '../features/products/productsApi'
import {
  getStockMovements,
  getStockMovementTypeLabel,
  type PagedResponse,
  type StockMovement,
} from '../features/stockMovements/stockMovementsApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatDate, formatQuantity } from '../shared/utils/formatters'

const pageSize = 10

function getQuantityIn(movement: StockMovement): string {
  return movement.quantityChange > 0 ? formatQuantity(movement.quantityChange) : '-'
}

function getQuantityOut(movement: StockMovement): string {
  return movement.quantityChange < 0 ? formatQuantity(Math.abs(movement.quantityChange)) : '-'
}

function getSourceReference(movement: StockMovement): string {
  if (movement.reference) {
    return movement.reference
  }

  if (movement.sourceId) {
    return `${movement.sourceType} #${movement.sourceId}`
  }

  return movement.sourceType || '-'
}

export function StockMovementsPage() {
  const [response, setResponse] = useState<PagedResponse<StockMovement> | null>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [pageNumber, setPageNumber] = useState(1)
  const [productId, setProductId] = useState('')
  const [movementType, setMovementType] = useState('')
  const [fromDateInput, setFromDateInput] = useState('')
  const [toDateInput, setToDateInput] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadMovements = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [movementPage, productPage] = await Promise.all([
        getStockMovements({
          pageNumber,
          pageSize,
          productId,
          movementType,
          fromDate,
          toDate,
        }),
        getProducts(1, 100),
      ])
      setResponse(movementPage)
      setProducts(productPage.items)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [fromDate, movementType, pageNumber, productId, toDate])

  useEffect(() => {
    void loadMovements()
  }, [loadMovements])

  function handleDateFilter(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setFromDate(fromDateInput)
    setToDate(toDateInput)
  }

  function clearFilters(): void {
    setPageNumber(1)
    setProductId('')
    setMovementType('')
    setFromDateInput('')
    setToDateInput('')
    setFromDate('')
    setToDate('')
  }

  const movements = response?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="stock-movements-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Append-only inventory ledger</p>
          <h1 id="stock-movements-title" className="page-title">Stock movements</h1>
        </div>
      </div>

      <form className="toolbar" onSubmit={handleDateFilter}>
        <select aria-label="Product" onChange={(event) => { setPageNumber(1); setProductId(event.target.value) }} value={productId}>
          <option value="">All products</option>
          {products.map((product) => <option key={product.id} value={product.id}>{product.name}</option>)}
        </select>
        <select aria-label="Movement type" onChange={(event) => { setPageNumber(1); setMovementType(event.target.value) }} value={movementType}>
          <option value="">All movement types</option>
          <option value="0">Opening stock</option>
          <option value="1">Purchase</option>
          <option value="2">Sale</option>
          <option value="3">Customer return</option>
          <option value="4">Supplier return</option>
          <option value="5">Adjustment</option>
          <option value="6">Damage</option>
          <option value="7">Reversal</option>
        </select>
        <input aria-label="From date" onChange={(event) => setFromDateInput(event.target.value)} type="date" value={fromDateInput} />
        <input aria-label="To date" onChange={(event) => setToDateInput(event.target.value)} type="date" value={toDateInput} />
        <button className="secondary-button" type="submit">Apply dates</button>
        <button className="text-button" onClick={clearFilters} type="button">Clear</button>
      </form>

      <EmptyState>This page is read-only. Stock changes are recorded by workflow posting or explicit correction endpoints, not by editing ledger rows.</EmptyState>

      {isLoading ? <LoadingState>Loading stock movements...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {!isLoading && !errorMessage && movements.length === 0 ? <EmptyState>No stock movements found.</EmptyState> : null}

      {!isLoading && !errorMessage && movements.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Product</th>
                  <th>Type</th>
                  <th>In</th>
                  <th>Out</th>
                  <th>Balance after</th>
                  <th>Reason</th>
                  <th>Source</th>
                </tr>
              </thead>
              <tbody>
                {movements.map((movement) => (
                  <tr key={movement.id}>
                    <td>{formatDate(movement.occurredAtUtc)}</td>
                    <td>{movement.productName}</td>
                    <td>{getStockMovementTypeLabel(movement.movementType)}</td>
                    <td>{getQuantityIn(movement)}</td>
                    <td>{getQuantityOut(movement)}</td>
                    <td>{formatQuantity(movement.balanceAfter)}</td>
                    <td>{movement.reason ?? movement.note ?? '-'}</td>
                    <td>{getSourceReference(movement)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="pagination">
            <button className="secondary-button" disabled={!response?.hasPreviousPage} onClick={() => setPageNumber((current) => Math.max(1, current - 1))} type="button">Previous</button>
            <span>Page {response?.pageNumber ?? 1} of {response?.totalPages ?? 1}</span>
            <button className="secondary-button" disabled={!response?.hasNextPage} onClick={() => setPageNumber((current) => current + 1)} type="button">Next</button>
          </div>
        </>
      ) : null}
    </section>
  )
}
