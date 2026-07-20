import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { markDeliveryChargePaid } from '../features/challans/challansApi'
import {
  getDriverDeliveries,
  type DriverDeliveriesResponse,
  type DriverDeliveryPaymentStatus,
  type DriverDeliveryRow,
} from '../features/drivers/driversApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate } from '../shared/utils/formatters'

const pageSize = 10

function canMarkDeliveryChargePaid(delivery: DriverDeliveryRow): boolean {
  return (
    delivery.deliveryCharge > 0 &&
    !delivery.isDeliveryChargePaid &&
    (delivery.status === 1 || delivery.status === 3)
  )
}

export function DriverDetailPage() {
  const { id } = useParams()
  const { currentUser } = useAuth()
  const driverId = Number(id)
  const canManageChallans = hasRouteAccess(currentUser?.roles ?? [], 'manageDeliveryChallans')
  const [response, setResponse] = useState<DriverDeliveriesResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [pageNumber, setPageNumber] = useState(1)
  const [dateFromInput, setDateFromInput] = useState('')
  const [dateToInput, setDateToInput] = useState('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [paymentStatus, setPaymentStatus] = useState<DriverDeliveryPaymentStatus>('all')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [markingChallanId, setMarkingChallanId] = useState<number | null>(null)

  const loadDriverDeliveries = useCallback(async (): Promise<void> => {
    if (!driverId) {
      setErrorMessage('Driver not found.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      setResponse(await getDriverDeliveries(driverId, {
        pageNumber,
        pageSize,
        dateFrom,
        dateTo,
        paymentStatus,
      }))
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [dateFrom, dateTo, driverId, pageNumber, paymentStatus])

  useEffect(() => {
    void loadDriverDeliveries()
  }, [loadDriverDeliveries])

  function handleFilters(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setDateFrom(dateFromInput)
    setDateTo(dateToInput)
  }

  function clearFilters(): void {
    setDateFromInput('')
    setDateToInput('')
    setDateFrom('')
    setDateTo('')
    setPaymentStatus('all')
    setPageNumber(1)
  }

  async function handleMarkPaid(delivery: DriverDeliveryRow): Promise<void> {
    setActionError(null)
    setMarkingChallanId(delivery.challanId)

    try {
      await markDeliveryChargePaid(delivery.challanId)
      await loadDriverDeliveries()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setMarkingChallanId(null)
    }
  }

  const deliveries = response?.deliveries.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="driver-detail-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Driver detail</p>
          <h1 id="driver-detail-title" className="page-title">
            {response?.name ?? 'Driver detail'}
          </h1>
        </div>
      </div>

      {isLoading ? <LoadingState>Loading driver deliveries...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

      {response ? (
        <>
          <div className="detail-grid">
            <span>Status</span><strong>{response.isActive ? 'Active' : 'Inactive'}</strong>
            <span>Phone</span><strong>{response.phone || '-'}</strong>
            <span>License number</span><strong>{response.licenseNumber || '-'}</strong>
          </div>

          <form className="toolbar customer-account-filters" onSubmit={handleFilters}>
            <input aria-label="From date" onChange={(event) => setDateFromInput(event.target.value)} type="date" value={dateFromInput} />
            <input aria-label="To date" onChange={(event) => setDateToInput(event.target.value)} type="date" value={dateToInput} />
            <select aria-label="Delivery charge payment status" onChange={(event) => { setPageNumber(1); setPaymentStatus(event.target.value as DriverDeliveryPaymentStatus) }} value={paymentStatus}>
              <option value="all">All</option>
              <option value="paid">Paid</option>
              <option value="unpaid">Unpaid</option>
            </select>
            <button className="secondary-button" type="submit">Apply dates</button>
            <button className="text-button" onClick={clearFilters} type="button">Clear</button>
          </form>

          <h2>Delivery history</h2>
          {!isLoading && deliveries.length === 0 ? <EmptyState>No delivery history found.</EmptyState> : null}

          {deliveries.length > 0 ? (
            <>
              <div className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Challan number</th>
                      <th>Customer</th>
                      <th>From address</th>
                      <th>To address</th>
                      <th>Vehicle</th>
                      <th>Charge</th>
                      <th>Paid status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {deliveries.map((delivery) => (
                      <tr key={delivery.challanId}>
                        <td>{formatDate(delivery.challanDate)}</td>
                        <td>
                          {canManageChallans ? (
                            <Link className="text-link" to={`/app/challans/${delivery.challanId}`}>{delivery.challanNumber}</Link>
                          ) : (
                            delivery.challanNumber
                          )}
                        </td>
                        <td>{delivery.customerName}</td>
                        <td>{delivery.deliveryFromAddress || '-'}</td>
                        <td>{delivery.deliveryToAddress || '-'}</td>
                        <td>{delivery.vehicleNumber || '-'}</td>
                        <td>{formatCurrency(delivery.deliveryCharge)}</td>
                        <td>
                          {delivery.deliveryCharge > 0
                            ? delivery.isDeliveryChargePaid ? 'Paid' : 'Unpaid'
                            : '-'}
                        </td>
                        <td>
                          <div className="table-actions">
                            {canMarkDeliveryChargePaid(delivery) ? (
                              <button
                                className="text-button"
                                disabled={markingChallanId === delivery.challanId}
                                onClick={() => void handleMarkPaid(delivery)}
                                type="button"
                              >
                                {markingChallanId === delivery.challanId ? 'Saving...' : 'Mark paid'}
                              </button>
                            ) : (
                              <span>-</span>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="pagination">
                <button className="secondary-button" disabled={!response.deliveries.hasPreviousPage} onClick={() => setPageNumber((current) => Math.max(1, current - 1))} type="button">Previous</button>
                <span>Page {response.deliveries.pageNumber} of {response.deliveries.totalPages}</span>
                <button className="secondary-button" disabled={!response.deliveries.hasNextPage} onClick={() => setPageNumber((current) => current + 1)} type="button">Next</button>
              </div>
            </>
          ) : null}
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to="/app/drivers">Back to drivers</Link></p>
    </section>
  )
}
