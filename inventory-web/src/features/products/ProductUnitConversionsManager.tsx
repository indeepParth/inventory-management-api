import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../../shared/components/Feedback'
import {
  createProductUnitConversion,
  deactivateProductUnitConversion,
  getProductUnitConversions,
  updateProductUnitConversion,
  type Product,
  type ProductUnitConversion,
  type Unit,
} from './productsApi'

type ProductUnitConversionsManagerProps = {
  product: Product
  units: Unit[]
}

export function ProductUnitConversionsManager({
  product,
  units,
}: ProductUnitConversionsManagerProps) {
  const [conversions, setConversions] = useState<ProductUnitConversion[]>([])
  const [unitId, setUnitId] = useState(0)
  const [factorToBaseUnit, setFactorToBaseUnit] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingFactor, setEditingFactor] = useState('')
  const [editingIsActive, setEditingIsActive] = useState(true)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const availableUnits = useMemo(() => {
    const usedUnitIds = new Set(conversions.map((conversion) => conversion.unitId))
    return units.filter((unit) => unit.isActive && !usedUnitIds.has(unit.id))
  }, [conversions, units])

  const loadConversions = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      setConversions(await getProductUnitConversions(product.id))
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [product.id])

  useEffect(() => {
    void loadConversions()
  }, [loadConversions])

  useEffect(() => {
    setUnitId(availableUnits[0]?.id ?? 0)
  }, [availableUnits])

  async function handleCreate(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    if (!unitId) {
      return
    }

    setIsSaving(true)
    setActionError(null)

    try {
      await createProductUnitConversion(product.id, {
        unitId,
        factorToBaseUnit: Number(factorToBaseUnit),
      })
      setFactorToBaseUnit('')
      await loadConversions()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleUpdate(conversion: ProductUnitConversion): Promise<void> {
    setIsSaving(true)
    setActionError(null)

    try {
      await updateProductUnitConversion(product.id, conversion.id, {
        factorToBaseUnit: Number(editingFactor),
        isActive: editingIsActive,
      })
      setEditingId(null)
      await loadConversions()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeactivate(conversion: ProductUnitConversion): Promise<void> {
    setIsSaving(true)
    setActionError(null)

    try {
      await deactivateProductUnitConversion(product.id, conversion.id)
      await loadConversions()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="conversion-section" aria-labelledby="product-conversions-title">
      <div className="conversion-header">
        <div>
          <p className="page-kicker">Allowed units</p>
          <h2 id="product-conversions-title" className="section-title">
            {product.name}
          </h2>
        </div>
      </div>

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}
      {isLoading ? <LoadingState>Loading unit conversions...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {!isLoading && !errorMessage ? (
        <>
          <form className="toolbar" onSubmit={handleCreate}>
            <select
              aria-label="Unit"
              disabled={isSaving || availableUnits.length === 0}
              onChange={(event) => setUnitId(Number(event.target.value))}
              value={unitId}
            >
              {availableUnits.map((unit) => (
                <option key={unit.id} value={unit.id}>
                  {unit.name}
                </option>
              ))}
            </select>
            <input
              aria-label="Factor to base unit"
              disabled={isSaving || availableUnits.length === 0}
              min="0.000001"
              onChange={(event) => setFactorToBaseUnit(event.target.value)}
              placeholder="Factor to base"
              required
              step="0.000001"
              type="number"
              value={factorToBaseUnit}
            />
            <button
              className="secondary-button"
              disabled={isSaving || availableUnits.length === 0}
              type="submit"
            >
              Add unit
            </button>
          </form>

          {conversions.length === 0 ? (
            <EmptyState>No unit conversions found.</EmptyState>
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Unit</th>
                    <th>Factor to base</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {conversions.map((conversion) => (
                    <tr key={conversion.id}>
                      <td>
                        {conversion.unitName}
                        {conversion.isBaseUnit ? ' (base)' : ''}
                      </td>
                      <td>
                        {editingId === conversion.id ? (
                          <input
                            aria-label="Edit factor to base unit"
                            disabled={isSaving || conversion.isBaseUnit}
                            min="0.000001"
                            onChange={(event) => setEditingFactor(event.target.value)}
                            step="0.000001"
                            type="number"
                            value={editingFactor}
                          />
                        ) : (
                          conversion.factorToBaseUnit
                        )}
                      </td>
                      <td>
                        {editingId === conversion.id ? (
                          <label className="checkbox-field">
                            <input
                              checked={editingIsActive}
                              disabled={isSaving || conversion.isBaseUnit}
                              onChange={(event) => setEditingIsActive(event.target.checked)}
                              type="checkbox"
                            />
                            <span>Active</span>
                          </label>
                        ) : conversion.isActive ? (
                          'Active'
                        ) : (
                          'Inactive'
                        )}
                      </td>
                      <td>
                        {conversion.isBaseUnit ? (
                          '-'
                        ) : (
                          <div className="table-actions">
                            {editingId === conversion.id ? (
                              <>
                                <button
                                  className="text-button"
                                  disabled={isSaving}
                                  onClick={() => void handleUpdate(conversion)}
                                  type="button"
                                >
                                  Save
                                </button>
                                <button
                                  className="secondary-button"
                                  disabled={isSaving}
                                  onClick={() => setEditingId(null)}
                                  type="button"
                                >
                                  Cancel
                                </button>
                              </>
                            ) : (
                              <>
                                <button
                                  className="text-button"
                                  disabled={isSaving}
                                  onClick={() => {
                                    setEditingId(conversion.id)
                                    setEditingFactor(conversion.factorToBaseUnit.toString())
                                    setEditingIsActive(conversion.isActive)
                                  }}
                                  type="button"
                                >
                                  Edit
                                </button>
                                <button
                                  className="danger-button"
                                  disabled={isSaving || !conversion.isActive}
                                  onClick={() => void handleDeactivate(conversion)}
                                  type="button"
                                >
                                  Deactivate
                                </button>
                              </>
                            )}
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      ) : null}
    </section>
  )
}
