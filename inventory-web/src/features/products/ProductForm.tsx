import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Category, Product, ProductFormValues, UnitOfMeasure } from './productsApi'

const unitOptions: Array<{ label: string; value: UnitOfMeasure }> = [
  { label: 'Ton', value: 1 },
  { label: 'Kilogram', value: 2 },
  { label: 'Bag', value: 3 },
  { label: 'Piece', value: 4 },
  { label: 'Cubic foot', value: 5 },
  { label: 'Cubic meter', value: 6 },
]

function getUnitValue(baseUnit?: string): UnitOfMeasure {
  const unitNameToValue: Record<string, UnitOfMeasure> = {
    Ton: 1,
    Kilogram: 2,
    Bag: 3,
    Piece: 4,
    CubicFoot: 5,
    CubicMeter: 6,
  }

  return baseUnit ? unitNameToValue[baseUnit] ?? 4 : 4
}

type ProductFormProps = {
  categories: Category[]
  initialValue?: Product
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: ProductFormValues) => Promise<void>
}

export function ProductForm({
  categories,
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: ProductFormProps) {
  const firstCategoryId = categories[0]?.id ?? 0
  const [name, setName] = useState(initialValue?.name ?? '')
  const [sku, setSku] = useState(initialValue?.sku ?? '')
  const [baseUnit, setBaseUnit] = useState<UnitOfMeasure>(getUnitValue(initialValue?.baseUnit))
  const [defaultSellingPrice, setDefaultSellingPrice] = useState(
    initialValue?.defaultSellingPrice.toString() ?? '',
  )
  const [categoryId, setCategoryId] = useState(initialValue?.categoryId ?? firstCategoryId)

  useEffect(() => {
    setName(initialValue?.name ?? '')
    setSku(initialValue?.sku ?? '')
    setBaseUnit(getUnitValue(initialValue?.baseUnit))
    setDefaultSellingPrice(initialValue?.defaultSellingPrice.toString() ?? '')
    setCategoryId(initialValue?.categoryId ?? firstCategoryId)
  }, [firstCategoryId, initialValue])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      name,
      sku,
      baseUnit,
      defaultSellingPrice: Number(defaultSellingPrice),
      categoryId,
    })
  }

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <label className="form-field">
        <span>Name</span>
        <input
          disabled={isSubmitting}
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
        <span>SKU</span>
        <input
          disabled={isSubmitting}
          onChange={(event) => setSku(event.target.value)}
          required
          type="text"
          value={sku}
        />
        {getFieldError(errors, 'SKU') ? (
          <span className="field-error">{getFieldError(errors, 'SKU')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Category</span>
        <select
          disabled={isSubmitting}
          onChange={(event) => setCategoryId(Number(event.target.value))}
          required
          value={categoryId}
        >
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
        {getFieldError(errors, 'CategoryId') ? (
          <span className="field-error">{getFieldError(errors, 'CategoryId')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Base unit</span>
        <select
          disabled={isSubmitting}
          onChange={(event) => setBaseUnit(Number(event.target.value) as UnitOfMeasure)}
          required
          value={baseUnit}
        >
          {unitOptions.map((unit) => (
            <option key={unit.value} value={unit.value}>
              {unit.label}
            </option>
          ))}
        </select>
        {getFieldError(errors, 'BaseUnit') ? (
          <span className="field-error">{getFieldError(errors, 'BaseUnit')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Default selling price</span>
        <input
          disabled={isSubmitting}
          min="0.01"
          onChange={(event) => setDefaultSellingPrice(event.target.value)}
          required
          step="0.01"
          type="number"
          value={defaultSellingPrice}
        />
        {getFieldError(errors, 'DefaultSellingPrice') ? (
          <span className="field-error">{getFieldError(errors, 'DefaultSellingPrice')}</span>
        ) : null}
      </label>

      <div className="form-actions">
        <button
          className="primary-button"
          disabled={isSubmitting || categories.length === 0}
          type="submit"
        >
          {isSubmitting ? 'Saving...' : 'Save'}
        </button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">
          Cancel
        </button>
      </div>
    </form>
  )
}
