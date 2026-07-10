import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { getDeliveryChallans, type DeliveryChallan } from '../features/challans/challansApi'
import { getCustomers, type Customer } from '../features/parties/partiesApi'
import { getProducts, type Product } from '../features/products/productsApi'
import { ChallanInvoiceForm } from '../features/salesInvoices/ChallanInvoiceForm'
import { DirectInvoiceForm } from '../features/salesInvoices/DirectInvoiceForm'
import {
  cancelSalesInvoice,
  createDirectInvoice,
  createInvoiceFromChallans,
  getSalesInvoices,
  getSalesInvoiceStatusLabel,
  postSalesInvoice,
  updateDirectInvoice,
  type ChallanInvoiceFormValues,
  type DirectInvoiceFormValues,
  type PagedResponse,
  type SalesInvoice,
} from '../features/salesInvoices/salesInvoicesApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency } from '../shared/utils/formatters'

const pageSize = 10

type InvoiceFormMode = 'direct' | 'challans'

function isDirectInvoice(invoice: SalesInvoice): boolean {
  return invoice.items.every((item) => !item.deliveryChallanItemId)
}

export function SalesInvoicesPage() {
  const { currentUser } = useAuth()
  const canCancelInvoices = hasRouteAccess(currentUser?.roles ?? [], 'adminOrManager')
  const [response, setResponse] = useState<PagedResponse<SalesInvoice> | null>(null)
  const [customers, setCustomers] = useState<Customer[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [postedChallans, setPostedChallans] = useState<DeliveryChallan[]>([])
  const [editingInvoice, setEditingInvoice] = useState<SalesInvoice | undefined>()
  const [formMode, setFormMode] = useState<InvoiceFormMode | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [status, setStatus] = useState('')
  const [invoiceNumberInput, setInvoiceNumberInput] = useState('')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadInvoices = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [invoicePage, customerPage, productPage, challanPage] = await Promise.all([
        getSalesInvoices({
          pageNumber,
          pageSize,
          customerId: '',
          status,
          invoiceNumber,
        }),
        getCustomers(1, 100, '', 'true'),
        getProducts(1, 100),
        getDeliveryChallans({
          pageNumber: 1,
          pageSize: 100,
          customerId: '',
          status: '1',
          challanNumber: '',
        }),
      ])
      setResponse(invoicePage)
      setCustomers(customerPage.items)
      setProducts(productPage.items)
      setPostedChallans(challanPage.items)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [invoiceNumber, pageNumber, status])

  useEffect(() => {
    void loadInvoices()
  }, [loadInvoices])

  function closeForm(): void {
    setFormMode(null)
    setEditingInvoice(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function openDirectForm(invoice?: SalesInvoice): void {
    setFormMode('direct')
    setEditingInvoice(invoice)
    setFieldErrors({})
    setActionError(null)
  }

  function openChallanForm(): void {
    setFormMode('challans')
    setEditingInvoice(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setInvoiceNumber(invoiceNumberInput.trim())
  }

  async function handleDirectSubmit(values: DirectInvoiceFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingInvoice) {
        await updateDirectInvoice(editingInvoice.id, values)
      } else {
        await createDirectInvoice(values)
      }

      closeForm()
      await loadInvoices()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleChallanSubmit(values: ChallanInvoiceFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      await createInvoiceFromChallans(values)
      closeForm()
      await loadInvoices()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handlePost(invoice: SalesInvoice): Promise<void> {
    const confirmed = window.confirm(`Post invoice "${invoice.invoiceNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await postSalesInvoice(invoice.id)
      await loadInvoices()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  async function handleCancel(invoice: SalesInvoice): Promise<void> {
    const confirmed = window.confirm(`Cancel invoice "${invoice.invoiceNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await cancelSalesInvoice(invoice.id)
      await loadInvoices()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const invoices = response?.items ?? []
  const hasPostedChallanItems = postedChallans.some((challan) => challan.items.length > 0)

  return (
    <section className="content-panel wide-panel" aria-labelledby="sales-invoices-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Sales billing</p>
          <h1 id="sales-invoices-title" className="page-title">Sales invoices</h1>
        </div>
        <div className="form-actions">
          <button className="primary-button" disabled={customers.length === 0 || products.length === 0} onClick={() => openDirectForm()} type="button">New direct invoice</button>
          <button className="secondary-button" disabled={!hasPostedChallanItems} onClick={openChallanForm} type="button">Invoice from challans</button>
        </div>
      </div>

      <form className="toolbar" onSubmit={handleSearch}>
        <input aria-label="Invoice number" onChange={(event) => setInvoiceNumberInput(event.target.value)} placeholder="Invoice number" type="search" value={invoiceNumberInput} />
        <select aria-label="Invoice status" onChange={(event) => { setPageNumber(1); setStatus(event.target.value) }} value={status}>
          <option value="">All statuses</option>
          <option value="0">Draft</option>
          <option value="1">Posted</option>
          <option value="2">Partially paid</option>
          <option value="3">Paid</option>
          <option value="4">Cancelled</option>
        </select>
        <button className="secondary-button" type="submit">Search</button>
      </form>

      {(customers.length === 0 || products.length === 0) && !isLoading ? (
        <p className="state-message">Create at least one active customer and one product before adding direct invoices.</p>
      ) : null}

      {!hasPostedChallanItems && !isLoading ? (
        <p className="state-message">Posted delivery challans will appear as sources for challan-based invoices.</p>
      ) : null}

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

      {formMode === 'direct' ? (
        <DirectInvoiceForm
          customers={customers}
          errors={fieldErrors}
          initialValue={editingInvoice}
          isSubmitting={isSaving}
          onCancel={closeForm}
          onSubmit={handleDirectSubmit}
          products={products}
        />
      ) : null}

      {formMode === 'challans' ? (
        <ChallanInvoiceForm
          challans={postedChallans}
          errors={fieldErrors}
          isSubmitting={isSaving}
          onCancel={closeForm}
          onSubmit={handleChallanSubmit}
        />
      ) : null}

      {isLoading ? <LoadingState>Loading sales invoices...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {!isLoading && !errorMessage && invoices.length === 0 ? <EmptyState>No sales invoices found.</EmptyState> : null}

      {!isLoading && !errorMessage && invoices.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Invoice</th>
                  <th>Customer</th>
                  <th>Status</th>
                  <th>Total</th>
                  <th>Paid</th>
                  <th>Balance</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {invoices.map((invoice) => (
                  <tr key={invoice.id}>
                    <td>
                      <strong>{invoice.invoiceNumber}</strong>
                      <br />
                      <span>{invoice.items.some((item) => item.deliveryChallanItemId) ? 'From challans' : 'Direct'}</span>
                    </td>
                    <td>{invoice.customerName}</td>
                    <td>{getSalesInvoiceStatusLabel(invoice.status)}</td>
                    <td>{formatCurrency(invoice.grandTotal)}</td>
                    <td>{formatCurrency(invoice.amountPaid)}</td>
                    <td>{formatCurrency(invoice.balanceDue)}</td>
                    <td>
                      <div className="table-actions">
                        {invoice.status === 0 ? (
                          <>
                            {isDirectInvoice(invoice) ? (
                              <button className="text-button" onClick={() => openDirectForm(invoice)} type="button">Edit</button>
                            ) : null}
                            <button className="text-button" onClick={() => void handlePost(invoice)} type="button">Post</button>
                          </>
                        ) : null}
                        {invoice.status !== 4 && canCancelInvoices ? (
                          <button className="danger-button" onClick={() => void handleCancel(invoice)} type="button">Cancel</button>
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
