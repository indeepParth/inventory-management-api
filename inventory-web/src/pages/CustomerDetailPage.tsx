import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { getDrivers, type Driver } from '../features/drivers/driversApi'
import { Link, useParams } from 'react-router-dom'
import { CustomerForm } from '../features/parties/CustomerForm'
import {
  getCustomer,
  updateCustomer,
  type Customer,
  type CustomerFormValues,
} from '../features/parties/partiesApi'
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

function canReceivePaymentForInvoice(invoice: SalesInvoice): boolean {
  return (invoice.status === 1 || invoice.status === 2) && invoice.balanceDue > 0
}

function getCustomerMetaItems(customer: Customer): string[] {
  return [
    customer.contactPerson,
    customer.phone || customer.email,
  ].filter((value): value is string => Boolean(value))
}

export function CustomerDetailPage() {
  const { id } = useParams()
  const { currentUser } = useAuth()
  const canCreateInvoices = hasRouteAccess(currentUser?.roles ?? [], 'manageSalesInvoices')
  const canReceivePayments = hasRouteAccess(currentUser?.roles ?? [], 'viewPayments')
  const canViewLedger = hasRouteAccess(currentUser?.roles ?? [], 'viewCustomerStatements')
  const canManageCustomers = hasRouteAccess(currentUser?.roles ?? [], 'manageCustomers')
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [invoices, setInvoices] = useState<SalesInvoice[]>([])
  const [drivers, setDrivers] = useState<Driver[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [paymentInvoice, setPaymentInvoice] = useState<SalesInvoice | undefined>()
  const [isCustomerDetailsOpen, setIsCustomerDetailsOpen] = useState(false)
  const [isCustomerDetailsEditing, setIsCustomerDetailsEditing] = useState(false)
  const [isDirectInvoiceFormOpen, setIsDirectInvoiceFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSavingCustomer, setIsSavingCustomer] = useState(false)
  const [isSavingDirectInvoice, setIsSavingDirectInvoice] = useState(false)
  const [isSavingPayment, setIsSavingPayment] = useState(false)
  const [fromDateInput, setFromDateInput] = useState('')
  const [toDateInput, setToDateInput] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [paymentFilter, setPaymentFilter] = useState<PaymentFilter>('outstanding')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [customerFieldErrors, setCustomerFieldErrors] = useState<FieldErrors>({})
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const customerId = Number(id)

  const loadCustomerDetail = useCallback(async (): Promise<void> => {
    if (!customerId) {
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [customerResponse, invoiceResponse, driverResponse, productResponse] = await Promise.all([
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
        getDrivers(1, 100, '', 'true'),
        getProducts(1, 100),
      ])

      setCustomer(customerResponse)
      setInvoices(invoiceResponse.items.filter(isCollectionInvoice))
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

  function openNewDirectInvoiceForm(): void {
    setFieldErrors({})
    setCustomerFieldErrors({})
    setActionError(null)
    setIsCustomerDetailsOpen(false)
    setIsCustomerDetailsEditing(false)
    setPaymentInvoice(undefined)
    setIsDirectInvoiceFormOpen(true)
  }

  function closeDirectInvoiceForm(): void {
    setFieldErrors({})
    setActionError(null)
    setIsDirectInvoiceFormOpen(false)
  }

  function openPaymentForm(invoice: SalesInvoice): void {
    setFieldErrors({})
    setCustomerFieldErrors({})
    setActionError(null)
    setIsCustomerDetailsOpen(false)
    setIsCustomerDetailsEditing(false)
    setIsDirectInvoiceFormOpen(false)
    setPaymentInvoice(invoice)
  }

  function closePaymentForm(): void {
    setFieldErrors({})
    setActionError(null)
    setPaymentInvoice(undefined)
  }

  function openCustomerDetails(): void {
    setFieldErrors({})
    setCustomerFieldErrors({})
    setActionError(null)
    setIsDirectInvoiceFormOpen(false)
    setPaymentInvoice(undefined)
    setIsCustomerDetailsOpen(true)
    setIsCustomerDetailsEditing(false)
  }

  function editCustomerDetails(): void {
    setCustomerFieldErrors({})
    setActionError(null)
    setIsCustomerDetailsEditing(true)
  }

  function cancelCustomerDetails(): void {
    setCustomerFieldErrors({})
    setActionError(null)

    if (isCustomerDetailsEditing) {
      setIsCustomerDetailsEditing(false)
      return
    }

    setIsCustomerDetailsOpen(false)
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

  async function handleCustomerSubmit(values: CustomerFormValues): Promise<void> {
    if (!customer) {
      return
    }

    setIsSavingCustomer(true)
    setCustomerFieldErrors({})
    setActionError(null)

    try {
      setCustomer(await updateCustomer(customer.id, values))
      setIsCustomerDetailsEditing(false)
      await loadCustomerDetail()
    } catch (error) {
      setCustomerFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSavingCustomer(false)
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

  const visibleInvoices = invoices.filter((invoice) =>
    matchesPaymentFilter(invoice, paymentFilter),
  )
  const outstandingInvoiceTotal = getInvoiceOutstandingTotal(invoices)
  const outstandingInvoiceCount = invoices.filter((invoice) =>
    invoice.status === 1 || invoice.status === 2,
  ).length
  const customerMetaItems = customer ? getCustomerMetaItems(customer) : []
  return (
    <section className="content-panel wide-panel" aria-labelledby="customer-detail-title">
      <div className="customer-account-header">
        <div className="customer-account-identity">
          <h1 id="customer-detail-title" className="customer-account-name">{customer?.name ?? 'Customer detail'}</h1>
          {customer ? (
            <div className="customer-account-meta">
              {customerMetaItems.map((item) => (
                <span key={item}>{item}</span>
              ))}
              <span className="customer-status-inline">
                <span aria-hidden="true" className={customer.isActive ? 'customer-status-dot active' : 'customer-status-dot inactive'} />
                {customer.isActive ? 'Active' : 'Inactive'}
              </span>
            </div>
          ) : null}
        </div>
        {customer || canCreateInvoices ? (
          <div className="customer-account-actions">
            {customer ? (
              <button className="customer-account-action ghost" onClick={openCustomerDetails} type="button">Details</button>
            ) : null}
            {canViewLedger && customer ? (
              <Link className="customer-account-action secondary" to={`/app/customers/${customer.id}/ledger`}>Ledger</Link>
            ) : null}
            {canCreateInvoices ? (
              <button className="customer-account-action primary" disabled={products.length === 0} onClick={openNewDirectInvoiceForm} type="button">+ New Invoice</button>
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
          </div>

          {isCustomerDetailsOpen ? (
            <CustomerForm
              canEdit={canManageCustomers}
              errors={customerFieldErrors}
              initialValue={customer}
              isReadOnly={!isCustomerDetailsEditing}
              isSubmitting={isSavingCustomer}
              onCancel={cancelCustomerDetails}
              onEdit={editCustomerDetails}
              onSubmit={handleCustomerSubmit}
            />
          ) : null}

          {isDirectInvoiceFormOpen ? (
            <DirectInvoiceForm
              customers={[customer]}
              drivers={drivers}
              errors={fieldErrors}
              initialCustomerId={customer.id}
              initialDeliveryAddress={customer.deliveryAddress}
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

          {products.length === 0 && canCreateInvoices && !isLoading ? (
            <p className="state-message">Create at least one product before adding invoices for this customer.</p>
          ) : null}

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
                      <td>
                        {canCreateInvoices ? (
                          <Link className="text-link" to={`/app/sales-invoices/${invoice.id}`}>{invoice.invoiceNumber}</Link>
                        ) : (
                          invoice.invoiceNumber
                        )}
                      </td>
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
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to="/app/customers">Back to customers</Link></p>
    </section>
  )
}
