import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import { formatCurrency } from '../../shared/utils/formatters'
import type { Driver } from '../drivers/driversApi'
import type { Customer } from '../parties/partiesApi'
import type { Product } from '../products/productsApi'
import type {
  DirectInvoiceFormValues,
  DirectInvoiceItemFormValues,
  SalesInvoice,
} from './salesInvoicesApi'

type DirectInvoiceFormProps = {
  customers: Customer[]
  drivers: Driver[]
  products: Product[]
  initialValue?: SalesInvoice
  initialCustomerId?: number
  initialDeliveryAddress?: string
  lockCustomer?: boolean
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

function getLineSubtotal(item: DirectInvoiceItemFormValues): number {
  return item.quantity * item.sellingUnitPrice
}

export function DirectInvoiceForm({
  customers,
  drivers,
  products,
  initialValue,
  initialCustomerId,
  initialDeliveryAddress,
  lockCustomer = false,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: DirectInvoiceFormProps) {
  const firstCustomerId = customers[0]?.id ?? 0
  const defaultCustomerId = initialCustomerId ?? firstCustomerId
  const defaultDeliveryAddress = initialValue?.deliveryAddress ?? initialDeliveryAddress ?? ''
  const firstProductId = products[0]?.id ?? 0
  const [invoiceNumber, setInvoiceNumber] = useState(initialValue?.invoiceNumber ?? '')
  const [customerId, setCustomerId] = useState(initialValue?.customerId ?? defaultCustomerId)
  const [driverId, setDriverId] = useState(initialValue?.driverId ?? 0)
  const [invoiceDate, setInvoiceDate] = useState(toDateInputValue(initialValue?.invoiceDate))
  const [driverCharge, setDriverCharge] = useState(initialValue?.otherCharges.toString() ?? '0')
  const [laborCharge, setLaborCharge] = useState(initialValue?.laborCharge.toString() ?? '0')
  const [deliveryAddress, setDeliveryAddress] = useState(defaultDeliveryAddress)
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
    setCustomerId(initialValue?.customerId ?? defaultCustomerId)
    setDriverId(initialValue?.driverId ?? 0)
    setInvoiceDate(toDateInputValue(initialValue?.invoiceDate))
    setDriverCharge(initialValue?.otherCharges.toString() ?? '0')
    setLaborCharge(initialValue?.laborCharge.toString() ?? '0')
    setDeliveryAddress(defaultDeliveryAddress)
    setNotes(initialValue?.notes ?? '')
    setItems(
      initialValue?.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        sellingUnitPrice: item.sellingUnitPrice,
        taxRate: item.taxRate,
      })) ?? [createBlankItem(firstProductId)],
    )
  }, [defaultCustomerId, defaultDeliveryAddress, firstProductId, initialValue])

  function updateItem(index: number, values: Partial<DirectInvoiceItemFormValues>): void {
    setItems((currentItems) =>
      currentItems.map((item, itemIndex) =>
        itemIndex === index ? { ...item, ...values } : item,
      ),
    )
  }

  function updateItemSubtotal(index: number, subtotal: number): void {
    setItems((currentItems) =>
      currentItems.map((item, itemIndex) =>
        itemIndex === index
          ? {
              ...item,
              sellingUnitPrice: item.quantity > 0 ? subtotal / item.quantity : 0,
            }
          : item,
      ),
    )
  }

  function removeItem(index: number): void {
    setItems((currentItems) => currentItems.filter((_, itemIndex) => itemIndex !== index))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      invoiceNumber: invoiceNumber.trim(),
      customerId,
      driverId: driverId > 0 ? driverId : null,
      invoiceDate,
      discount: 0,
      otherCharges: driverId > 0 ? Number(driverCharge) : 0,
      laborCharge: driverId > 0 ? Number(laborCharge) : 0,
      deliveryAddress,
      notes,
      items,
    })
  }

  const subtotal = items.reduce(
    (total, item) => total + getLineSubtotal(item),
    0,
  )
  const taxAmount = items.reduce(
    (total, item) => total + (item.quantity * item.sellingUnitPrice * item.taxRate) / 100,
    0,
  )
  const total = subtotal + taxAmount + (
    driverId > 0 ? Number(driverCharge || 0) + Number(laborCharge || 0) : 0
  )

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
          <select
            disabled={isSubmitting || lockCustomer}
            onChange={(event) => {
              const nextCustomerId = Number(event.target.value)
              setCustomerId(nextCustomerId)
              const customer = customers.find((candidate) => candidate.id === nextCustomerId)
              setDeliveryAddress(customer?.deliveryAddress ?? '')
            }}
            required
            value={customerId}
          >
            {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
          </select>
          {getFieldError(errors, 'CustomerId') ? <span className="field-error">{getFieldError(errors, 'CustomerId')}</span> : null}
        </label>
        <label className="form-field">
          <span>Invoice date</span>
          <input disabled={isSubmitting} onChange={(event) => setInvoiceDate(event.target.value)} required type="date" value={invoiceDate} />
        </label>
        <label className="form-field">
          <span>Driver</span>
          <select
            disabled={isSubmitting}
            onChange={(event) => {
              const nextDriverId = Number(event.target.value)
              setDriverId(nextDriverId)
              if (nextDriverId > 0) {
                const customer = customers.find((candidate) => candidate.id === customerId)
                setDeliveryAddress(deliveryAddress || customer?.deliveryAddress || '')
              } else {
                setDriverCharge('0')
                setLaborCharge('0')
              }
            }}
            value={driverId}
          >
            <option value={0}>No driver selected</option>
            {drivers.map((driver) => <option key={driver.id} value={driver.id}>{driver.name}</option>)}
          </select>
          {getFieldError(errors, 'DriverId') ? <span className="field-error">{getFieldError(errors, 'DriverId')}</span> : null}
        </label>
        {driverId > 0 ? (
          <>
            <label className="form-field">
              <span>Driver charge</span>
              <input disabled={isSubmitting} min="0" onChange={(event) => setDriverCharge(event.target.value)} required step="0.01" type="number" value={driverCharge} />
              {getFieldError(errors, 'OtherCharges') ? <span className="field-error">{getFieldError(errors, 'OtherCharges')}</span> : null}
            </label>
            <label className="form-field">
              <span>Laber charge</span>
              <input disabled={isSubmitting} min="0" onChange={(event) => setLaborCharge(event.target.value)} required step="0.01" type="number" value={laborCharge} />
              {getFieldError(errors, 'LaborCharge') ? <span className="field-error">{getFieldError(errors, 'LaborCharge')}</span> : null}
            </label>
          </>
        ) : null}
      </div>

      <div className="form-grid">
        <label className="form-field">
          <span>Delivery address</span>
          <textarea disabled={isSubmitting} maxLength={500} onChange={(event) => setDeliveryAddress(event.target.value)} rows={2} value={deliveryAddress} />
          {getFieldError(errors, 'DeliveryAddress') ? <span className="field-error">{getFieldError(errors, 'DeliveryAddress')}</span> : null}
        </label>

        <label className="form-field">
          <span>Notes</span>
          <textarea disabled={isSubmitting} onChange={(event) => setNotes(event.target.value)} rows={2} value={notes} />
        </label>
      </div>

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
                {products.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.isSubProduct && product.baseProductName
                      ? `${product.name} (${product.baseProductName})`
                      : product.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span>Quantity</span>
              <input disabled={isSubmitting} min="0.001" onChange={(event) => updateItem(index, { quantity: Number(event.target.value) })} required step="0.001" type="number" value={item.quantity} />
            </label>
            <label className="form-field">
              <span>Unit price</span>
              <input disabled={isSubmitting} min="0" onChange={(event) => updateItem(index, { sellingUnitPrice: Number(event.target.value) })} required step="0.01" type="number" value={item.sellingUnitPrice} />
            </label>
            <label className="form-field">
              <span>Subtotal</span>
              <input disabled={isSubmitting} min="0" onChange={(event) => updateItemSubtotal(index, Number(event.target.value))} required step="0.01" type="number" value={getLineSubtotal(item)} />
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
        <button className="primary-button" disabled={isSubmitting || customers.length === 0 || products.length === 0} type="submit">{isSubmitting ? 'Saving...' : 'Save'}</button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button>
      </div>
    </form>
  )
}
