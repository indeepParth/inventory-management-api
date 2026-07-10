import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { useAuth } from '../features/auth/AuthContext'
import { getSuppliers, type Supplier } from '../features/parties/partiesApi'
import { getProducts, type Product } from '../features/products/productsApi'
import { PurchaseForm } from '../features/purchases/PurchaseForm'
import {
  cancelPurchase,
  createPurchase,
  getPurchases,
  getPurchaseStatusLabel,
  postPurchase,
  updatePurchase,
  type PagedResponse,
  type Purchase,
  type PurchaseFormValues,
} from '../features/purchases/purchasesApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'

const pageSize = 10

export function PurchasesPage() {
  const { currentUser } = useAuth()
  const canCancelPurchases = hasRouteAccess(currentUser?.roles ?? [], 'adminOrManager')
  const [response, setResponse] = useState<PagedResponse<Purchase> | null>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [editingPurchase, setEditingPurchase] = useState<Purchase | undefined>()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [status, setStatus] = useState('')
  const [purchaseNumberInput, setPurchaseNumberInput] = useState('')
  const [purchaseNumber, setPurchaseNumber] = useState('')
  const [supplierBillNumberInput, setSupplierBillNumberInput] = useState('')
  const [supplierBillNumber, setSupplierBillNumber] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadPurchases = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [purchasePage, productPage, supplierPage] = await Promise.all([
        getPurchases({
          pageNumber,
          pageSize,
          supplierId: '',
          status,
          purchaseNumber,
          supplierBillNumber,
        }),
        getProducts(1, 100),
        getSuppliers(1, 100, '', 'true'),
      ])
      setResponse(purchasePage)
      setProducts(productPage.items)
      setSuppliers(supplierPage.items)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [pageNumber, purchaseNumber, status, supplierBillNumber])

  useEffect(() => {
    void loadPurchases()
  }, [loadPurchases])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingPurchase(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setPurchaseNumber(purchaseNumberInput.trim())
    setSupplierBillNumber(supplierBillNumberInput.trim())
  }

  async function handleSubmit(values: PurchaseFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingPurchase) {
        await updatePurchase(editingPurchase.id, values)
      } else {
        await createPurchase(values)
      }

      closeForm()
      await loadPurchases()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handlePost(purchase: Purchase): Promise<void> {
    const confirmed = window.confirm(`Post purchase "${purchase.purchaseNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await postPurchase(purchase.id)
      await loadPurchases()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  async function handleCancel(purchase: Purchase): Promise<void> {
    const confirmed = window.confirm(`Cancel purchase "${purchase.purchaseNumber}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await cancelPurchase(purchase.id)
      await loadPurchases()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const purchases = response?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="purchases-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Purchasing</p>
          <h1 id="purchases-title" className="page-title">Purchases</h1>
        </div>
        <button className="primary-button" disabled={products.length === 0 || suppliers.length === 0} onClick={() => { setIsFormOpen(true); setEditingPurchase(undefined); setFieldErrors({}); setActionError(null) }} type="button">New draft purchase</button>
      </div>

      <form className="toolbar" onSubmit={handleSearch}>
        <input aria-label="Purchase number" onChange={(event) => setPurchaseNumberInput(event.target.value)} placeholder="Purchase number" type="search" value={purchaseNumberInput} />
        <input aria-label="Supplier bill number" onChange={(event) => setSupplierBillNumberInput(event.target.value)} placeholder="Supplier bill number" type="search" value={supplierBillNumberInput} />
        <select aria-label="Purchase status" onChange={(event) => { setPageNumber(1); setStatus(event.target.value) }} value={status}>
          <option value="">All statuses</option>
          <option value="0">Draft</option>
          <option value="1">Posted</option>
          <option value="2">Cancelled</option>
          <option value="3">Partially paid</option>
          <option value="4">Paid</option>
        </select>
        <button className="secondary-button" type="submit">Search</button>
      </form>

      {(products.length === 0 || suppliers.length === 0) && !isLoading ? (
        <p className="state-message">Create at least one product and one active supplier before adding purchases.</p>
      ) : null}

      {actionError ? <p className="form-error" role="alert">{actionError}</p> : null}

      {isFormOpen ? (
        <PurchaseForm
          errors={fieldErrors}
          initialValue={editingPurchase}
          isSubmitting={isSaving}
          onCancel={closeForm}
          onSubmit={handleSubmit}
          products={products}
          suppliers={suppliers}
        />
      ) : null}

      {isLoading ? <p className="state-message">Loading purchases...</p> : null}
      {errorMessage ? <p className="form-error" role="alert">{errorMessage}</p> : null}
      {!isLoading && !errorMessage && purchases.length === 0 ? <p className="state-message">No purchases found.</p> : null}

      {!isLoading && !errorMessage && purchases.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Purchase</th>
                  <th>Supplier</th>
                  <th>Status</th>
                  <th>Total</th>
                  <th>Paid</th>
                  <th>Balance</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {purchases.map((purchase) => (
                  <tr key={purchase.id}>
                    <td>
                      <strong>{purchase.purchaseNumber}</strong>
                      <br />
                      <span>{purchase.supplierBillNumber || '-'}</span>
                    </td>
                    <td>{purchase.supplierName}</td>
                    <td>{getPurchaseStatusLabel(purchase.status)}</td>
                    <td>{purchase.grandTotal.toFixed(2)}</td>
                    <td>{purchase.amountPaid.toFixed(2)}</td>
                    <td>{purchase.balanceDue.toFixed(2)}</td>
                    <td>
                      <div className="table-actions">
                        {purchase.status === 0 ? (
                          <>
                            <button className="text-button" onClick={() => { setEditingPurchase(purchase); setIsFormOpen(true); setFieldErrors({}); setActionError(null) }} type="button">Edit</button>
                            <button className="text-button" onClick={() => void handlePost(purchase)} type="button">Post</button>
                          </>
                        ) : null}
                        {purchase.status !== 2 && canCancelPurchases ? (
                          <button className="danger-button" onClick={() => void handleCancel(purchase)} type="button">Cancel</button>
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
