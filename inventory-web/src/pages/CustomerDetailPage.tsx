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
  type PagedResponse,
  type SalesInvoice,
  type SalesInvoiceStatus,
} from '../features/salesInvoices/salesInvoicesApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate } from '../shared/utils/formatters'

const detailPageSize = 20

type InvoiceStatusFilter = 'outstanding' | 'all' | 'draft' | 'unpaid' | 'partial' | 'paid' | 'cancelled'
type DateFilter = 'all' | 'today' | 'thisWeek' | 'thisMonth' | 'lastMonth' | 'last30Days' | 'custom'

function matchesInvoiceStatusFilter(invoice: SalesInvoice, statusFilter: InvoiceStatusFilter): boolean {
  if (statusFilter === 'all') {
    return true
  }

  if (statusFilter === 'outstanding') {
    return invoice.status === 1 || invoice.status === 2
  }

  if (statusFilter === 'draft') {
    return invoice.status === 0
  }

  if (statusFilter === 'unpaid') {
    return invoice.status === 1
  }

  if (statusFilter === 'partial') {
    return invoice.status === 2
  }

  if (statusFilter === 'paid') {
    return invoice.status === 3
  }

  return invoice.status === 4
}

function getApiStatusFilter(statusFilter: InvoiceStatusFilter): string {
  const statusMap: Partial<Record<InvoiceStatusFilter, string>> = {
    draft: '0',
    unpaid: '1',
    partial: '2',
    paid: '3',
    cancelled: '4',
  }

  return statusMap[statusFilter] ?? ''
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

function formatDateInputValue(date: Date): string {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

function getDateRange(dateFilter: DateFilter, customFromDate: string, customToDate: string): { dateFrom: string, dateTo: string } {
  const today = new Date()

  if (dateFilter === 'all') {
    return { dateFrom: '', dateTo: '' }
  }

  if (dateFilter === 'custom') {
    return { dateFrom: customFromDate, dateTo: customToDate }
  }

  const start = new Date(today)
  const end = new Date(today)

  if (dateFilter === 'today') {
    return { dateFrom: formatDateInputValue(today), dateTo: formatDateInputValue(today) }
  }

  if (dateFilter === 'thisWeek') {
    const day = start.getDay()
    const offset = day === 0 ? -6 : 1 - day
    start.setDate(start.getDate() + offset)
  }

  if (dateFilter === 'thisMonth') {
    start.setDate(1)
  }

  if (dateFilter === 'lastMonth') {
    start.setMonth(start.getMonth() - 1, 1)
    end.setDate(0)
  }

  if (dateFilter === 'last30Days') {
    start.setDate(start.getDate() - 29)
  }

  return { dateFrom: formatDateInputValue(start), dateTo: formatDateInputValue(end) }
}

function getDisplayInvoiceStatusLabel(status: SalesInvoiceStatus): string {
  if (status === 1) {
    return 'Unpaid'
  }

  return getSalesInvoiceStatusLabel(status)
}

function getInvoiceStatusClassName(status: SalesInvoiceStatus): string {
  const statusClasses: Record<SalesInvoiceStatus, string> = {
    0: 'draft',
    1: 'unpaid',
    2: 'partial',
    3: 'paid',
    4: 'cancelled',
  }

  return `invoice-status-badge ${statusClasses[status]}`
}

export function CustomerDetailPage() {
  const { id } = useParams()
  const { currentUser } = useAuth()
  const canCreateInvoices = hasRouteAccess(currentUser?.roles ?? [], 'manageSalesInvoices')
  const canReceivePayments = hasRouteAccess(currentUser?.roles ?? [], 'viewPayments')
  const canViewLedger = hasRouteAccess(currentUser?.roles ?? [], 'viewCustomerStatements')
  const canManageCustomers = hasRouteAccess(currentUser?.roles ?? [], 'manageCustomers')
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [invoiceResponse, setInvoiceResponse] = useState<PagedResponse<SalesInvoice> | null>(null)
  const [accountOutstandingInvoiceCount, setAccountOutstandingInvoiceCount] = useState(0)
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
  const [pageNumber, setPageNumber] = useState(1)
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [statusFilter, setStatusFilter] = useState<InvoiceStatusFilter>('outstanding')
  const [dateFilter, setDateFilter] = useState<DateFilter>('all')
  const [customFromDate, setCustomFromDate] = useState('')
  const [customToDate, setCustomToDate] = useState('')
  const [openInvoiceActionsId, setOpenInvoiceActionsId] = useState<number | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [customerFieldErrors, setCustomerFieldErrors] = useState<FieldErrors>({})
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const customerId = Number(id)
  const dateRange = getDateRange(dateFilter, customFromDate, customToDate)

  const loadCustomerDetail = useCallback(async (): Promise<void> => {
    if (!customerId) {
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [
        customerResponse,
        invoiceResponse,
        unpaidInvoiceResponse,
        partialInvoiceResponse,
        driverResponse,
        productResponse,
      ] = await Promise.all([
        getCustomer(customerId),
        getSalesInvoices({
          pageNumber,
          pageSize: detailPageSize,
          customerId: customerId.toString(),
          status: getApiStatusFilter(statusFilter),
          invoiceNumber,
          dateFrom: dateRange.dateFrom,
          dateTo: dateRange.dateTo,
        }),
        getSalesInvoices({
          pageNumber: 1,
          pageSize: 1,
          customerId: customerId.toString(),
          status: '1',
          invoiceNumber: '',
        }),
        getSalesInvoices({
          pageNumber: 1,
          pageSize: 1,
          customerId: customerId.toString(),
          status: '2',
          invoiceNumber: '',
        }),
        getDrivers(1, 100, '', 'true'),
        getProducts(1, 100),
      ])

      setCustomer(customerResponse)
      setInvoiceResponse(invoiceResponse)
      setAccountOutstandingInvoiceCount(unpaidInvoiceResponse.totalCount + partialInvoiceResponse.totalCount)
      setInvoices(invoiceResponse.items)
      setDrivers(driverResponse.items)
      setProducts(productResponse.items)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [customerId, dateRange.dateFrom, dateRange.dateTo, invoiceNumber, pageNumber, statusFilter])

  useEffect(() => {
    void loadCustomerDetail()
  }, [loadCustomerDetail])

  function handleFilters(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
  }

  function clearFilters(): void {
    setInvoiceNumber('')
    setStatusFilter('outstanding')
    setDateFilter('all')
    setCustomFromDate('')
    setCustomToDate('')
    setPageNumber(1)
    setOpenInvoiceActionsId(null)
  }

  function handleInvoiceSearch(value: string): void {
    setInvoiceNumber(value)
    setPageNumber(1)
    setOpenInvoiceActionsId(null)
  }

  function handleStatusFilter(value: InvoiceStatusFilter): void {
    setStatusFilter(value)
    setPageNumber(1)
    setOpenInvoiceActionsId(null)
  }

  function handleDateFilter(value: DateFilter): void {
    setDateFilter(value)
    if (value !== 'custom') {
      setCustomFromDate('')
      setCustomToDate('')
    }
    setPageNumber(1)
    setOpenInvoiceActionsId(null)
  }

  function handleCustomFromDate(value: string): void {
    setCustomFromDate(value)
    setPageNumber(1)
    setOpenInvoiceActionsId(null)
  }

  function handleCustomToDate(value: string): void {
    setCustomToDate(value)
    setPageNumber(1)
    setOpenInvoiceActionsId(null)
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
    setOpenInvoiceActionsId(null)
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
    matchesInvoiceStatusFilter(invoice, statusFilter),
  )
  const customerMetaItems = customer ? getCustomerMetaItems(customer) : []
  const hasActiveFilters = invoiceNumber.trim().length > 0
    || statusFilter !== 'outstanding'
    || dateFilter !== 'all'
  const hasAnyInvoices = (invoiceResponse?.totalCount ?? 0) > 0
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
          <div className="customer-account-summary-grid">
            <article className="customer-account-summary-card balance-due">
              <span>Balance due</span>
              <strong>₹{formatCurrency(customer.balanceDue)}</strong>
            </article>
            <article className="customer-account-summary-card">
              <span>Outstanding</span>
              <strong>{accountOutstandingInvoiceCount} {accountOutstandingInvoiceCount === 1 ? 'invoice' : 'invoices'}</strong>
              <small>₹{formatCurrency(customer.balanceDue)} total</small>
            </article>
            <article className="customer-account-summary-card">
              <span>Credit limit</span>
              <strong>₹{formatCurrency(customer.creditLimit)}</strong>
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

          <section className="customer-invoices-section" aria-labelledby="customer-invoices-title">
            <h2 id="customer-invoices-title">Invoices</h2>
            <form className="customer-invoice-toolbar" onSubmit={handleFilters}>
              <input
                aria-label="Search invoices"
                onChange={(event) => handleInvoiceSearch(event.target.value)}
                placeholder="Search invoices"
                type="search"
                value={invoiceNumber}
              />
              <select
                aria-label="Invoice status"
                onChange={(event) => handleStatusFilter(event.target.value as InvoiceStatusFilter)}
                value={statusFilter}
              >
                <option value="all">All invoices</option>
                <option value="outstanding">Outstanding</option>
                <option value="draft">Draft</option>
                <option value="unpaid">Unpaid</option>
                <option value="partial">Partially paid</option>
                <option value="paid">Paid</option>
                <option value="cancelled">Cancelled</option>
              </select>
              <select
                aria-label="Invoice date range"
                onChange={(event) => handleDateFilter(event.target.value as DateFilter)}
                value={dateFilter}
              >
                <option value="all">Date</option>
                <option value="today">Today</option>
                <option value="thisWeek">This week</option>
                <option value="thisMonth">This month</option>
                <option value="lastMonth">Last month</option>
                <option value="last30Days">Last 30 days</option>
                <option value="custom">Custom range</option>
              </select>
              {dateFilter === 'custom' ? (
                <>
                  <input aria-label="From date" onChange={(event) => handleCustomFromDate(event.target.value)} type="date" value={customFromDate} />
                  <input aria-label="To date" onChange={(event) => handleCustomToDate(event.target.value)} type="date" value={customToDate} />
                </>
              ) : null}
              {hasActiveFilters ? (
                <button className="customer-invoice-clear" onClick={clearFilters} type="button">Clear</button>
              ) : null}
            </form>

            <div className="customer-invoice-table-wrap">
              {isLoading ? <LoadingState>Loading invoices...</LoadingState> : null}
              {!isLoading && !errorMessage && visibleInvoices.length === 0 ? (
                <div className="customer-invoice-empty">
                  <strong>{hasAnyInvoices ? 'No invoices match your filters.' : 'No invoices yet'}</strong>
                  {!hasAnyInvoices ? <span>Create the first invoice for this customer.</span> : null}
                  {hasAnyInvoices && hasActiveFilters ? (
                    <button className="customer-invoice-clear" onClick={clearFilters} type="button">Clear filters</button>
                  ) : null}
                  {!hasAnyInvoices && canCreateInvoices && products.length > 0 ? (
                    <button className="customer-account-action primary" onClick={openNewDirectInvoiceForm} type="button">+ New Invoice</button>
                  ) : null}
                </div>
              ) : null}
              {visibleInvoices.length > 0 ? (
                <table className="customer-invoice-table">
                  <thead>
                    <tr>
                      <th>Invoice</th>
                      <th>Date</th>
                      <th>Status</th>
                      <th className="numeric-cell">Total</th>
                      <th className="numeric-cell">Paid</th>
                      <th className="numeric-cell">Due</th>
                      <th className="actions-cell">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleInvoices.map((invoice) => (
                      <tr key={invoice.id}>
                        <td>
                          {canCreateInvoices ? (
                            <Link className="invoice-number-link" to={`/app/sales-invoices/${invoice.id}`}>{invoice.invoiceNumber}</Link>
                          ) : (
                            <span className="invoice-number-text">{invoice.invoiceNumber}</span>
                          )}
                        </td>
                        <td>{formatDate(invoice.invoiceDate)}</td>
                        <td>
                          <span className={getInvoiceStatusClassName(invoice.status as SalesInvoiceStatus)}>
                            {getDisplayInvoiceStatusLabel(invoice.status as SalesInvoiceStatus)}
                          </span>
                        </td>
                        <td className="numeric-cell">₹{formatCurrency(invoice.grandTotal)}</td>
                        <td className="numeric-cell">₹{formatCurrency(invoice.amountPaid)}</td>
                        <td className="numeric-cell due-cell">₹{formatCurrency(invoice.balanceDue)}</td>
                        <td className="actions-cell">
                          <div className="invoice-row-actions">
                            {canReceivePayments && canReceivePaymentForInvoice(invoice) ? (
                              <button className="invoice-row-action" onClick={() => openPaymentForm(invoice)} type="button">Receive Payment</button>
                            ) : null}
                            {canCreateInvoices ? (
                              <div className="invoice-more-menu">
                                <button
                                  aria-expanded={openInvoiceActionsId === invoice.id}
                                  aria-label={`More actions for invoice ${invoice.invoiceNumber}`}
                                  className="invoice-more-button"
                                  onClick={() => setOpenInvoiceActionsId((current) => current === invoice.id ? null : invoice.id)}
                                  type="button"
                                >
                                  ...
                                </button>
                                {openInvoiceActionsId === invoice.id ? (
                                  <div className="invoice-more-menu-panel">
                                    <Link className="invoice-menu-item" to={`/app/sales-invoices/${invoice.id}`}>View invoice</Link>
                                    {invoice.status === 0 ? (
                                      <button className="invoice-menu-item" onClick={() => void handlePostInvoice(invoice)} type="button">Post</button>
                                    ) : null}
                                  </div>
                                ) : null}
                              </div>
                            ) : null}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : null}
            </div>
            {invoiceResponse && invoiceResponse.totalPages > 1 ? (
              <div className="customer-invoice-pagination">
                <span>
                  Showing {(invoiceResponse.pageNumber - 1) * invoiceResponse.pageSize + 1}-{Math.min(invoiceResponse.pageNumber * invoiceResponse.pageSize, invoiceResponse.totalCount)} of {invoiceResponse.totalCount}
                </span>
                <div>
                  <button disabled={!invoiceResponse.hasPreviousPage} onClick={() => setPageNumber((current) => Math.max(1, current - 1))} type="button">Previous</button>
                  <span>Page {invoiceResponse.pageNumber} of {invoiceResponse.totalPages}</span>
                  <button disabled={!invoiceResponse.hasNextPage} onClick={() => setPageNumber((current) => current + 1)} type="button">Next</button>
                </div>
              </div>
            ) : null}
          </section>
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to="/app/customers">Back to customers</Link></p>
    </section>
  )
}
