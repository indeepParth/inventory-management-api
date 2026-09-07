import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getPaymentMethodLabel } from '../features/payments/paymentsApi'
import { getReturnStatusLabel } from '../features/returns/returnsApi'
import {
  getSalesInvoice,
  getSalesInvoiceStatusLabel,
  type SalesInvoiceDetail,
  type SalesInvoicePayment,
} from '../features/salesInvoices/salesInvoicesApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate, formatQuantity } from '../shared/utils/formatters'

function formatOptionalDate(value?: string): string {
  return value ? formatDate(value) : '-'
}

function getInvoiceSourceType(invoice: SalesInvoiceDetail): string {
  return invoice.items.some((item) => item.deliveryChallanItemId)
    ? 'From challans'
    : 'Direct'
}

function getOtherChargesLabel(invoice: SalesInvoiceDetail): string {
  if (invoice.items.some((item) => item.deliveryChallanItemId)) {
    return 'Delivery / other charges'
  }

  return invoice.driverId ? 'Driver charge' : 'Other charges'
}

function getPaymentState(payment: SalesInvoicePayment): string {
  if (payment.reversesPaymentId) {
    return `Reversal of #${payment.reversesPaymentId}`
  }

  if (payment.reversalPaymentId) {
    return `Reversed by #${payment.reversalPaymentId}`
  }

  return 'Posted'
}

type DetailItem = {
  label: string
  value: string
  isEmphasized?: boolean
}

type DetailSectionProps = {
  title: string
  items: DetailItem[]
}

