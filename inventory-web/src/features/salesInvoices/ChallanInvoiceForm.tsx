import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import { formatCurrency, formatQuantity, toDateInputValue } from '../../shared/utils/formatters'
import type { DeliveryChallan } from '../challans/challansApi'
import type { ChallanInvoiceFormValues, ChallanInvoiceItemFormValues } from './salesInvoicesApi'

type ChallanInvoiceFormProps = {
  challans: DeliveryChallan[]
  errors: FieldErrors
  initialChallanId?: number
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

function createItemsForChallan(challan?: DeliveryChallan): ChallanInvoiceItemFormValues[] {
  if (!challan || challan.items.length === 0) {
    return []
  }

  return challan.items.map((item) => createBlankItem(item.id))
}

export function ChallanInvoiceForm({
  challans,
  errors,
  initialChallanId,
  isSubmitting,
  onCancel,
  onSubmit,
}: ChallanInvoiceFormProps) {
  const availableItems = useMemo(
    () =>
      challans.flatMap((challan) =>
        challan.items.map((item) => ({
          id: item.id,
          label: `${challan.challanNumber} - ${item.productName} (${formatQuantity(item.enteredQuantity)} ${item.unitName})`,
          challanId: challan.id,
          customerId: challan.customerId,
          deliveryCharge: challan.deliveryCharge,
          quantity: item.enteredQuantity,
        })),
      ),
    [challans],
  )
  const firstItemId = availableItems[0]?.id ?? 0
  const initialChallan = initialChallanId
    ? challans.find((challan) => challan.id === initialChallanId)
    : undefined
  const [invoiceDate, setInvoiceDate] = useState(toDateInputValue())
  const [discount, setDiscount] = useState('0')
  const [otherCharges, setOtherCharges] = useState('0')
  const [notes, setNotes] = useState('')
  const [items, setItems] = useState<ChallanInvoiceItemFormValues[]>([
    ...(createItemsForChallan(initialChallan).length > 0
      ? createItemsForChallan(initialChallan)
      : [createBlankItem(firstItemId)]),
  ])

  useEffect(() => {
    const initialItems = createItemsForChallan(initialChallan)
    setItems(initialItems.length > 0 ? initialItems : [createBlankItem(firstItemId)])
  }, [firstItemId, initialChallan])

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

  function getSelectedDeliveryChargeTotal(): number {
    const selectedChallans = new Map<number, number>()

    items.forEach((item) => {
      const sourceItem = availableItems.find(
        (option) => option.id === item.deliveryChallanItemId,
      )

      if (sourceItem) {
        selectedChallans.set(sourceItem.challanId, sourceItem.deliveryCharge)
      }
    })

    return Array.from(selectedChallans.values()).reduce(
      (total, deliveryCharge) => total + deliveryCharge,
      0,
    )
  }

  const selectedDeliveryChargeTotal = getSelectedDeliveryChargeTotal()
  const effectiveOtherCharges = selectedDeliveryChargeTotal > 0
    ? selectedDeliveryChargeTotal
    : Number(otherCharges || 0)

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      invoiceDate,
      discount: Number(discount),
      otherCharges: effectiveOtherCharges,
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
  const total = subtotal - Number(discount || 0) + taxAmount + effectiveOtherCharges

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <label className="form-field">
          <span>Invoice date</span>
          <input disabled={isSubmitting} onChange={(event) => setInvoiceDate(event.target.value)} required type="date" value={invoiceDate} />
        </label>
        <label className="form-field">
          <span>Discount</span>
          <input disabled={isSubmitting} min="0" onChange={(event) => setDiscount(event.target.value)} step="0.01" type="number" value={discount} />
        </label>
        {selectedDeliveryChargeTotal > 0 ? (
          <div className="form-field">
            <span>Delivery charge</span>
            <strong>{formatCurrency(selectedDeliveryChargeTotal)}</strong>
          </div>
        ) : (
          <label className="form-field">
            <span>Delivery / other charges</span>
            <input disabled={isSubmitting} min="0" onChange={(event) => setOtherCharges(event.target.value)} step="0.01" type="number" value={otherCharges} />
          </label>
        )}
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
        <span>Line prices: {formatCurrency(subtotal)}</span>
        <span>Tax: {formatCurrency(taxAmount)}</span>
        <span>Charges: {formatCurrency(effectiveOtherCharges)}</span>
        <strong>Total: {formatCurrency(total)}</strong>
      </div>

      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting || availableItems.length === 0} type="submit">{isSubmitting ? 'Saving...' : 'Save'}</button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button>
      </div>
    </form>
  )
}
