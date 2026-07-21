import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import { formatCurrency } from '../../shared/utils/formatters'
import type { Product } from '../products/productsApi'
import type { Supplier } from '../parties/partiesApi'
import type { Purchase, PurchaseFormValues, PurchaseItemFormValues } from './purchasesApi'

type PurchaseFormProps = {
  initialValue?: Purchase
  products: Product[]
  suppliers: Supplier[]
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: PurchaseFormValues) => Promise<void>
}

function toDateInputValue(value?: string): string {
  return value ? value.slice(0, 10) : new Date().toISOString().slice(0, 10)
}

function createBlankItem(productId: number): PurchaseItemFormValues {
  return {
    productId,
    quantity: 1,
    unitCost: 0,
    taxRate: 0,
  }
}

export function PurchaseForm({
  initialValue,
  products,
  suppliers,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: PurchaseFormProps) {
  const firstSupplierId = suppliers[0]?.id ?? 0
  const firstProductId = products[0]?.id ?? 0
  const [supplierId, setSupplierId] = useState(initialValue?.supplierId ?? firstSupplierId)
  const [supplierBillNumber, setSupplierBillNumber] = useState(initialValue?.supplierBillNumber ?? '')
  const [billDate, setBillDate] = useState(toDateInputValue(initialValue?.billDate))
  const [discount, setDiscount] = useState(initialValue?.discount.toString() ?? '0')
  const [otherCharges, setOtherCharges] = useState(initialValue?.otherCharges.toString() ?? '0')
  const [notes, setNotes] = useState(initialValue?.notes ?? '')
  const [items, setItems] = useState<PurchaseItemFormValues[]>(
    initialValue?.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
      unitCost: item.unitCost,
      taxRate: item.taxRate,
    })) ?? [createBlankItem(firstProductId)],
  )

  useEffect(() => {
    setSupplierId(initialValue?.supplierId ?? firstSupplierId)
    setSupplierBillNumber(initialValue?.supplierBillNumber ?? '')
    setBillDate(toDateInputValue(initialValue?.billDate))
    setDiscount(initialValue?.discount.toString() ?? '0')
    setOtherCharges(initialValue?.otherCharges.toString() ?? '0')
    setNotes(initialValue?.notes ?? '')
    setItems(
      initialValue?.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        unitCost: item.unitCost,
        taxRate: item.taxRate,
      })) ?? [createBlankItem(firstProductId)],
    )
  }, [firstProductId, firstSupplierId, initialValue])

  function updateItem(index: number, values: Partial<PurchaseItemFormValues>): void {
    setItems((currentItems) =>
      currentItems.map((item, itemIndex) =>
        itemIndex === index ? { ...item, ...values } : item,
      ),
    )
  }

  function removeItem(index: number): void {
    setItems((currentItems) => currentItems.filter((_, itemIndex) => itemIndex !== index))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      supplierId,
      supplierBillNumber,
      billDate,
      discount: Number(discount),
      otherCharges: Number(otherCharges),
      notes,
      items,
    })
  }

  const subtotal = items.reduce((total, item) => total + item.quantity * item.unitCost, 0)
  const taxAmount = items.reduce(
    (total, item) => total + (item.quantity * item.unitCost * item.taxRate) / 100,
    0,
  )
  const total = subtotal - Number(discount || 0) + taxAmount + Number(otherCharges || 0)

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        {initialValue ? (
          <div className="form-field">
            <span>Purchase number</span>
            <strong>{initialValue.purchaseNumber}</strong>
          </div>
        ) : null}
        <label className="form-field">
          <span>Supplier</span>
          <select disabled={isSubmitting} onChange={(event) => setSupplierId(Number(event.target.value))} required value={supplierId}>
            {suppliers.map((supplier) => <option key={supplier.id} value={supplier.id}>{supplier.name}</option>)}
          </select>
          {getFieldError(errors, 'SupplierId') ? <span className="field-error">{getFieldError(errors, 'SupplierId')}</span> : null}
        </label>
        <label className="form-field">
          <span>Supplier bill number</span>
          <input disabled={isSubmitting} onChange={(event) => setSupplierBillNumber(event.target.value)} type="text" value={supplierBillNumber} />
        </label>
        <label className="form-field">
          <span>Bill date</span>
          <input disabled={isSubmitting} onChange={(event) => setBillDate(event.target.value)} required type="date" value={billDate} />
          {getFieldError(errors, 'BillDate') ? <span className="field-error">{getFieldError(errors, 'BillDate')}</span> : null}
        </label>
        <label className="form-field">
          <span>Discount</span>
          <input disabled={isSubmitting} min="0" onChange={(event) => setDiscount(event.target.value)} step="0.01" type="number" value={discount} />
        </label>
        <label className="form-field">
          <span>Other charges</span>
          <input disabled={isSubmitting} min="0" onChange={(event) => setOtherCharges(event.target.value)} step="0.01" type="number" value={otherCharges} />
        </label>
      </div>

      <label className="form-field">
        <span>Notes</span>
        <textarea disabled={isSubmitting} onChange={(event) => setNotes(event.target.value)} rows={2} value={notes} />
      </label>

      <div className="line-items">
        <div className="line-items-header">
          <h2>Items</h2>
          <button className="secondary-button" disabled={isSubmitting || products.length === 0} onClick={() => setItems((currentItems) => [...currentItems, createBlankItem(firstProductId)])} type="button">Add item</button>
        </div>
        {getFieldError(errors, 'Items') ? <span className="field-error">{getFieldError(errors, 'Items')}</span> : null}
        {items.map((item, index) => (
          <div className="line-item-row" key={index}>
            <label className="form-field">
              <span>Product</span>
              <select disabled={isSubmitting} onChange={(event) => updateItem(index, { productId: Number(event.target.value) })} required value={item.productId}>
                {products.map((product) => <option key={product.id} value={product.id}>{product.name}</option>)}
              </select>
            </label>
            <label className="form-field">
              <span>Quantity</span>
              <input disabled={isSubmitting} min="0.001" onChange={(event) => updateItem(index, { quantity: Number(event.target.value) })} required step="0.001" type="number" value={item.quantity} />
            </label>
            <label className="form-field">
              <span>Unit cost</span>
              <input disabled={isSubmitting} min="0" onChange={(event) => updateItem(index, { unitCost: Number(event.target.value) })} required step="0.01" type="number" value={item.unitCost} />
            </label>
            <label className="form-field">
              <span>Tax %</span>
              <input disabled={isSubmitting} min="0" onChange={(event) => updateItem(index, { taxRate: Number(event.target.value) })} step="0.01" type="number" value={item.taxRate} />
            </label>
            <button className="danger-button" disabled={isSubmitting || items.length === 1} onClick={() => removeItem(index)} type="button">Remove</button>
          </div>
        ))}
      </div>

      <div className="summary-strip">
        <span>Subtotal: {formatCurrency(subtotal)}</span>
        <span>Tax: {formatCurrency(taxAmount)}</span>
        <strong>Total: {formatCurrency(total)}</strong>
      </div>

      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting || products.length === 0 || suppliers.length === 0} type="submit">{isSubmitting ? 'Saving...' : 'Save draft'}</button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button>
      </div>
    </form>
  )
}
