import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { SupplierForm } from '../features/parties/SupplierForm'
import {
  createSupplier,
  deleteSupplier,
  getSuppliers,
  updateSupplier,
  type PagedResponse,
  type Supplier,
  type SupplierFormValues,
} from '../features/parties/partiesApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'

const pageSize = 10

export function SuppliersPage() {
  const { currentUser } = useAuth()
  const canManageSuppliers = hasRouteAccess(currentUser?.roles ?? [], 'manageSuppliers')
  const [response, setResponse] = useState<PagedResponse<Supplier> | null>(null)
  const [editingSupplier, setEditingSupplier] = useState<Supplier | undefined>()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [isActive, setIsActive] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadSuppliers = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      setResponse(await getSuppliers(pageNumber, pageSize, search, isActive))
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [isActive, pageNumber, search])

  useEffect(() => {
    void loadSuppliers()
  }, [loadSuppliers])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingSupplier(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setSearch(searchInput.trim())
  }

  async function handleSubmit(values: SupplierFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingSupplier) {
        await updateSupplier(editingSupplier.id, values)
      } else {
        await createSupplier({
          name: values.name,
          contactPerson: values.contactPerson,
          email: values.email,
          phone: values.phone,
          address: values.address,
          gstNumber: values.gstNumber,
        })
      }

      closeForm()
      await loadSuppliers()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(supplier: Supplier): Promise<void> {
    const confirmed = window.confirm(`Delete supplier "${supplier.name}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await deleteSupplier(supplier.id)
      await loadSuppliers()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const suppliers = response?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="suppliers-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Purchasing</p>
          <h1 id="suppliers-title" className="page-title">Suppliers</h1>
        </div>
        {canManageSuppliers ? <button className="primary-button" onClick={() => setIsFormOpen(true)} type="button">New supplier</button> : null}
      </div>

      <form className="toolbar" onSubmit={handleSearch}>
        <input aria-label="Search suppliers" onChange={(event) => setSearchInput(event.target.value)} placeholder="Search suppliers" type="search" value={searchInput} />
        <select aria-label="Status filter" onChange={(event) => { setPageNumber(1); setIsActive(event.target.value) }} value={isActive}>
          <option value="">All statuses</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
        <button className="secondary-button" type="submit">Search</button>
      </form>

      {actionError ? <p className="form-error" role="alert">{actionError}</p> : null}
      {isFormOpen ? (
        <SupplierForm errors={fieldErrors} initialValue={editingSupplier} isSubmitting={isSaving} onCancel={closeForm} onSubmit={handleSubmit} />
      ) : null}

      {isLoading ? <p className="state-message">Loading suppliers...</p> : null}
      {errorMessage ? <p className="form-error" role="alert">{errorMessage}</p> : null}
      {!isLoading && !errorMessage && suppliers.length === 0 ? <p className="state-message">No suppliers found.</p> : null}

      {!isLoading && !errorMessage && suppliers.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Contact</th>
                  <th>GST</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {suppliers.map((supplier) => (
                  <tr key={supplier.id}>
                    <td><Link className="text-link" to={`/app/suppliers/${supplier.id}`}>{supplier.name}</Link></td>
                    <td>{supplier.phone || supplier.email || '-'}</td>
                    <td>{supplier.gstNumber || '-'}</td>
                    <td>{supplier.isActive ? 'Active' : 'Inactive'}</td>
                    <td>
                      <div className="table-actions">
                        <Link className="text-link" to={`/app/suppliers/${supplier.id}`}>View</Link>
                        {canManageSuppliers ? (
                          <>
                            <button className="text-button" onClick={() => { setEditingSupplier(supplier); setIsFormOpen(true); setFieldErrors({}); setActionError(null) }} type="button">Edit</button>
                            <button className="danger-button" onClick={() => void handleDelete(supplier)} type="button">Delete</button>
                          </>
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
