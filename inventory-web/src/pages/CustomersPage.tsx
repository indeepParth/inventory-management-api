import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { CustomerForm } from '../features/parties/CustomerForm'
import {
  createCustomer,
  deactivateCustomer,
  getCustomers,
  updateCustomer,
  type Customer,
  type CustomerFormValues,
  type PagedResponse,
} from '../features/parties/partiesApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency } from '../shared/utils/formatters'

const pageSize = 10

export function CustomersPage() {
  const { currentUser } = useAuth()
  const canManageCustomers = hasRouteAccess(currentUser?.roles ?? [], 'manageCustomers')
  const [response, setResponse] = useState<PagedResponse<Customer> | null>(null)
  const [editingCustomer, setEditingCustomer] = useState<Customer | undefined>()
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

  const loadCustomers = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      setResponse(await getCustomers(pageNumber, pageSize, search, isActive))
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [isActive, pageNumber, search])

  useEffect(() => {
    void loadCustomers()
  }, [loadCustomers])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingCustomer(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setSearch(searchInput.trim())
  }

  async function handleSubmit(values: CustomerFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingCustomer) {
        await updateCustomer(editingCustomer.id, values)
      } else {
        await createCustomer({
          name: values.name,
          contactPerson: values.contactPerson,
          phone: values.phone,
          email: values.email,
          billingAddress: values.billingAddress,
          deliveryAddress: values.deliveryAddress,
          gstNumber: values.gstNumber,
          creditLimit: values.creditLimit,
        })
      }

      closeForm()
      await loadCustomers()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeactivate(customer: Customer): Promise<void> {
    const confirmed = window.confirm(`Deactivate customer "${customer.name}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await deactivateCustomer(customer.id)
      await loadCustomers()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const customers = response?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="customers-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Sales</p>
          <h1 id="customers-title" className="page-title">Customers</h1>
        </div>
        {canManageCustomers ? (
          <button className="primary-button" onClick={() => setIsFormOpen(true)} type="button">
            New customer
          </button>
        ) : null}
      </div>

      <form className="toolbar" onSubmit={handleSearch}>
        <input aria-label="Search customers" onChange={(event) => setSearchInput(event.target.value)} placeholder="Search customers" type="search" value={searchInput} />
        <select aria-label="Status filter" onChange={(event) => { setPageNumber(1); setIsActive(event.target.value) }} value={isActive}>
          <option value="">All statuses</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
        <button className="secondary-button" type="submit">Search</button>
      </form>

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

      {isFormOpen ? (
        <CustomerForm errors={fieldErrors} initialValue={editingCustomer} isSubmitting={isSaving} onCancel={closeForm} onSubmit={handleSubmit} />
      ) : null}

      {isLoading ? <LoadingState>Loading customers...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {!isLoading && !errorMessage && customers.length === 0 ? <EmptyState>No customers found.</EmptyState> : null}

      {!isLoading && !errorMessage && customers.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Contact</th>
                  <th>Balance</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {customers.map((customer) => (
                  <tr key={customer.id}>
                    <td><Link className="text-link" to={`/app/customers/${customer.id}`}>{customer.name}</Link></td>
                    <td>{customer.phone || customer.email || '-'}</td>
                    <td>{formatCurrency(customer.balanceDue)}</td>
                    <td>{customer.isActive ? 'Active' : 'Inactive'}</td>
                    <td>
                      <div className="table-actions">
                        <Link className="text-link" to={`/app/customers/${customer.id}`}>View</Link>
                        {canManageCustomers ? (
                          <>
                            <button className="text-button" onClick={() => { setEditingCustomer(customer); setIsFormOpen(true); setFieldErrors({}); setActionError(null) }} type="button">Edit</button>
                            {customer.isActive ? <button className="danger-button" onClick={() => void handleDeactivate(customer)} type="button">Deactivate</button> : null}
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
