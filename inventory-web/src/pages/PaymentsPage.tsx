import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { getCustomers, getSuppliers, type Customer, type Supplier } from '../features/parties/partiesApi'
import { PaymentForm } from '../features/payments/PaymentForm'
import {
  createPayment,
  getPaymentMethodLabel,
  getPayments,
  reversePayment,
  type PagedResponse,
  type Payment,
  type PaymentFormValues,
  type PaymentMethod,
} from '../features/payments/paymentsApi'
import { getPurchases, type Purchase } from '../features/purchases/purchasesApi'
import { getSalesInvoices, type SalesInvoice } from '../features/salesInvoices/salesInvoicesApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate, toDateInputValue } from '../shared/utils/formatters'

const pageSize = 10

type PaymentMode = 'customer' | 'supplier'

function getPartyName(payment: Payment): string {
  return payment.customerName ?? payment.supplierName ?? '-'
}

function getDocumentName(payment: Payment): string {
  return payment.invoiceNumber ?? payment.purchaseNumber ?? 'Party balance'
}

function getPaymentState(payment: Payment): string {
  if (payment.reversesPaymentId) {
    return `Reversal of #${payment.reversesPaymentId}`
  }

  if (payment.reversalPaymentId) {
    return `Reversed by #${payment.reversalPaymentId}`
  }

  return 'Posted'
}

export function PaymentsPage() {
  const { currentUser } = useAuth()
  const canReversePayments = hasRouteAccess(currentUser?.roles ?? [], 'adminOrManager')
  const canViewInvoices = hasRouteAccess(currentUser?.roles ?? [], 'manageSalesInvoices')
  const [response, setResponse] = useState<PagedResponse<Payment> | null>(null)
  const [customers, setCustomers] = useState<Customer[]>([])
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [invoices, setInvoices] = useState<SalesInvoice[]>([])
  const [purchases, setPurchases] = useState<Purchase[]>([])
  const [mode, setMode] = useState<PaymentMode>('customer')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [receiptNumberInput, setReceiptNumberInput] = useState('')
  const [receiptNumber, setReceiptNumber] = useState('')
  const [method, setMethod] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadPayments = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [paymentPage, customerPage, supplierPage, invoicePage, purchasePage] = await Promise.all([
        getPayments({
          pageNumber,
          pageSize,
          receiptNumber,
          method,
        }),
        getCustomers(1, 100, '', 'true'),
        getSuppliers(1, 100, '', 'true'),
        getSalesInvoices({
          pageNumber: 1,
          pageSize: 100,
          customerId: '',
          status: '',
          invoiceNumber: '',
        }),
        getPurchases({
          pageNumber: 1,
          pageSize: 100,
          supplierId: '',
          status: '',
          purchaseNumber: '',
          supplierBillNumber: '',
        }),
      ])
      setResponse(paymentPage)
      setCustomers(customerPage.items)
      setSuppliers(supplierPage.items)
      setInvoices(invoicePage.items)
      setPurchases(purchasePage.items)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [method, pageNumber, receiptNumber])

  useEffect(() => {
    void loadPayments()
  }, [loadPayments])

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setReceiptNumber(receiptNumberInput.trim())
  }

  async function handleSubmit(values: PaymentFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      await createPayment(values)
      await loadPayments()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleReverse(payment: Payment): Promise<void> {
    const receiptNumberForReversal = window.prompt(
      `Enter reversal receipt number for "${payment.receiptNumber}"`,
    )

    if (!receiptNumberForReversal) {
      return
    }

    const confirmed = window.confirm(`Reverse payment "${payment.receiptNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await reversePayment(payment.id, {
        receiptNumber: receiptNumberForReversal,
        paymentDate: toDateInputValue(),
        externalReference: '',
        note: `Reversal for ${payment.receiptNumber}`,
      })
      await loadPayments()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const payments = response?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="payments-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Receipts and payouts</p>
          <h1 id="payments-title" className="page-title">Payments</h1>
        </div>
        <div className="form-actions">
          <button className={mode === 'customer' ? 'primary-button' : 'secondary-button'} onClick={() => { setMode('customer'); setFieldErrors({}); setActionError(null) }} type="button">Customer payment</button>
          <button className={mode === 'supplier' ? 'primary-button' : 'secondary-button'} onClick={() => { setMode('supplier'); setFieldErrors({}); setActionError(null) }} type="button">Supplier payment</button>
        </div>
      </div>

      {(customers.length === 0 || suppliers.length === 0) && !isLoading ? (
        <p className="state-message">Customer payments need active customers; supplier payments need active suppliers.</p>
      ) : null}

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

      <PaymentForm
        customers={customers}
        errors={fieldErrors}
        invoices={invoices}
        isSubmitting={isSaving}
        mode={mode}
        onSubmit={handleSubmit}
        purchases={purchases}
        suppliers={suppliers}
      />

      <form className="toolbar" onSubmit={handleSearch}>
        <input aria-label="Receipt number" onChange={(event) => setReceiptNumberInput(event.target.value)} placeholder="Receipt number" type="search" value={receiptNumberInput} />
        <select aria-label="Payment method" onChange={(event) => { setPageNumber(1); setMethod(event.target.value) }} value={method}>
          <option value="">All methods</option>
          <option value="0">Cash</option>
          <option value="1">Bank transfer</option>
          <option value="2">Cheque</option>
          <option value="3">UPI</option>
          <option value="4">Other</option>
        </select>
        <button className="secondary-button" type="submit">Search</button>
      </form>

      {isLoading ? <LoadingState>Loading payments...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {!isLoading && !errorMessage && payments.length === 0 ? <EmptyState>No payments found.</EmptyState> : null}

      {!isLoading && !errorMessage && payments.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Receipt</th>
                  <th>Party</th>
                  <th>Document</th>
                  <th>Method</th>
                  <th>Amount</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {payments.map((payment) => (
                  <tr key={payment.id}>
                    <td>
                      <strong>{payment.receiptNumber}</strong>
                      <br />
                      <span>{formatDate(payment.paymentDate)}</span>
                    </td>
                    <td>{getPartyName(payment)}</td>
                    <td>
                      {payment.salesInvoiceId && payment.invoiceNumber && canViewInvoices ? (
                        <Link className="text-link" to={`/app/sales-invoices/${payment.salesInvoiceId}`}>{payment.invoiceNumber}</Link>
                      ) : (
                        getDocumentName(payment)
                      )}
                    </td>
                    <td>{getPaymentMethodLabel(payment.method as PaymentMethod)}</td>
                    <td>{formatCurrency(payment.amount)}</td>
                    <td>{getPaymentState(payment)}</td>
                    <td>
                      <div className="table-actions">
                        {!payment.reversesPaymentId && !payment.reversalPaymentId && canReversePayments ? (
                          <button className="danger-button" onClick={() => void handleReverse(payment)} type="button">Reverse</button>
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
