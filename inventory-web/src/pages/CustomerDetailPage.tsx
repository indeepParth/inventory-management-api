import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getCustomer, type Customer } from '../features/parties/partiesApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'

export function CustomerDetailPage() {
  const { id } = useParams()
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    async function loadCustomer(): Promise<void> {
      setIsLoading(true)
      setErrorMessage(null)

      try {
        setCustomer(await getCustomer(Number(id)))
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsLoading(false)
      }
    }

    void loadCustomer()
  }, [id])

  return (
    <section className="content-panel" aria-labelledby="customer-detail-title">
      <p className="page-kicker">Customer</p>
      <h1 id="customer-detail-title" className="page-title">{customer?.name ?? 'Customer detail'}</h1>
      {isLoading ? <p className="state-message">Loading customer...</p> : null}
      {errorMessage ? <p className="form-error" role="alert">{errorMessage}</p> : null}
      {customer ? (
        <div className="detail-grid">
          <span>Status</span><strong>{customer.isActive ? 'Active' : 'Inactive'}</strong>
          <span>Balance due</span><strong>{customer.balanceDue.toFixed(2)}</strong>
          <span>Credit limit</span><strong>{customer.creditLimit.toFixed(2)}</strong>
          <span>Contact person</span><strong>{customer.contactPerson || '-'}</strong>
          <span>Phone</span><strong>{customer.phone || '-'}</strong>
          <span>Email</span><strong>{customer.email || '-'}</strong>
          <span>GST number</span><strong>{customer.gstNumber || '-'}</strong>
          <span>Billing address</span><strong>{customer.billingAddress || '-'}</strong>
          <span>Delivery address</span><strong>{customer.deliveryAddress || '-'}</strong>
        </div>
      ) : null}
      <p className="page-action"><Link className="text-link" to="/app/customers">Back to customers</Link></p>
    </section>
  )
}
