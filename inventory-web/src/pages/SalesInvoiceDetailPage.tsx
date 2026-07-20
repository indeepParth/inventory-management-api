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

function getPaymentState(payment: SalesInvoicePayment): string {
  if (payment.reversesPaymentId) {
    return `Reversal of #${payment.reversesPaymentId}`
  }

  if (payment.reversalPaymentId) {
    return `Reversed by #${payment.reversalPaymentId}`
  }

  return 'Posted'
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
          <div className="detail-grid">
            <span>Status</span><strong>{getSalesInvoiceStatusLabel(invoice.status)}</strong>
            <span>Invoice date</span><strong>{formatDate(invoice.invoiceDate)}</strong>
            <span>Customer</span><strong>{invoice.customerName}</strong>
            <span>Source type</span><strong>{getInvoiceSourceType(invoice)}</strong>
            <span>Created by</span><strong>{invoice.createdBy || '-'}</strong>
            <span>Subtotal</span><strong>{formatCurrency(invoice.subtotal)}</strong>
            <span>Discount</span><strong>{formatCurrency(invoice.discount)}</strong>
            <span>Tax amount</span><strong>{formatCurrency(invoice.taxAmount)}</strong>
            <span>Other charges</span><strong>{formatCurrency(invoice.otherCharges)}</strong>
            <span>Grand total</span><strong>{formatCurrency(invoice.grandTotal)}</strong>
            <span>Amount paid</span><strong>{formatCurrency(invoice.amountPaid)}</strong>
            <span>Balance due</span><strong>{formatCurrency(invoice.balanceDue)}</strong>
            <span>Notes</span><strong>{invoice.notes || '-'}</strong>
            <span>Created</span><strong>{formatOptionalDate(invoice.createdAtUtc)}</strong>
            <span>Updated</span><strong>{formatOptionalDate(invoice.updatedAtUtc)}</strong>
            <span>Posted</span><strong>{formatOptionalDate(invoice.postedAtUtc)}</strong>
            <span>Cancelled</span><strong>{formatOptionalDate(invoice.cancelledAtUtc)}</strong>
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
                    <th>Source challan</th>
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
                      <td>
                        {item.deliveryChallanId && item.deliveryChallanNumber ? (
                          <Link className="text-link" to={`/app/challans/${item.deliveryChallanId}`}>{item.deliveryChallanNumber}</Link>
                        ) : (
                          '-'
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}

          <h2>Source challans</h2>
          {invoice.sourceChallans.length === 0 ? <EmptyState>No source challans linked.</EmptyState> : null}
          {invoice.sourceChallans.length > 0 ? (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Challan</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.sourceChallans.map((challan) => (
                    <tr key={challan.id}>
                      <td><Link className="text-link" to={`/app/challans/${challan.id}`}>{challan.challanNumber}</Link></td>
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
