import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  getDeliveryChallan,
  getDeliveryChallanStatusLabel,
  type DeliveryChallan,
} from '../features/challans/challansApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate, formatQuantity } from '../shared/utils/formatters'

function formatOptionalDate(value?: string): string {
  return value ? formatDate(value) : '-'
}

export function DeliveryChallanDetailPage() {
  const { id } = useParams()
  const challanId = Number(id)
  const [challan, setChallan] = useState<DeliveryChallan | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    async function loadChallan(): Promise<void> {
      if (!Number.isInteger(challanId) || challanId <= 0) {
        setErrorMessage('Delivery challan not found.')
        setIsLoading(false)
        return
      }

      setIsLoading(true)
      setErrorMessage(null)

      try {
        setChallan(await getDeliveryChallan(challanId))
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsLoading(false)
      }
    }

    void loadChallan()
  }, [challanId])

  return (
    <section className="content-panel wide-panel" aria-labelledby="challan-detail-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Delivery challan</p>
          <h1 id="challan-detail-title" className="page-title">
            {challan?.challanNumber ?? 'Challan detail'}
          </h1>
        </div>
      </div>

      {isLoading ? <LoadingState>Loading delivery challan...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {challan ? (
        <>
          <div className="detail-grid">
            <span>Status</span><strong>{getDeliveryChallanStatusLabel(challan.status)}</strong>
            <span>Challan date</span><strong>{formatDate(challan.challanDate)}</strong>
            <span>Customer</span><strong>{challan.customerName}</strong>
            <span>Created by</span><strong>{challan.createdBy || '-'}</strong>
            <span>Driver</span><strong>{challan.driverName || '-'}</strong>
            <span>Vehicle number</span><strong>{challan.vehicleNumber || '-'}</strong>
            <span>Delivery from</span><strong>{challan.deliveryFromAddress || '-'}</strong>
            <span>Delivery address</span><strong>{challan.deliveryAddress || '-'}</strong>
            <span>Delivery charge</span><strong>{formatCurrency(challan.deliveryCharge)}</strong>
            <span>Charge paid</span><strong>{challan.isDeliveryChargePaid ? 'Paid' : 'Unpaid'}</strong>
            <span>Available for invoicing</span><strong>{challan.isAvailableForInvoicing ? 'Yes' : 'No'}</strong>
            <span>Notes</span><strong>{challan.notes || '-'}</strong>
            <span>Created</span><strong>{formatOptionalDate(challan.createdAtUtc)}</strong>
            <span>Updated</span><strong>{formatOptionalDate(challan.updatedAtUtc)}</strong>
            <span>Posted</span><strong>{formatOptionalDate(challan.postedAtUtc)}</strong>
            <span>Cancelled</span><strong>{formatOptionalDate(challan.cancelledAtUtc)}</strong>
            <span>Invoiced</span><strong>{formatOptionalDate(challan.invoicedAtUtc)}</strong>
          </div>

          <h2>Items</h2>
          {challan.items.length === 0 ? <EmptyState>No challan items found.</EmptyState> : null}
          {challan.items.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Product</th>
                    <th>SKU</th>
                    <th>Quantity</th>
                  </tr>
                </thead>
                <tbody>
                  {challan.items.map((item) => (
                    <tr key={item.id}>
                      <td>{item.productName}</td>
                      <td>{item.productSku}</td>
                      <td>{formatQuantity(item.quantity)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to="/app/challans">Back to challans</Link></p>
    </section>
  )
}
