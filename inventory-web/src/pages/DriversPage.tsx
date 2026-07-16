import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { DriverForm } from '../features/drivers/DriverForm'
import {
  createDriver,
  deactivateDriver,
  getDrivers,
  updateDriver,
  type Driver,
  type DriverFormValues,
  type PagedResponse,
} from '../features/drivers/driversApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'

const pageSize = 10

export function DriversPage() {
  const { currentUser } = useAuth()
  const canManageDrivers = hasRouteAccess(currentUser?.roles ?? [], 'manageDrivers')
  const [response, setResponse] = useState<PagedResponse<Driver> | null>(null)
  const [editingDriver, setEditingDriver] = useState<Driver | undefined>()
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

  const loadDrivers = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      setResponse(await getDrivers(pageNumber, pageSize, search, isActive))
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [isActive, pageNumber, search])

  useEffect(() => {
    void loadDrivers()
  }, [loadDrivers])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingDriver(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setSearch(searchInput.trim())
  }

  async function handleSubmit(values: DriverFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingDriver) {
        await updateDriver(editingDriver.id, values)
      } else {
        await createDriver({
          name: values.name,
          phone: values.phone,
          licenseNumber: values.licenseNumber,
        })
      }

      closeForm()
      await loadDrivers()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeactivate(driver: Driver): Promise<void> {
    const confirmed = window.confirm(`Deactivate driver "${driver.name}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await deactivateDriver(driver.id)
      await loadDrivers()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const drivers = response?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="drivers-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Delivery</p>
          <h1 id="drivers-title" className="page-title">Drivers</h1>
        </div>
        {canManageDrivers ? (
          <button className="primary-button" onClick={() => setIsFormOpen(true)} type="button">
            New driver
          </button>
        ) : null}
      </div>

      <form className="toolbar" onSubmit={handleSearch}>
        <input aria-label="Search drivers" onChange={(event) => setSearchInput(event.target.value)} placeholder="Search drivers" type="search" value={searchInput} />
        <select aria-label="Status filter" onChange={(event) => { setPageNumber(1); setIsActive(event.target.value) }} value={isActive}>
          <option value="">All statuses</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
        <button className="secondary-button" type="submit">Search</button>
      </form>

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}
      {isFormOpen ? (
        <DriverForm errors={fieldErrors} initialValue={editingDriver} isSubmitting={isSaving} onCancel={closeForm} onSubmit={handleSubmit} />
      ) : null}

      {isLoading ? <LoadingState>Loading drivers...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {!isLoading && !errorMessage && drivers.length === 0 ? <EmptyState>No drivers found.</EmptyState> : null}

      {!isLoading && !errorMessage && drivers.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Phone</th>
                  <th>License number</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {drivers.map((driver) => (
                  <tr key={driver.id}>
                    <td>{driver.name}</td>
                    <td>{driver.phone || '-'}</td>
                    <td>{driver.licenseNumber || '-'}</td>
                    <td>{driver.isActive ? 'Active' : 'Inactive'}</td>
                    <td>
                      <div className="table-actions">
                        <Link className="text-link" to={`/app/drivers/${driver.id}`}>View</Link>
                        {canManageDrivers ? (
                          <>
                            <button className="text-button" onClick={() => { setEditingDriver(driver); setIsFormOpen(true); setFieldErrors({}); setActionError(null) }} type="button">Edit</button>
                            {driver.isActive ? <button className="danger-button" onClick={() => void handleDeactivate(driver)} type="button">Deactivate</button> : null}
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
