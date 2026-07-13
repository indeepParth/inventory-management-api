import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Customer, Supplier } from '../parties/partiesApi'
import type { Purchase } from '../purchases/purchasesApi'
import type { SalesInvoice } from '../salesInvoices/salesInvoicesApi'
import type { PaymentFormValues, PaymentMethod } from './paymentsApi'
import { formatCurrency, toDateInputValue } from '../../shared/utils/formatters'

type PaymentMode = 'customer' | 'supplier'

type PaymentFormProps = {
  mode: PaymentMode
  customers: Customer[]
  suppliers: Supplier[]
  invoices: SalesInvoice[]
  purchases: Purchase[]
  initialCustomerId?: number
  initialSalesInvoiceId?: number
  initialAmount?: number
  lockCustomer?: boolean
  lockDocument?: boolean
  errors: FieldErrors
  isSubmitting: boolean
  onCancel?: () => void
  onSubmit: (values: PaymentFormValues) => Promise<void>
}

function today(): string {
  return toDateInputValue()
}

export function PaymentForm({
  mode,
  customers,
  suppliers,
  invoices,
  purchases,
  initialCustomerId,
  initialSalesInvoiceId,
  initialAmount,
  lockCustomer = false,
  lockDocument = false,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: PaymentFormProps) {
  const firstCustomerId = customers[0]?.id ?? 0
  const firstSupplierId = suppliers[0]?.id ?? 0
  const defaultCustomerId = mode === 'customer' ? initialCustomerId ?? firstCustomerId : firstCustomerId
  const defaultDocumentId = mode === 'customer' && initialSalesInvoiceId ? initialSalesInvoiceId.toString() : ''
  const defaultAmount = mode === 'customer' && initialAmount !== undefined ? initialAmount.toString() : '0'
  const [receiptNumber, setReceiptNumber] = useState('')
  const [customerId, setCustomerId] = useState(defaultCustomerId)
  const [supplierId, setSupplierId] = useState(firstSupplierId)
  const [documentId, setDocumentId] = useState(defaultDocumentId)
  const [paymentDate, setPaymentDate] = useState(today())
  const [amount, setAmount] = useState(defaultAmount)
  const [method, setMethod] = useState<PaymentMethod>(0)
  const [externalReference, setExternalReference] = useState('')
  const [note, setNote] = useState('')

  useEffect(() => {
    setCustomerId(defaultCustomerId)
    setSupplierId(firstSupplierId)
    setDocumentId(defaultDocumentId)
    setAmount(defaultAmount)
  }, [defaultAmount, defaultCustomerId, defaultDocumentId, firstSupplierId, mode])

  const selectedCustomer = customers.find((customer) => customer.id === customerId)

  const customerInvoices = useMemo(
    () =>
      invoices.filter(
        (invoice) =>
          invoice.customerId === customerId &&
          invoice.balanceDue > 0 &&
          (invoice.status === 1 || invoice.status === 2),
      ),
    [customerId, invoices],
  )
  const supplierPurchases = useMemo(
    () =>
      purchases.filter(
        (purchase) =>
          purchase.supplierId === supplierId &&
          purchase.balanceDue > 0 &&
          (purchase.status === 1 || purchase.status === 3),
      ),
    [purchases, supplierId],
  )

  const selectedInvoice = customerInvoices.find((invoice) => invoice.id.toString() === documentId)
  const selectedPurchase = supplierPurchases.find((purchase) => purchase.id.toString() === documentId)
  const balanceBefore =
    mode === 'customer'
      ? selectedInvoice?.balanceDue ?? selectedCustomer?.balanceDue ?? 0
      : selectedPurchase?.balanceDue ??
        supplierPurchases.reduce((total, purchase) => total + purchase.balanceDue, 0)
  const balanceAfter = balanceBefore - Number(amount || 0)

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      receiptNumber,
      customerId: mode === 'customer' ? customerId : undefined,
      salesInvoiceId: mode === 'customer' && documentId ? Number(documentId) : undefined,
      supplierId: mode === 'supplier' ? supplierId : undefined,
      purchaseId: mode === 'supplier' && documentId ? Number(documentId) : undefined,
      paymentDate,
      amount: Number(amount),
      method,
      externalReference,
      note,
    })
  }

  const isCustomerMode = mode === 'customer'

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <label className="form-field">
          <span>Receipt number</span>
          <input disabled={isSubmitting} maxLength={50} onChange={(event) => setReceiptNumber(event.target.value)} required type="text" value={receiptNumber} />
          {getFieldError(errors, 'ReceiptNumber') ? <span className="field-error">{getFieldError(errors, 'ReceiptNumber')}</span> : null}
        </label>
        <label className="form-field">
          <span>{isCustomerMode ? 'Customer' : 'Supplier'}</span>
          {isCustomerMode ? (
            <select disabled={isSubmitting || lockCustomer} onChange={(event) => { setCustomerId(Number(event.target.value)); setDocumentId('') }} required value={customerId}>
              {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
            </select>
          ) : (
            <select disabled={isSubmitting} onChange={(event) => { setSupplierId(Number(event.target.value)); setDocumentId('') }} required value={supplierId}>
              {suppliers.map((supplier) => <option key={supplier.id} value={supplier.id}>{supplier.name}</option>)}
            </select>
          )}
        </label>
        <label className="form-field">
          <span>{isCustomerMode ? 'Sales invoice' : 'Purchase'}</span>
          <select disabled={isSubmitting || lockDocument} onChange={(event) => setDocumentId(event.target.value)} value={documentId}>
            <option value="">Apply to party balance</option>
            {(isCustomerMode ? customerInvoices : supplierPurchases).map((document) => (
              <option key={document.id} value={document.id}>
                {'invoiceNumber' in document
                  ? `${document.invoiceNumber} - due ${formatCurrency(document.balanceDue)}`
                  : `${document.purchaseNumber} - due ${formatCurrency(document.balanceDue)}`}
              </option>
            ))}
          </select>
        </label>
        <label className="form-field">
          <span>Payment date</span>
          <input disabled={isSubmitting} onChange={(event) => setPaymentDate(event.target.value)} required type="date" value={paymentDate} />
        </label>
        <label className="form-field">
          <span>Amount</span>
          <input disabled={isSubmitting} min="0.01" onChange={(event) => setAmount(event.target.value)} required step="0.01" type="number" value={amount} />
          {getFieldError(errors, 'Amount') ? <span className="field-error">{getFieldError(errors, 'Amount')}</span> : null}
        </label>
        <label className="form-field">
          <span>Method</span>
          <select disabled={isSubmitting} onChange={(event) => setMethod(Number(event.target.value) as PaymentMethod)} value={method}>
            <option value="0">Cash</option>
            <option value="1">Bank transfer</option>
            <option value="2">Cheque</option>
            <option value="3">UPI</option>
            <option value="4">Other</option>
          </select>
        </label>
        <label className="form-field">
          <span>External reference</span>
          <input disabled={isSubmitting} maxLength={150} onChange={(event) => setExternalReference(event.target.value)} type="text" value={externalReference} />
        </label>
      </div>

      <label className="form-field">
        <span>Note</span>
        <textarea disabled={isSubmitting} maxLength={1000} onChange={(event) => setNote(event.target.value)} rows={2} value={note} />
      </label>

      <div className="summary-strip">
        <span>Balance before: {formatCurrency(balanceBefore)}</span>
        <strong>Balance after: {formatCurrency(balanceAfter)}</strong>
      </div>

      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting || (isCustomerMode ? customers.length === 0 : suppliers.length === 0)} type="submit">{isSubmitting ? 'Saving...' : `Save ${isCustomerMode ? 'customer' : 'supplier'} payment`}</button>
        {onCancel ? <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button> : null}
      </div>
    </form>
  )
}
