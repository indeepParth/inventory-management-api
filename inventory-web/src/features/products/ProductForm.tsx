import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Category, Product, ProductFormValues, Unit } from './productsApi'

type ProductFormProps = {
  categories: Category[]
  units: Unit[]
  products: Product[]
  initialValue?: Product
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: ProductFormValues) => Promise<void>
}

export function ProductForm({
  categories,
  units,
  products,
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: ProductFormProps) {
  const firstCategoryId = categories[0]?.id ?? 0
  const firstUnitId = units[0]?.id ?? 0
  const [name, setName] = useState(initialValue?.name ?? '')
  const [sku, setSku] = useState(initialValue?.sku ?? '')
  const [baseUnitId, setBaseUnitId] = useState(initialValue?.baseUnitId ?? firstUnitId)
  const [baseProductId, setBaseProductId] = useState(initialValue?.baseProductId ?? 0)
  const [factorToBaseProduct, setFactorToBaseProduct] = useState(
    initialValue?.factorToBaseProduct?.toString() ?? '',
  )
  const [defaultSellingPrice, setDefaultSellingPrice] = useState(
    initialValue?.defaultSellingPrice.toString() ?? '',
  )
  const [categoryId, setCategoryId] = useState(initialValue?.categoryId ?? firstCategoryId)
  const selectedBaseProduct = products.find((product) => product.id === baseProductId)
  const baseProductOptions = products.filter(
    (product) => !product.isSubProduct && product.id !== initialValue?.id,
  )
  const isSubProduct = Boolean(selectedBaseProduct)

  useEffect(() => {
    setName(initialValue?.name ?? '')
    setSku(initialValue?.sku ?? '')
    setBaseUnitId(initialValue?.baseUnitId ?? firstUnitId)
    setBaseProductId(initialValue?.baseProductId ?? 0)
    setFactorToBaseProduct(initialValue?.factorToBaseProduct?.toString() ?? '')
    setDefaultSellingPrice(initialValue?.defaultSellingPrice.toString() ?? '')
    setCategoryId(initialValue?.categoryId ?? firstCategoryId)
  }, [firstCategoryId, firstUnitId, initialValue])

  useEffect(() => {
    if (selectedBaseProduct && baseUnitId !== selectedBaseProduct.baseUnitId) {
      setBaseUnitId(selectedBaseProduct.baseUnitId)
    }
  }, [baseUnitId, selectedBaseProduct])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      name,
      sku,
      baseUnitId,
      baseProductId: selectedBaseProduct?.id ?? null,
      factorToBaseProduct: selectedBaseProduct ? Number(factorToBaseProduct) : null,
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
        <span>Base product</span>
        <select
          disabled={isSubmitting}
          onChange={(event) => {
            const productId = Number(event.target.value)
            const product = products.find((candidate) => candidate.id === productId)
            setBaseProductId(productId)
            if (product) {
              setBaseUnitId(product.baseUnitId)
            } else {
              setFactorToBaseProduct('')
            }
          }}
          value={baseProductId}
        >
          <option value={0}>None</option>
          {baseProductOptions.map((product) => (
            <option key={product.id} value={product.id}>
              {product.name}
            </option>
          ))}
        </select>
        {getFieldError(errors, 'BaseProductId') ? (
          <span className="field-error">{getFieldError(errors, 'BaseProductId')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Base unit</span>
        <select
          disabled={isSubmitting || isSubProduct}
          onChange={(event) => setBaseUnitId(Number(event.target.value))}
          required
          value={baseUnitId}
        >
          {units.map((unit) => (
            <option key={unit.id} value={unit.id}>
              {unit.name}
            </option>
          ))}
        </select>
        {getFieldError(errors, 'BaseUnitId') ? (
          <span className="field-error">{getFieldError(errors, 'BaseUnitId')}</span>
        ) : null}
      </label>

      {isSubProduct ? (
        <label className="form-field">
          <span>Factor to base product</span>
          <input
            disabled={isSubmitting}
            min="0.000001"
            onChange={(event) => setFactorToBaseProduct(event.target.value)}
            required
            step="0.000001"
            type="number"
            value={factorToBaseProduct}
          />
          {getFieldError(errors, 'FactorToBaseProduct') ? (
            <span className="field-error">{getFieldError(errors, 'FactorToBaseProduct')}</span>
          ) : null}
        </label>
      ) : null}

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
          disabled={isSubmitting || categories.length === 0 || units.length === 0}
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
