import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import { formatCurrency } from '../../shared/utils/formatters'
import type { Customer } from '../parties/partiesApi'
import type { Product } from '../products/productsApi'
import type {
  DirectInvoiceFormValues,
  DirectInvoiceItemFormValues,
  SalesInvoice,
} from './salesInvoicesApi'

type DirectInvoiceFormProps = {
  customers: Customer[]
  products: Product[]
  initialValue?: SalesInvoice
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: DirectInvoiceFormValues) => Promise<void>
}

function toDateInputValue(value?: string): string {
  return value ? value.slice(0, 10) : new Date().toISOString().slice(0, 10)
}

function createBlankItem(productId: number): DirectInvoiceItemFormValues {
  return {
    productId,
    quantity: 1,
    sellingUnitPrice: 0,
    taxRate: 0,
  }
}

export function DirectInvoiceForm({
  customers,
  products,
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: DirectInvoiceFormProps) {
  const firstCustomerId = customers[0]?.id ?? 0
  const firstProductId = products[0]?.id ?? 0
  const [invoiceNumber, setInvoiceNumber] = useState(initialValue?.invoiceNumber ?? '')
  const [customerId, setCustomerId] = useState(initialValue?.customerId ?? firstCustomerId)
  const [invoiceDate, setInvoiceDate] = useState(toDateInputValue(initialValue?.invoiceDate))
  const [discount, setDiscount] = useState(initialValue?.discount.toString() ?? '0')
  const [otherCharges, setOtherCharges] = useState(initialValue?.otherCharges.toString() ?? '0')
  const [notes, setNotes] = useState(initialValue?.notes ?? '')
  const [items, setItems] = useState<DirectInvoiceItemFormValues[]>(
    initialValue?.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
      sellingUnitPrice: item.sellingUnitPrice,
      taxRate: item.taxRate,
    })) ?? [createBlankItem(firstProductId)],
  )

  useEffect(() => {
    setInvoiceNumber(initialValue?.invoiceNumber ?? '')
    setCustomerId(initialValue?.customerId ?? firstCustomerId)
    setInvoiceDate(toDateInputValue(initialValue?.invoiceDate))
    setDiscount(initialValue?.discount.toString() ?? '0')
    setOtherCharges(initialValue?.otherCharges.toString() ?? '0')
    setNotes(initialValue?.notes ?? '')
    setItems(
      initialValue?.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        sellingUnitPrice: item.sellingUnitPrice,
        taxRate: item.taxRate,
      })) ?? [createBlankItem(firstProductId)],
    )
  }, [firstCustomerId, firstProductId, initialValue])

  function updateItem(index: number, values: Partial<DirectInvoiceItemFormValues>): void {
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
      invoiceNumber,
      customerId,
      invoiceDate,
      discount: Number(discount),
      otherCharges: Number(otherCharges),
      notes,
      items,
    })
  }

  const subtotal = items.reduce(
    (total, item) => total + item.quantity * item.sellingUnitPrice,
    0,
  )
  const taxAmount = items.reduce(
    (total, item) => total + (item.quantity * item.sellingUnitPrice * item.taxRate) / 100,
    0,
  )
  const total = subtotal - Number(discount || 0) + taxAmount + Number(otherCharges || 0)

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <label className="form-field">
          <span>Invoice number</span>
          <input disabled={isSubmitting} maxLength={50} onChange={(event) => setInvoiceNumber(event.target.value)} required type="text" value={invoiceNumber} />
          {getFieldError(errors, 'InvoiceNumber') ? <span className="field-error">{getFieldError(errors, 'InvoiceNumber')}</span> : null}
        </label>
        <label className="form-field">
          <span>Customer</span>
          <select disabled={isSubmitting} onChange={(event) => setCustomerId(Number(event.target.value))} required value={customerId}>
            {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
          </select>
          {getFieldError(errors, 'CustomerId') ? <span className="field-error">{getFieldError(errors, 'CustomerId')}</span> : null}
        </label>
        <label className="form-field">
          <span>Invoice date</span>
          <input disabled={isSubmitting} onChange={(event) => setInvoiceDate(event.target.value)} required type="date" value={invoiceDate} />
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
          <h2>Direct invoice items</h2>
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
              <span>Selling price</span>
              <input disabled={isSubmitting} min="0" onChange={(event) => updateItem(index, { sellingUnitPrice: Number(event.target.value) })} required step="0.01" type="number" value={item.sellingUnitPrice} />
            </label>
            <label className="form-field">
              <span>Tax %</span>
              <input disabled={isSubmitting} min="0" max="100" onChange={(event) => updateItem(index, { taxRate: Number(event.target.value) })} step="0.01" type="number" value={item.taxRate} />
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
        <button className="primary-button" disabled={isSubmitting || customers.length === 0 || products.length === 0} type="submit">{isSubmitting ? 'Saving...' : 'Save direct draft'}</button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button>
      </div>
    </form>
  )
}
