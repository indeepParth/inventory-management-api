import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Unit, UnitFormValues } from './productsApi'

type UnitFormProps = {
  units: Unit[]
  initialValue?: Unit
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: UnitFormValues) => Promise<void>
}

export function UnitForm({
  units,
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: UnitFormProps) {
  const [name, setName] = useState(initialValue?.name ?? '')
  const [shortName, setShortName] = useState(initialValue?.shortName ?? '')
  const [factorToBaseUnit, setFactorToBaseUnit] = useState(
    (initialValue?.factorToBaseUnit ?? 1).toString(),
  )
  const [baseUnitId, setBaseUnitId] = useState(initialValue?.baseUnitId ?? 0)
  const [isActive, setIsActive] = useState(initialValue?.isActive ?? true)
  const baseUnits = units.filter(
    (unit) => unit.isActive && unit.baseUnitId === unit.id,
  )
  const isBaseUnit = baseUnitId === 0 || baseUnitId === initialValue?.id

  useEffect(() => {
    setName(initialValue?.name ?? '')
    setShortName(initialValue?.shortName ?? '')
    setFactorToBaseUnit((initialValue?.factorToBaseUnit ?? 1).toString())
    setBaseUnitId(initialValue?.baseUnitId ?? 0)
    setIsActive(initialValue?.isActive ?? true)
  }, [initialValue])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      name,
      shortName,
      factorToBaseUnit: isBaseUnit ? 1 : Number(factorToBaseUnit),
      baseUnitId: baseUnitId > 0 ? baseUnitId : null,
      isActive,
    })
  }

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <label className="form-field">
        <span>Name</span>
        <input
          disabled={isSubmitting}
          maxLength={100}
          onChange={(event) => setName(event.target.value)}
          required
          type="text"
          value={name}
        />
        {getFieldError(errors, 'Name') ? (
          <span className="field-error">{getFieldError(errors, 'Name')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Short name</span>
        <input
          disabled={isSubmitting}
          maxLength={20}
          onChange={(event) => setShortName(event.target.value)}
          type="text"
          value={shortName}
        />
        {getFieldError(errors, 'ShortName') ? (
          <span className="field-error">{getFieldError(errors, 'ShortName')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Base unit</span>
        <select
          disabled={isSubmitting}
          onChange={(event) => setBaseUnitId(Number(event.target.value))}
          value={isBaseUnit ? 0 : baseUnitId}
        >
          <option value={0}>This unit is a base unit</option>
          {baseUnits
            .filter((unit) => unit.id !== initialValue?.id)
            .map((unit) => (
              <option key={unit.id} value={unit.id}>
                {unit.name}
              </option>
            ))}
        </select>
        {getFieldError(errors, 'BaseUnitId') ? (
          <span className="field-error">{getFieldError(errors, 'BaseUnitId')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Factor to base</span>
        <input
          disabled={isSubmitting || isBaseUnit}
          min="0.000001"
          onChange={(event) => setFactorToBaseUnit(event.target.value)}
          required
          step="0.000001"
          type="number"
          value={isBaseUnit ? '1' : factorToBaseUnit}
        />
        {getFieldError(errors, 'FactorToBaseUnit') ? (
          <span className="field-error">{getFieldError(errors, 'FactorToBaseUnit')}</span>
        ) : null}
      </label>

      {initialValue ? (
        <label className="checkbox-field">
          <input
            checked={isActive}
            disabled={isSubmitting}
            onChange={(event) => setIsActive(event.target.checked)}
            type="checkbox"
          />
          <span>Active</span>
        </label>
      ) : null}

      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting} type="submit">
          {isSubmitting ? 'Saving...' : 'Save'}
        </button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">
          Cancel
        </button>
      </div>
    </form>
  )
}
