import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { DeliveryChallan } from '../challans/challansApi'
import type { ChallanInvoiceFormValues, ChallanInvoiceItemFormValues } from './salesInvoicesApi'

type ChallanInvoiceFormProps = {
  challans: DeliveryChallan[]
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: ChallanInvoiceFormValues) => Promise<void>
}

function createBlankItem(itemId: number): ChallanInvoiceItemFormValues {
  return {
    deliveryChallanItemId: itemId,
    sellingUnitPrice: 0,
    taxRate: 0,
  }
}

export function ChallanInvoiceForm({
  challans,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: ChallanInvoiceFormProps) {
  const availableItems = useMemo(
    () =>
      challans.flatMap((challan) =>
        challan.items.map((item) => ({
          id: item.id,
          label: `${challan.challanNumber} - ${item.productName} (${item.quantity})`,
          customerId: challan.customerId,
          quantity: item.quantity,
        })),
      ),
    [challans],
  )
  const firstItemId = availableItems[0]?.id ?? 0
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [invoiceDate, setInvoiceDate] = useState(new Date().toISOString().slice(0, 10))
  const [discount, setDiscount] = useState('0')
  const [otherCharges, setOtherCharges] = useState('0')
  const [notes, setNotes] = useState('')
  const [items, setItems] = useState<ChallanInvoiceItemFormValues[]>([
    createBlankItem(firstItemId),
  ])

  useEffect(() => {
    setItems([createBlankItem(firstItemId)])
  }, [firstItemId])

  function updateItem(index: number, values: Partial<ChallanInvoiceItemFormValues>): void {
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
      invoiceDate,
      discount: Number(discount),
      otherCharges: Number(otherCharges),
      notes,
      items,
    })
  }

  const subtotal = items.reduce((total, item) => {
    const sourceItem = availableItems.find(
      (option) => option.id === item.deliveryChallanItemId,
    )

    return total + item.sellingUnitPrice * (sourceItem?.quantity ?? 0)
  }, 0)
  const taxAmount = items.reduce(
    (total, item) => {
      const sourceItem = availableItems.find(
        (option) => option.id === item.deliveryChallanItemId,
      )

      return total + ((item.sellingUnitPrice * (sourceItem?.quantity ?? 0)) * item.taxRate) / 100
    },
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
          <h2>Posted challan items</h2>
          <button className="secondary-button" disabled={isSubmitting || availableItems.length === 0} onClick={() => setItems((currentItems) => [...currentItems, createBlankItem(firstItemId)])} type="button">Add challan item</button>
        </div>
        {getFieldError(errors, 'Items') ? <span className="field-error">{getFieldError(errors, 'Items')}</span> : null}
        {items.map((item, index) => (
          <div className="line-item-row invoice-challan-line-item-row" key={index}>
            <label className="form-field">
              <span>Challan item</span>
              <select disabled={isSubmitting} onChange={(event) => updateItem(index, { deliveryChallanItemId: Number(event.target.value) })} required value={item.deliveryChallanItemId}>
                {availableItems.map((option) => <option key={option.id} value={option.id}>{option.label}</option>)}
              </select>
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
        <span>Line prices: {subtotal.toFixed(2)}</span>
        <span>Tax: {taxAmount.toFixed(2)}</span>
        <strong>Total: {total.toFixed(2)}</strong>
      </div>

      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting || availableItems.length === 0} type="submit">{isSubmitting ? 'Saving...' : 'Save challan draft'}</button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button>
      </div>
    </form>
  )
}
