import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import {
  cancelCustomerReturn,
  createCustomerReturn,
  getReturnStatusLabel,
  postCustomerReturn,
  type CustomerReturn,
  type CustomerReturnFormValues,
} from '../features/returns/returnsApi'
import { getSalesInvoices, type SalesInvoice } from '../features/salesInvoices/salesInvoicesApi'
import {
  getErrorMessage,
  getFieldError,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, toDateInputValue } from '../shared/utils/formatters'

function today(): string {
  return toDateInputValue()
}

export function CustomerReturnsPage() {
  const { currentUser } = useAuth()
  const canCancelReturns = hasRouteAccess(currentUser?.roles ?? [], 'adminOrManager')
  const [invoices, setInvoices] = useState<SalesInvoice[]>([])
  const [selectedInvoiceId, setSelectedInvoiceId] = useState(0)
  const [returnNumber, setReturnNumber] = useState('')
  const [returnDate, setReturnDate] = useState(today())
  const [notes, setNotes] = useState('')
  const [quantities, setQuantities] = useState<Record<number, string>>({})
  const [currentReturn, setCurrentReturn] = useState<CustomerReturn | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadInvoices = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const response = await getSalesInvoices({
        pageNumber: 1,
        pageSize: 100,
        customerId: '',
        status: '',
        invoiceNumber: '',
      })
      const returnable = response.items.filter(
        (invoice) => invoice.status !== 0 && invoice.status !== 4,
      )
      setInvoices(returnable)
      setSelectedInvoiceId((current) => current || returnable[0]?.id || 0)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadInvoices()
  }, [loadInvoices])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    const selectedInvoice = invoices.find((invoice) => invoice.id === selectedInvoiceId)
    const values: CustomerReturnFormValues = {
      returnNumber,
      salesInvoiceId: selectedInvoiceId,
      returnDate,
      notes,
      items:
        selectedInvoice?.items
          .map((item) => ({
            salesInvoiceItemId: item.id,
            quantity: Number(quantities[item.id] || 0),
          }))
          .filter((item) => item.quantity > 0) ?? [],
    }

    try {
      setCurrentReturn(await createCustomerReturn(values))
      setQuantities({})
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handlePost(): Promise<void> {
    if (!currentReturn) {
      return
    }

    const confirmed = window.confirm(`Post customer return "${currentReturn.returnNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      setCurrentReturn(await postCustomerReturn(currentReturn.id))
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  async function handleCancel(): Promise<void> {
    if (!currentReturn) {
      return
    }

    const confirmed = window.confirm(`Cancel customer return "${currentReturn.returnNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      setCurrentReturn(await cancelCustomerReturn(currentReturn.id))
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const selectedInvoice = invoices.find((invoice) => invoice.id === selectedInvoiceId)

  return (
    <section className="content-panel wide-panel" aria-labelledby="customer-returns-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Sales returns</p>
          <h1 id="customer-returns-title" className="page-title">Customer returns</h1>
        </div>
      </div>

      {isLoading ? <LoadingState>Loading posted invoices...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}
      {!isLoading && invoices.length === 0 ? <EmptyState>Posted sales invoices are required before creating customer returns.</EmptyState> : null}

      <form className="entity-form" onSubmit={handleSubmit}>
        <div className="form-grid">
          <label className="form-field">
            <span>Return number</span>
            <input disabled={isSaving} maxLength={50} onChange={(event) => setReturnNumber(event.target.value)} required type="text" value={returnNumber} />
            {getFieldError(fieldErrors, 'ReturnNumber') ? <span className="field-error">{getFieldError(fieldErrors, 'ReturnNumber')}</span> : null}
          </label>
          <label className="form-field">
            <span>Sales invoice</span>
            <select disabled={isSaving} onChange={(event) => { setSelectedInvoiceId(Number(event.target.value)); setQuantities({}) }} required value={selectedInvoiceId}>
              {invoices.map((invoice) => <option key={invoice.id} value={invoice.id}>{invoice.invoiceNumber} - {invoice.customerName}</option>)}
            </select>
          </label>
          <label className="form-field">
            <span>Return date</span>
            <input disabled={isSaving} onChange={(event) => setReturnDate(event.target.value)} required type="date" value={returnDate} />
          </label>
        </div>

        <label className="form-field">
          <span>Notes</span>
          <textarea disabled={isSaving} maxLength={1000} onChange={(event) => setNotes(event.target.value)} rows={2} value={notes} />
        </label>

        <div className="line-items">
          <div className="line-items-header">
            <h2>Return quantities</h2>
          </div>
          {getFieldError(fieldErrors, 'Items') ? <span className="field-error">{getFieldError(fieldErrors, 'Items')}</span> : null}
          {selectedInvoice?.items.map((item) => (
            <div className="line-item-row compact-line-item-row" key={item.id}>
              <span>{item.productName} ({item.quantity})</span>
              <label className="form-field">
                <span>Quantity</span>
                <input disabled={isSaving} min="0" onChange={(event) => setQuantities((current) => ({ ...current, [item.id]: event.target.value }))} step="0.001" type="number" value={quantities[item.id] ?? ''} />
              </label>
              <span>Stock impact: + returned quantity</span>
            </div>
          ))}
        </div>

        <div className="form-actions">
          <button className="primary-button" disabled={isSaving || invoices.length === 0} type="submit">{isSaving ? 'Saving...' : 'Create draft return'}</button>
        </div>
      </form>

      {currentReturn ? (
        <div className="detail-grid">
          <span>Current return</span>
          <strong>{currentReturn.returnNumber} - {getReturnStatusLabel(currentReturn.status)}</strong>
          <span>Customer</span>
          <strong>{currentReturn.customerName}</strong>
          <span>Total credit</span>
          <strong>{formatCurrency(currentReturn.grandTotal)}</strong>
          <span>Stock impact</span>
          <strong>{currentReturn.status === 1 ? 'Posted: stock increased by returned quantities' : 'Draft: no stock movement yet'}</strong>
          <span>Actions</span>
          <div className="form-actions">
            {currentReturn.status === 0 ? <button className="text-button" onClick={() => void handlePost()} type="button">Post</button> : null}
            {currentReturn.status !== 2 && canCancelReturns ? <button className="danger-button" onClick={() => void handleCancel()} type="button">Cancel</button> : null}
          </div>
        </div>
      ) : null}
    </section>
  )
}