function DetailSection({ title, items }: DetailSectionProps) {
  return (
    <section className="invoice-detail-section" aria-labelledby={`invoice-${title.toLowerCase().replace(/\W+/g, '-')}`}>
      <h2 id={`invoice-${title.toLowerCase().replace(/\W+/g, '-')}`}>{title}</h2>
      <dl className="invoice-detail-list">
        {items.map((item) => (
          <div className={item.isEmphasized ? 'invoice-detail-row emphasized' : 'invoice-detail-row'} key={item.label}>
            <dt>{item.label}</dt>
            <dd>{item.value}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}

export function SalesInvoiceDetailPage() {
  const { id } = useParams()
  const invoiceId = Number(id)
  const [invoice, setInvoice] = useState<SalesInvoiceDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    async function loadInvoice(): Promise<void> {
      if (!Number.isInteger(invoiceId) || invoiceId <= 0) {
        setErrorMessage('Sales invoice not found.')
        setIsLoading(false)
        return
      }

      setIsLoading(true)
      setErrorMessage(null)

      try {
        setInvoice(await getSalesInvoice(invoiceId))
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsLoading(false)
      }
    }

    void loadInvoice()
  }, [invoiceId])

  return (
    <section className="content-panel wide-panel" aria-labelledby="invoice-detail-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Sales invoice</p>
          <h1 id="invoice-detail-title" className="page-title">
            {invoice?.invoiceNumber ?? 'Invoice detail'}
          </h1>
        </div>
      </div>

      {isLoading ? <LoadingState>Loading sales invoice...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {invoice ? (
        <>
          <div className="invoice-detail-summary">
            <span className="status-pill success invoice-detail-status">{getSalesInvoiceStatusLabel(invoice.status)}</span>

            <div className="invoice-detail-summary-grid">
              <DetailSection
                title="Customer & Source"
                items={[
                  { label: 'Customer', value: invoice.customerName },
                  { label: 'Invoice date', value: formatDate(invoice.invoiceDate) },
                  { label: 'Source type', value: getInvoiceSourceType(invoice) },
                ]}
              />
              <DetailSection
                title="Delivery / Driver"
                items={[
                  { label: 'Driver', value: invoice.driverName || '-' },
                  { label: 'Delivery address', value: invoice.deliveryAddress || '-' },
                  {
                    label: 'Driver charge status',
                    value: invoice.otherCharges > 0
                      ? invoice.isDeliveryChargePaid ? 'Paid' : 'Unpaid'
                      : '-',
                  },
                ]}
              />
              <DetailSection
                title="Timeline / Notes"
                items={[
                  { label: 'Created', value: formatOptionalDate(invoice.createdAtUtc) },
                  { label: 'Updated', value: formatOptionalDate(invoice.updatedAtUtc) },
                  { label: 'Posted', value: formatOptionalDate(invoice.postedAtUtc) },
                  { label: 'Cancelled', value: formatOptionalDate(invoice.cancelledAtUtc) },
                  { label: 'Created by', value: invoice.createdBy || '-' },
                  { label: 'Notes', value: invoice.notes || '-' },
                ]}
              />
              <DetailSection
                title="Amount Summary"
                items={[
                  { label: 'Subtotal', value: formatCurrency(invoice.subtotal) },
                  { label: 'Discount', value: formatCurrency(invoice.discount) },
                  { label: 'Tax amount', value: formatCurrency(invoice.taxAmount) },
                  { label: getOtherChargesLabel(invoice), value: formatCurrency(invoice.otherCharges) },
                  { label: 'Labor charge', value: formatCurrency(invoice.laborCharge) },
                  { label: 'Grand total', value: formatCurrency(invoice.grandTotal), isEmphasized: true },
                  { label: 'Amount paid', value: formatCurrency(invoice.amountPaid) },
                  { label: 'Balance due', value: formatCurrency(invoice.balanceDue), isEmphasized: true },
                ]}
              />
            </div>
          </div>

          <h2>Items</h2>
          {invoice.items.length === 0 ? <EmptyState>No invoice items found.</EmptyState> : null}
          {invoice.items.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Product</th>
                    <th>SKU</th>
                    <th>Quantity</th>
                    <th>Unit price</th>
                    <th>Tax rate</th>
                    <th>Tax amount</th>
                    <th>Line total</th>
                    <th>Cost at sale</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.items.map((item) => (
                    <tr key={item.id}>
                      <td>{item.productName}</td>
                      <td>{item.productSku}</td>
                      <td>{formatQuantity(item.quantity)}</td>
                      <td>{formatCurrency(item.sellingUnitPrice)}</td>
                      <td>{formatQuantity(item.taxRate)}%</td>
                      <td>{formatCurrency(item.taxAmount)}</td>
                      <td>{formatCurrency(item.lineTotal)}</td>
                      <td>{item.costAtSale == null ? '-' : formatCurrency(item.costAtSale)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
          <h2>Payments</h2>
          {invoice.payments.length === 0 ? <EmptyState>No payments linked.</EmptyState> : null}
          {invoice.payments.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Receipt</th>
                    <th>Date</th>
                    <th>Method</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Reference</th>
                    <th>Note</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.payments.map((payment) => (
                    <tr key={payment.id}>
                      <td>{payment.receiptNumber}</td>
                      <td>{formatDate(payment.paymentDate)}</td>
                      <td>{getPaymentMethodLabel(payment.method)}</td>
                      <td>{formatCurrency(payment.amount)}</td>
                      <td>{getPaymentState(payment)}</td>
                      <td>{payment.externalReference || '-'}</td>
                      <td>{payment.note || '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}

          <h2>Customer returns</h2>
          {invoice.customerReturns.length === 0 ? <EmptyState>No customer returns linked.</EmptyState> : null}
          {invoice.customerReturns.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Return</th>
                    <th>Date</th>
                    <th>Status</th>
                    <th>Total credit</th>
                    <th>Posted</th>
                    <th>Cancelled</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.customerReturns.map((customerReturn) => (
                    <tr key={customerReturn.id}>
                      <td>{customerReturn.returnNumber}</td>
                      <td>{formatDate(customerReturn.returnDate)}</td>
                      <td>{getReturnStatusLabel(customerReturn.status)}</td>
                      <td>{formatCurrency(customerReturn.grandTotal)}</td>
                      <td>{formatOptionalDate(customerReturn.postedAtUtc)}</td>
                      <td>{formatOptionalDate(customerReturn.cancelledAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to="/app/sales-invoices">Back to invoices</Link></p>
    </section>
  )
}
