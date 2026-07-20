import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { Link, useNavigate } from 'react-router-dom'
import { DeliveryChallanForm } from '../features/challans/DeliveryChallanForm'
import {
  cancelDeliveryChallan,
  createDeliveryChallan,
  getDeliveryChallans,
  getDeliveryChallanStatusLabel,
  postDeliveryChallan,
  updateDeliveryChallan,
  type DeliveryChallan,
  type DeliveryChallanFormValues,
  type PagedResponse,
} from '../features/challans/challansApi'
import { getDrivers, type Driver } from '../features/drivers/driversApi'
import { getCustomers, type Customer } from '../features/parties/partiesApi'
import { getProducts, type Product } from '../features/products/productsApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatDate } from '../shared/utils/formatters'

const pageSize = 10

function canCreateInvoiceFromChallan(challan: DeliveryChallan): boolean {
  return challan.isAvailableForInvoicing
}

export function DeliveryChallansPage() {
  const { currentUser } = useAuth()
  const navigate = useNavigate()
  const canCancelChallans = hasRouteAccess(currentUser?.roles ?? [], 'adminOrManager')
  const canCreateInvoices = hasRouteAccess(currentUser?.roles ?? [], 'manageSalesInvoices')
  const [response, setResponse] = useState<PagedResponse<DeliveryChallan> | null>(null)
  const [customers, setCustomers] = useState<Customer[]>([])
  const [drivers, setDrivers] = useState<Driver[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [editingChallan, setEditingChallan] = useState<DeliveryChallan | undefined>()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [status, setStatus] = useState('')
  const [challanNumberInput, setChallanNumberInput] = useState('')
  const [challanNumber, setChallanNumber] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadChallans = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [challanPage, customerPage, driverPage, productPage] = await Promise.all([
        getDeliveryChallans({
          pageNumber,
          pageSize,
          customerId: '',
          status,
          challanNumber,
        }),
        getCustomers(1, 100, '', 'true'),
        getDrivers(1, 100, '', 'true'),
        getProducts(1, 100),
      ])
      setResponse(challanPage)
      setCustomers(customerPage.items)
      setDrivers(driverPage.items)
      setProducts(productPage.items)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [challanNumber, pageNumber, status])

  useEffect(() => {
    void loadChallans()
  }, [loadChallans])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingChallan(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setChallanNumber(challanNumberInput.trim())
  }

  async function handleSubmit(values: DeliveryChallanFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingChallan) {
        await updateDeliveryChallan(editingChallan.id, values)
      } else {
        await createDeliveryChallan(values)
      }

      closeForm()
      await loadChallans()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handlePost(challan: DeliveryChallan): Promise<void> {
    const confirmed = window.confirm(`Post challan "${challan.challanNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await postDeliveryChallan(challan.id)
      await loadChallans()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  async function handleCancel(challan: DeliveryChallan): Promise<void> {
    const confirmed = window.confirm(`Cancel challan "${challan.challanNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await cancelDeliveryChallan(challan.id)
      await loadChallans()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  function handleCreateInvoice(challan: DeliveryChallan): void {
    navigate(`/app/sales-invoices?mode=challans&challanId=${challan.id}`)
  }

  const challans = response?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="challans-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Sales dispatch</p>
          <h1 id="challans-title" className="page-title">Delivery challans</h1>
        </div>
        <button className="primary-button" disabled={customers.length === 0 || products.length === 0} onClick={() => { setIsFormOpen(true); setEditingChallan(undefined); setFieldErrors({}); setActionError(null) }} type="button">New draft challan</button>
      </div>

      <form className="toolbar" onSubmit={handleSearch}>
        <input aria-label="Challan number" onChange={(event) => setChallanNumberInput(event.target.value)} placeholder="Challan number" type="search" value={challanNumberInput} />
        <select aria-label="Challan status" onChange={(event) => { setPageNumber(1); setStatus(event.target.value) }} value={status}>
          <option value="">All statuses</option>
          <option value="0">Draft</option>
          <option value="1">Posted</option>
          <option value="2">Cancelled</option>
          <option value="3">Invoiced</option>
        </select>
        <button className="secondary-button" type="submit">Search</button>
      </form>

      {(customers.length === 0 || products.length === 0) && !isLoading ? (
        <p className="state-message">Create at least one active customer and one product before adding challans.</p>
      ) : null}

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

      {isFormOpen ? (
        <DeliveryChallanForm
          customers={customers}
          drivers={drivers}
          errors={fieldErrors}
          initialValue={editingChallan}
          isSubmitting={isSaving}
          onCancel={closeForm}
          onSubmit={handleSubmit}
          products={products}
        />
      ) : null}

      {isLoading ? <LoadingState>Loading delivery challans...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {!isLoading && !errorMessage && challans.length === 0 ? <EmptyState>No delivery challans found.</EmptyState> : null}

      {!isLoading && !errorMessage && challans.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Challan</th>
                  <th>Customer</th>
                  <th>Date</th>
                  <th>Status</th>
                  <th>Items</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {challans.map((challan) => (
                  <tr key={challan.id}>
                    <td><Link className="text-link" to={`/app/challans/${challan.id}`}>{challan.challanNumber}</Link></td>
                    <td>{challan.customerName}</td>
                    <td>{formatDate(challan.challanDate)}</td>
                    <td>{getDeliveryChallanStatusLabel(challan.status)}</td>
                    <td>{challan.items.length}</td>
                    <td>
                      <div className="table-actions">
                        {challan.status === 0 ? (
                          <>
                            <button className="text-button" onClick={() => { setEditingChallan(challan); setIsFormOpen(true); setFieldErrors({}); setActionError(null) }} type="button">Edit</button>
                            <button className="text-button" onClick={() => void handlePost(challan)} type="button">Post</button>
                          </>
                        ) : null}
                        {challan.status !== 2 && challan.status !== 3 && canCancelChallans ? (
                          <button className="danger-button" onClick={() => void handleCancel(challan)} type="button">Cancel</button>
                        ) : null}
                        {canCreateInvoices && canCreateInvoiceFromChallan(challan) ? (
                          <button className="text-button" onClick={() => handleCreateInvoice(challan)} type="button">Create invoice</button>
                        ) : null}
                      </div>
                    </td>
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
