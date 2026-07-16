import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { DeliveryChallanForm } from '../features/challans/DeliveryChallanForm'
import {
  createDeliveryChallan,
  getDeliveryChallans,
  getDeliveryChallanStatusLabel,
  postDeliveryChallan,
  updateDeliveryChallan,
  type DeliveryChallan,
  type DeliveryChallanFormValues,
} from '../features/challans/challansApi'
import { getDrivers, type Driver } from '../features/drivers/driversApi'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { getCustomer, type Customer } from '../features/parties/partiesApi'
import { PaymentForm } from '../features/payments/PaymentForm'
import { createPayment, type PaymentFormValues } from '../features/payments/paymentsApi'
import { getProducts, type Product } from '../features/products/productsApi'
import { DirectInvoiceForm } from '../features/salesInvoices/DirectInvoiceForm'
import {
  createDirectInvoice,
  getSalesInvoices,
  getSalesInvoiceStatusLabel,
  postSalesInvoice,
  type DirectInvoiceFormValues,
  type SalesInvoice,
  type SalesInvoiceStatus,
} from '../features/salesInvoices/salesInvoicesApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate } from '../shared/utils/formatters'

const detailPageSize = 100

type PaymentFilter = 'outstanding' | 'all' | 'draft' | 'unpaid' | 'partial' | 'paid'

function isCollectionInvoice(invoice: SalesInvoice): boolean {
  return invoice.status === 0 || invoice.status === 1 || invoice.status === 2 || invoice.status === 3
}

function matchesPaymentFilter(invoice: SalesInvoice, paymentFilter: PaymentFilter): boolean {
  if (paymentFilter === 'all') {
    return isCollectionInvoice(invoice)
  }

  if (paymentFilter === 'outstanding') {
    return invoice.status === 1 || invoice.status === 2
  }

  if (paymentFilter === 'draft') {
    return invoice.status === 0
  }

  if (paymentFilter === 'unpaid') {
    return invoice.status === 1
  }

  if (paymentFilter === 'partial') {
    return invoice.status === 2
  }

  return invoice.status === 3
}

function getInvoiceOutstandingTotal(invoices: SalesInvoice[]): number {
  return invoices
    .filter((invoice) => invoice.status === 1 || invoice.status === 2)
    .reduce((total, invoice) => total + invoice.balanceDue, 0)
}

function canCreateInvoiceFromChallan(challan: DeliveryChallan): boolean {
  return challan.isAvailableForInvoicing
}

function canReceivePaymentForInvoice(invoice: SalesInvoice): boolean {
  return (invoice.status === 1 || invoice.status === 2) && invoice.balanceDue > 0
}

