import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../features/auth/AuthContext'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { UnitForm } from '../features/products/UnitForm'
import {
  createUnit,
  deleteUnit,
  getUnits,
  updateUnit,
  type Unit,
  type UnitFormValues,
} from '../features/products/productsApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'

export function UnitsPage() {
  const { currentUser } = useAuth()
  const canManageProducts = hasRouteAccess(currentUser?.roles ?? [], 'manageProducts')
  const [units, setUnits] = useState<Unit[]>([])
  const [editingUnit, setEditingUnit] = useState<Unit | undefined>()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadUnits = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      setUnits(await getUnits())
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadUnits()
  }, [loadUnits])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingUnit(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  async function handleSubmit(values: UnitFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingUnit) {
        await updateUnit(editingUnit.id, values)
      } else {
        await createUnit({
          name: values.name,
          shortName: values.shortName,
          factorToBaseUnit: values.factorToBaseUnit,
          baseUnitId: values.baseUnitId,
        })
      }

      closeForm()
      await loadUnits()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(unit: Unit): Promise<void> {
    const confirmed = window.confirm(`Delete unit "${unit.name}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await deleteUnit(unit.id)
      await loadUnits()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  return (
    <section className="content-panel wide-panel" aria-labelledby="units-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Master data</p>
          <h1 id="units-title" className="page-title">
            Units
          </h1>
        </div>
        {canManageProducts ? (
          <button
            className="primary-button"
            onClick={() => {
              setIsFormOpen(true)
              setEditingUnit(undefined)
              setFieldErrors({})
              setActionError(null)
            }}
            type="button"
          >
            New unit
          </button>
        ) : null}
      </div>

      {actionError ? (
        <ErrorBanner>{actionError}</ErrorBanner>
      ) : null}

      {isFormOpen ? (
        <UnitForm
          units={units}
          errors={fieldErrors}
          initialValue={editingUnit}
          isSubmitting={isSaving}
          onCancel={closeForm}
          onSubmit={handleSubmit}
        />
      ) : null}

      {isLoading ? <LoadingState>Loading units...</LoadingState> : null}
      {errorMessage ? (
        <ErrorBanner>{errorMessage}</ErrorBanner>
      ) : null}
      {!isLoading && !errorMessage && units.length === 0 ? (
        <EmptyState>No units found.</EmptyState>
      ) : null}

      {!isLoading && !errorMessage && units.length > 0 ? (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Short name</th>
                <th>Factor to base</th>
                <th>Base unit</th>
                <th>Status</th>
                {canManageProducts ? <th>Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {units.map((unit) => (
                <tr key={unit.id}>
                  <td>{unit.name}</td>
                  <td>{unit.shortName || '-'}</td>
                  <td>{unit.factorToBaseUnit}</td>
                  <td>{unit.baseUnitName || unit.name}</td>
                  <td>{unit.isActive ? 'Active' : 'Inactive'}</td>
                  {canManageProducts ? (
                    <td>
                      <div className="table-actions">
                        <button
                          className="text-button"
                          onClick={() => {
                            setEditingUnit(unit)
                            setIsFormOpen(true)
                            setFieldErrors({})
                            setActionError(null)
                          }}
                          type="button"
                        >
                          Edit
                        </button>
                        <button
                          className="danger-button"
                          onClick={() => void handleDelete(unit)}
                          type="button"
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  ) : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  )
}
