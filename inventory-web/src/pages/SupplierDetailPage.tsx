import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { getSupplier, type Supplier } from '../features/parties/partiesApi'
import {
  getPurchases,
  getPurchaseStatusLabel,
  type PagedResponse,
  type Purchase,
} from '../features/purchases/purchasesApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate } from '../shared/utils/formatters'

const pageSize = 10

export function SupplierDetailPage() {
  const { id } = useParams()
  const { currentUser } = useAuth()
  const supplierId = Number(id)
  const canViewPurchases = hasRouteAccess(currentUser?.roles ?? [], 'managePurchases')
  const canViewLedger = hasRouteAccess(currentUser?.roles ?? [], 'viewSupplierStatements')
  const [supplier, setSupplier] = useState<Supplier | null>(null)
  const [purchaseResponse, setPurchaseResponse] = useState<PagedResponse<Purchase> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [pageNumber, setPageNumber] = useState(1)
  const [status, setStatus] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadSupplierDetail = useCallback(async (): Promise<void> => {
    if (!supplierId) {
      setErrorMessage('Supplier not found.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      const supplierRequest = getSupplier(supplierId)

      if (!canViewPurchases) {
        setSupplier(await supplierRequest)
        setPurchaseResponse(null)
        return
      }

      const [supplierResult, purchasesResult] = await Promise.all([
        supplierRequest,
        getPurchases({
          pageNumber,
          pageSize,
          supplierId: supplierId.toString(),
          status,
          purchaseNumber: '',
          supplierBillNumber: '',
        }),
      ])

      setSupplier(supplierResult)
      setPurchaseResponse(purchasesResult)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [canViewPurchases, pageNumber, status, supplierId])

  useEffect(() => {
    void loadSupplierDetail()
  }, [loadSupplierDetail])

  function handleFilters(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
  }

  const purchases = purchaseResponse?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="supplier-detail-title">
      <p className="page-kicker">Supplier</p>
      <h1 id="supplier-detail-title" className="page-title">{supplier?.name ?? 'Supplier detail'}</h1>
      {isLoading ? <LoadingState>Loading supplier...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {supplier ? (
        <>
          <div className="detail-grid">
            <span>Status</span><strong>{supplier.isActive ? 'Active' : 'Inactive'}</strong>
            <span>Contact person</span><strong>{supplier.contactPerson || '-'}</strong>
            <span>Phone</span><strong>{supplier.phone || '-'}</strong>
            <span>Email</span><strong>{supplier.email || '-'}</strong>
            <span>GST number</span><strong>{supplier.gstNumber || '-'}</strong>
            <span>Address</span><strong>{supplier.address || '-'}</strong>
          </div>

          {canViewLedger ? (
            <p className="page-action">
              <Link className="text-link" to={`/app/suppliers/${supplier.id}/ledger`}>View ledger</Link>
            </p>
          ) : null}
        </>
      ) : null}

      {supplier && canViewPurchases ? (
        <>
          <form className="toolbar" onSubmit={handleFilters}>
            <select aria-label="Purchase payment status" onChange={(event) => { setPageNumber(1); setStatus(event.target.value) }} value={status}>
              <option value="">All statuses</option>
              <option value="0">Draft</option>
              <option value="1">Posted / unpaid</option>
              <option value="2">Cancelled</option>
              <option value="3">Partially paid</option>
              <option value="4">Paid</option>
            </select>
            <button className="secondary-button" type="submit">Apply</button>
          </form>

          <h2>Purchases</h2>
          {!isLoading && purchases.length === 0 ? <EmptyState>No purchases found for this supplier.</EmptyState> : null}

          {purchases.length > 0 ? (
            <>
              <div className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Purchase</th>
                      <th>Date</th>
                      <th>Payment status</th>
                      <th>Grand total</th>
                      <th>Amount paid</th>
                      <th>Balance due</th>
                    </tr>
                  </thead>
                  <tbody>
                    {purchases.map((purchase) => (
                      <tr key={purchase.id}>
                        <td>
                          <strong>{purchase.purchaseNumber}</strong>
                          <br />
                          <span>{purchase.supplierBillNumber || '-'}</span>
                        </td>
                        <td>{formatDate(purchase.billDate)}</td>
                        <td>{getPurchaseStatusLabel(purchase.status)}</td>
                        <td>{formatCurrency(purchase.grandTotal)}</td>
                        <td>{formatCurrency(purchase.amountPaid)}</td>
                        <td>{formatCurrency(purchase.balanceDue)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="pagination">
                <button className="secondary-button" disabled={!purchaseResponse?.hasPreviousPage} onClick={() => setPageNumber((current) => Math.max(1, current - 1))} type="button">Previous</button>
                <span>Page {purchaseResponse?.pageNumber ?? 1} of {purchaseResponse?.totalPages ?? 1}</span>
                <button className="secondary-button" disabled={!purchaseResponse?.hasNextPage} onClick={() => setPageNumber((current) => current + 1)} type="button">Next</button>
              </div>
            </>
          ) : null}
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to="/app/suppliers">Back to suppliers</Link></p>
    </section>
  )
}