export function CustomerDetailPage() {
  const { id } = useParams()
  const { currentUser } = useAuth()
  const navigate = useNavigate()
  const canManageChallans = hasRouteAccess(currentUser?.roles ?? [], 'manageDeliveryChallans')
  const canCreateInvoices = hasRouteAccess(currentUser?.roles ?? [], 'manageSalesInvoices')
  const canReceivePayments = hasRouteAccess(currentUser?.roles ?? [], 'viewPayments')
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [invoices, setInvoices] = useState<SalesInvoice[]>([])
  const [customerChallans, setCustomerChallans] = useState<DeliveryChallan[]>([])
  const [drivers, setDrivers] = useState<Driver[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [editingChallan, setEditingChallan] = useState<DeliveryChallan | undefined>()
  const [paymentInvoice, setPaymentInvoice] = useState<SalesInvoice | undefined>()
  const [isChallanFormOpen, setIsChallanFormOpen] = useState(false)
  const [isDirectInvoiceFormOpen, setIsDirectInvoiceFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSavingChallan, setIsSavingChallan] = useState(false)
  const [isSavingDirectInvoice, setIsSavingDirectInvoice] = useState(false)
  const [isSavingPayment, setIsSavingPayment] = useState(false)
  const [fromDateInput, setFromDateInput] = useState('')
  const [toDateInput, setToDateInput] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [paymentFilter, setPaymentFilter] = useState<PaymentFilter>('outstanding')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const customerId = Number(id)

  const loadCustomerDetail = useCallback(async (): Promise<void> => {
    if (!customerId) {
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [customerResponse, invoiceResponse, challanResponse, driverResponse, productResponse] = await Promise.all([
        getCustomer(customerId),
        getSalesInvoices({
          pageNumber: 1,
          pageSize: detailPageSize,
          customerId: customerId.toString(),
          status: '',
          invoiceNumber: '',
          dateFrom: fromDate,
          dateTo: toDate,
        }),
        getDeliveryChallans({
          pageNumber: 1,
          pageSize: detailPageSize,
          customerId: customerId.toString(),
          status: '',
          challanNumber: '',
          dateFrom: fromDate,
          dateTo: toDate,
        }),
        getDrivers(1, 100, '', 'true'),
        getProducts(1, 100),
      ])

      setCustomer(customerResponse)
      setInvoices(invoiceResponse.items.filter(isCollectionInvoice))
      setCustomerChallans(challanResponse.items)
      setDrivers(driverResponse.items)
      setProducts(productResponse.items)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [customerId, fromDate, toDate])

  useEffect(() => {
    void loadCustomerDetail()
  }, [loadCustomerDetail])

  function handleFilters(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setFromDate(fromDateInput)
    setToDate(toDateInput)
  }

  function clearFilters(): void {
    setFromDateInput('')
    setToDateInput('')
    setFromDate('')
    setToDate('')
    setPaymentFilter('outstanding')
  }

  function openNewChallanForm(): void {
    setEditingChallan(undefined)
    setFieldErrors({})
    setActionError(null)
    setPaymentInvoice(undefined)
    setIsDirectInvoiceFormOpen(false)
    setIsChallanFormOpen(true)
  }

  function openEditChallanForm(challan: DeliveryChallan): void {
    setEditingChallan(challan)
    setFieldErrors({})
    setActionError(null)
    setPaymentInvoice(undefined)
    setIsDirectInvoiceFormOpen(false)
    setIsChallanFormOpen(true)
  }

  function closeChallanForm(): void {
    setEditingChallan(undefined)
    setFieldErrors({})
    setActionError(null)
    setIsChallanFormOpen(false)
  }

  function openNewDirectInvoiceForm(): void {
    setFieldErrors({})
    setActionError(null)
    setEditingChallan(undefined)
    setPaymentInvoice(undefined)
    setIsChallanFormOpen(false)
    setIsDirectInvoiceFormOpen(true)
  }

  function closeDirectInvoiceForm(): void {
    setFieldErrors({})
    setActionError(null)
    setIsDirectInvoiceFormOpen(false)
  }

  function openPaymentForm(invoice: SalesInvoice): void {
    setFieldErrors({})
    setActionError(null)
    setEditingChallan(undefined)
    setIsChallanFormOpen(false)
    setIsDirectInvoiceFormOpen(false)
    setPaymentInvoice(invoice)
  }

  function closePaymentForm(): void {
    setFieldErrors({})
    setActionError(null)
    setPaymentInvoice(undefined)
  }

  async function handleChallanSubmit(values: DeliveryChallanFormValues): Promise<void> {
    setIsSavingChallan(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingChallan) {
        await updateDeliveryChallan(editingChallan.id, values)
      } else {
        await createDeliveryChallan(values)
      }

      closeChallanForm()
      await loadCustomerDetail()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSavingChallan(false)
    }
  }

  async function handleDirectInvoiceSubmit(values: DirectInvoiceFormValues): Promise<void> {
    setIsSavingDirectInvoice(true)
    setFieldErrors({})
    setActionError(null)

    try {
      await createDirectInvoice(values)
      closeDirectInvoiceForm()
      await loadCustomerDetail()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSavingDirectInvoice(false)
    }
  }

  async function handlePaymentSubmit(values: PaymentFormValues): Promise<void> {
    setIsSavingPayment(true)
    setFieldErrors({})
    setActionError(null)

    try {
      await createPayment(values)
      closePaymentForm()
      await loadCustomerDetail()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSavingPayment(false)
    }
  }

  async function handlePostChallan(challan: DeliveryChallan): Promise<void> {
    const confirmed = window.confirm(`Post challan "${challan.challanNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await postDeliveryChallan(challan.id)
      await loadCustomerDetail()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  async function handlePostInvoice(invoice: SalesInvoice): Promise<void> {
    const confirmed = window.confirm(`Post invoice "${invoice.invoiceNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await postSalesInvoice(invoice.id)
      await loadCustomerDetail()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  function handleCreateInvoice(challan: DeliveryChallan): void {
    navigate(`/app/sales-invoices?mode=challans&challanId=${challan.id}`)
  }

  const visibleInvoices = invoices.filter((invoice) =>
    matchesPaymentFilter(invoice, paymentFilter),
  )
  const outstandingInvoiceTotal = getInvoiceOutstandingTotal(invoices)
  const outstandingInvoiceCount = invoices.filter((invoice) =>
    invoice.status === 1 || invoice.status === 2,
  ).length
  const availableChallanCount = customerChallans.filter((challan) =>
    challan.isAvailableForInvoicing,
  ).length

  return (
    <section className="content-panel wide-panel" aria-labelledby="customer-detail-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Customer account</p>
          <h1 id="customer-detail-title" className="page-title">{customer?.name ?? 'Customer detail'}</h1>
        </div>
        {canCreateInvoices || canManageChallans ? (
          <div className="form-actions">
            {canCreateInvoices ? (
              <button className="primary-button" disabled={products.length === 0} onClick={openNewDirectInvoiceForm} type="button">New direct invoice</button>
            ) : null}
            {canManageChallans ? (
              <button className="secondary-button" disabled={products.length === 0} onClick={openNewChallanForm} type="button">New draft challan</button>
            ) : null}
          </div>
        ) : null}
      </div>

      {isLoading ? <LoadingState>Loading customer...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

      {customer ? (
        <>
          <div className="summary-grid">
            <article className="summary-card">
              <span>Balance due</span>
              <strong>{formatCurrency(customer.balanceDue)}</strong>
            </article>
            <article className="summary-card">
              <span>Outstanding invoices</span>
              <strong>{outstandingInvoiceCount}</strong>
              <small>{formatCurrency(outstandingInvoiceTotal)}</small>
            </article>
            <article className="summary-card">
              <span>Available challans</span>
              <strong>{availableChallanCount}</strong>
            </article>
          </div>

          {isChallanFormOpen ? (
            <DeliveryChallanForm
              customers={[customer]}
              drivers={drivers}
              errors={fieldErrors}
              initialCustomerId={customer.id}
              initialDeliveryAddress={customer.deliveryAddress}
              initialValue={editingChallan}
              isSubmitting={isSavingChallan}
              lockCustomer
              onCancel={closeChallanForm}
              onSubmit={handleChallanSubmit}
              products={products}
            />
          ) : null}

          {isDirectInvoiceFormOpen ? (
            <DirectInvoiceForm
              customers={[customer]}
              errors={fieldErrors}
              initialCustomerId={customer.id}
              isSubmitting={isSavingDirectInvoice}
              lockCustomer
              onCancel={closeDirectInvoiceForm}
              onSubmit={handleDirectInvoiceSubmit}
              products={products}
            />
          ) : null}

          {paymentInvoice ? (
            <PaymentForm
              customers={[customer]}
              errors={fieldErrors}
              initialAmount={paymentInvoice.balanceDue}
              initialCustomerId={customer.id}
              initialSalesInvoiceId={paymentInvoice.id}
              invoices={invoices}
              isSubmitting={isSavingPayment}
              lockCustomer
              lockDocument
              mode="customer"
              onCancel={closePaymentForm}
              onSubmit={handlePaymentSubmit}
              purchases={[]}
              suppliers={[]}
            />
          ) : null}

          {products.length === 0 && (canCreateInvoices || canManageChallans) && !isLoading ? (
            <p className="state-message">Create at least one product before adding invoices or challans for this customer.</p>
          ) : null}

          <div className="detail-grid">
            <span>Status</span><strong>{customer.isActive ? 'Active' : 'Inactive'}</strong>
            <span>Credit limit</span><strong>{formatCurrency(customer.creditLimit)}</strong>
            <span>Contact person</span><strong>{customer.contactPerson || '-'}</strong>
            <span>Phone</span><strong>{customer.phone || '-'}</strong>
            <span>Email</span><strong>{customer.email || '-'}</strong>
            <span>GST number</span><strong>{customer.gstNumber || '-'}</strong>
            <span>Billing address</span><strong>{customer.billingAddress || '-'}</strong>
            <span>Delivery address</span><strong>{customer.deliveryAddress || '-'}</strong>
          </div>

          <form className="toolbar customer-account-filters" onSubmit={handleFilters}>
            <input aria-label="From date" onChange={(event) => setFromDateInput(event.target.value)} type="date" value={fromDateInput} />
            <input aria-label="To date" onChange={(event) => setToDateInput(event.target.value)} type="date" value={toDateInput} />
            <select aria-label="Payment status" onChange={(event) => setPaymentFilter(event.target.value as PaymentFilter)} value={paymentFilter}>
              <option value="outstanding">Outstanding</option>
              <option value="all">All</option>
              <option value="draft">Draft</option>
              <option value="unpaid">Unpaid</option>
              <option value="partial">Partially paid</option>
              <option value="paid">Paid</option>
            </select>
            <button className="secondary-button" type="submit">Apply dates</button>
            <button className="text-button" onClick={clearFilters} type="button">Clear</button>
          </form>

          <h2>Sales invoices</h2>
          {visibleInvoices.length === 0 ? <EmptyState>No matching invoices found.</EmptyState> : null}
          {visibleInvoices.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Invoice</th>
                    <th>Date</th>
                    <th>Payment status</th>
                    <th>Grand total</th>
                    <th>Amount paid</th>
                    <th>Balance due</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {visibleInvoices.map((invoice) => (
                    <tr key={invoice.id}>
                      <td>{invoice.invoiceNumber}</td>
                      <td>{formatDate(invoice.invoiceDate)}</td>
                      <td>{getSalesInvoiceStatusLabel(invoice.status as SalesInvoiceStatus)}</td>
                      <td>{formatCurrency(invoice.grandTotal)}</td>
                      <td>{formatCurrency(invoice.amountPaid)}</td>
                      <td>{formatCurrency(invoice.balanceDue)}</td>
                      <td>
                        <div className="table-actions">
                          {canCreateInvoices && invoice.status === 0 ? (
                            <button className="text-button" onClick={() => void handlePostInvoice(invoice)} type="button">Post</button>
                          ) : null}
                          {canReceivePayments && canReceivePaymentForInvoice(invoice) ? (
                            <button className="text-button" onClick={() => openPaymentForm(invoice)} type="button">Receive payment</button>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}

          <h2>Customer challans</h2>
          {customerChallans.length === 0 ? <EmptyState>No customer challans found.</EmptyState> : null}
          {customerChallans.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Challan</th>
                    <th>Date</th>
                    <th>Status</th>
                    <th>Items</th>
                    <th>Delivery address</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {customerChallans.map((challan) => (
                    <tr key={challan.id}>
                      <td>{challan.challanNumber}</td>
                      <td>{formatDate(challan.challanDate)}</td>
                      <td>{getDeliveryChallanStatusLabel(challan.status)}</td>
                      <td>{challan.items.length}</td>
                      <td>{challan.deliveryAddress || '-'}</td>
                      <td>
                        <div className="table-actions">
                          {canManageChallans && challan.status === 0 ? (
                            <>
                              <button className="text-button" onClick={() => openEditChallanForm(challan)} type="button">Edit</button>
                              <button className="text-button" onClick={() => void handlePostChallan(challan)} type="button">Post</button>
                            </>
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
          ) : null}
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to="/app/customers">Back to customers</Link></p>
    </section>
  )
}
