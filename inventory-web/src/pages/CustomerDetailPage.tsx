import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { getDeliveryChallans, type DeliveryChallan } from '../features/challans/challansApi'
import { Link, useParams } from 'react-router-dom'
import { getCustomer, type Customer } from '../features/parties/partiesApi'
import {
  getSalesInvoices,
  getSalesInvoiceStatusLabel,
  type SalesInvoice,
  type SalesInvoiceStatus,
} from '../features/salesInvoices/salesInvoicesApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate } from '../shared/utils/formatters'

const detailPageSize = 100

type PaymentFilter = 'outstanding' | 'all' | 'unpaid' | 'partial' | 'paid'

function isCollectionInvoice(invoice: SalesInvoice): boolean {
  return invoice.status === 1 || invoice.status === 2 || invoice.status === 3
}

function matchesPaymentFilter(invoice: SalesInvoice, paymentFilter: PaymentFilter): boolean {
  if (paymentFilter === 'all') {
    return isCollectionInvoice(invoice)
  }

  if (paymentFilter === 'outstanding') {
    return invoice.status === 1 || invoice.status === 2
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

export function CustomerDetailPage() {
  const { id } = useParams()
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [invoices, setInvoices] = useState<SalesInvoice[]>([])
  const [openChallans, setOpenChallans] = useState<DeliveryChallan[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [fromDateInput, setFromDateInput] = useState('')
  const [toDateInput, setToDateInput] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [paymentFilter, setPaymentFilter] = useState<PaymentFilter>('outstanding')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const customerId = Number(id)

  const loadCustomerDetail = useCallback(async (): Promise<void> => {
    if (!customerId) {
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [customerResponse, invoiceResponse, challanResponse] = await Promise.all([
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
          status: '1',
          challanNumber: '',
          dateFrom: fromDate,
          dateTo: toDate,
        }),
      ])

      setCustomer(customerResponse)
      setInvoices(invoiceResponse.items.filter(isCollectionInvoice))
      setOpenChallans(challanResponse.items.filter((challan) => !challan.invoicedAtUtc))
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

  const visibleInvoices = invoices.filter((invoice) =>
    matchesPaymentFilter(invoice, paymentFilter),
  )
  const outstandingInvoiceTotal = getInvoiceOutstandingTotal(invoices)
  const outstandingInvoiceCount = invoices.filter((invoice) =>
    invoice.status === 1 || invoice.status === 2,
  ).length

  return (
    <section className="content-panel wide-panel" aria-labelledby="customer-detail-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Customer account</p>
          <h1 id="customer-detail-title" className="page-title">{customer?.name ?? 'Customer detail'}</h1>
        </div>
      </div>

      {isLoading ? <LoadingState>Loading customer...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

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
              <span>Open challans</span>
              <strong>{openChallans.length}</strong>
            </article>
          </div>

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
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}

          <h2>Open posted challans</h2>
          {openChallans.length === 0 ? <EmptyState>No open posted challans found.</EmptyState> : null}
          {openChallans.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Challan</th>
                    <th>Date</th>
                    <th>Items</th>
                    <th>Delivery address</th>
                  </tr>
                </thead>
                <tbody>
                  {openChallans.map((challan) => (
                    <tr key={challan.id}>
                      <td>{challan.challanNumber}</td>
                      <td>{formatDate(challan.challanDate)}</td>
                      <td>{challan.items.length}</td>
                      <td>{challan.deliveryAddress || '-'}</td>
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
