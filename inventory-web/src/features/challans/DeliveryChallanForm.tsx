import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Driver } from '../drivers/driversApi'
import type { Customer } from '../parties/partiesApi'
import type { Product } from '../products/productsApi'
import type {
  DeliveryChallan,
  DeliveryChallanFormValues,
  DeliveryChallanItemFormValues,
} from './challansApi'

type DeliveryChallanFormProps = {
  customers: Customer[]
  drivers: Driver[]
  products: Product[]
  initialValue?: DeliveryChallan
  initialCustomerId?: number
  initialDeliveryAddress?: string
  lockCustomer?: boolean
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: DeliveryChallanFormValues) => Promise<void>
}

function toDateInputValue(value?: string): string {
  if (!value) {
    return new Date().toISOString().slice(0, 10)
  }

  return value.slice(0, 10)
}

function createBlankItem(productId: number): DeliveryChallanItemFormValues {
  return {
    productId,
    quantity: 1,
  }
}

export function DeliveryChallanForm({
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
}: DeliveryChallanFormProps) {
  const firstCustomerId = customers[0]?.id ?? 0
  const defaultCustomerId = initialValue?.customerId ?? initialCustomerId ?? firstCustomerId
  const defaultDeliveryAddress = initialValue?.deliveryAddress ?? initialDeliveryAddress ?? ''
  const firstProductId = products[0]?.id ?? 0
  const [customerId, setCustomerId] = useState(defaultCustomerId)
  const [challanDate, setChallanDate] = useState(toDateInputValue(initialValue?.challanDate))
  const [vehicleNumber, setVehicleNumber] = useState(initialValue?.vehicleNumber ?? '')
  const [driverId, setDriverId] = useState(initialValue?.driverId ?? 0)
  const [deliveryFromAddress, setDeliveryFromAddress] = useState(initialValue?.deliveryFromAddress ?? '')
  const [deliveryAddress, setDeliveryAddress] = useState(defaultDeliveryAddress)
  const [deliveryCharge, setDeliveryCharge] = useState((initialValue?.deliveryCharge ?? 0).toString())
  const [notes, setNotes] = useState(initialValue?.notes ?? '')
  const [items, setItems] = useState<DeliveryChallanItemFormValues[]>(
    initialValue?.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
    })) ?? [createBlankItem(firstProductId)],
  )

  useEffect(() => {
    setCustomerId(defaultCustomerId)
    setChallanDate(toDateInputValue(initialValue?.challanDate))
    setVehicleNumber(initialValue?.vehicleNumber ?? '')
    setDriverId(initialValue?.driverId ?? 0)
    setDeliveryFromAddress(initialValue?.deliveryFromAddress ?? '')
    setDeliveryAddress(defaultDeliveryAddress)
    setDeliveryCharge((initialValue?.deliveryCharge ?? 0).toString())
    setNotes(initialValue?.notes ?? '')
    setItems(
      initialValue?.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
      })) ?? [createBlankItem(firstProductId)],
    )
  }, [defaultCustomerId, defaultDeliveryAddress, firstProductId, initialValue])

  function updateItem(index: number, values: Partial<DeliveryChallanItemFormValues>): void {
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
      customerId,
      challanDate,
      vehicleNumber,
      driverId: driverId > 0 ? driverId : null,
      deliveryFromAddress,
      deliveryAddress,
      deliveryCharge: Number(deliveryCharge),
      notes,
      items,
    })
  }

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        {initialValue ? (
          <div className="form-field">
            <span>Challan number</span>
            <strong>{initialValue.challanNumber}</strong>
          </div>
        ) : null}
        <label className="form-field">
          <span>Customer</span>
          <select disabled={isSubmitting || lockCustomer} onChange={(event) => setCustomerId(Number(event.target.value))} required value={customerId}>
            {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
          </select>
          {getFieldError(errors, 'CustomerId') ? <span className="field-error">{getFieldError(errors, 'CustomerId')}</span> : null}
        </label>
        <label className="form-field">
          <span>Challan date</span>
          <input disabled={isSubmitting} onChange={(event) => setChallanDate(event.target.value)} required type="date" value={challanDate} />
          {getFieldError(errors, 'ChallanDate') ? <span className="field-error">{getFieldError(errors, 'ChallanDate')}</span> : null}
        </label>
        <label className="form-field">
          <span>Vehicle number</span>
          <input disabled={isSubmitting} maxLength={50} onChange={(event) => setVehicleNumber(event.target.value)} type="text" value={vehicleNumber} />
        </label>
        <label className="form-field">
          <span>Driver</span>
          <select disabled={isSubmitting} onChange={(event) => setDriverId(Number(event.target.value))} value={driverId}>
            <option value={0}>No driver selected</option>
            {drivers.map((driver) => <option key={driver.id} value={driver.id}>{driver.name}</option>)}
          </select>
          {getFieldError(errors, 'DriverId') ? <span className="field-error">{getFieldError(errors, 'DriverId')}</span> : null}
        </label>
      </div>

      <label className="form-field">
        <span>Delivery from address</span>
        <textarea disabled={isSubmitting} maxLength={500} onChange={(event) => setDeliveryFromAddress(event.target.value)} required rows={3} value={deliveryFromAddress} />
        {getFieldError(errors, 'DeliveryFromAddress') ? <span className="field-error">{getFieldError(errors, 'DeliveryFromAddress')}</span> : null}
      </label>
      <label className="form-field">
        <span>Delivery to address</span>
        <textarea disabled={isSubmitting} maxLength={500} onChange={(event) => setDeliveryAddress(event.target.value)} required rows={3} value={deliveryAddress} />
        {getFieldError(errors, 'DeliveryAddress') ? <span className="field-error">{getFieldError(errors, 'DeliveryAddress')}</span> : null}
      </label>
      <label className="form-field">
        <span>Delivery charge</span>
        <input disabled={isSubmitting} min="0" onChange={(event) => setDeliveryCharge(event.target.value)} required step="0.01" type="number" value={deliveryCharge} />
        {getFieldError(errors, 'DeliveryCharge') ? <span className="field-error">{getFieldError(errors, 'DeliveryCharge')}</span> : null}
      </label>
      {initialValue && initialValue.deliveryCharge > 0 ? (
        <p className="state-message">
          Delivery charge status: {initialValue.isDeliveryChargePaid ? 'Paid' : 'Unpaid'}
        </p>
      ) : null}
      <label className="form-field">
        <span>Notes</span>
        <textarea disabled={isSubmitting} maxLength={1000} onChange={(event) => setNotes(event.target.value)} rows={2} value={notes} />
      </label>

      <div className="line-items">
        <div className="line-items-header">
          <h2>Items</h2>
          <button className="secondary-button" disabled={isSubmitting || products.length === 0} onClick={() => setItems((currentItems) => [...currentItems, createBlankItem(firstProductId)])} type="button">Add item</button>
        </div>
        {getFieldError(errors, 'Items') ? <span className="field-error">{getFieldError(errors, 'Items')}</span> : null}
        {items.map((item, index) => (
          <div className="line-item-row compact-line-item-row" key={index}>
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
            <button className="danger-button" disabled={isSubmitting || items.length === 1} onClick={() => removeItem(index)} type="button">Remove</button>
          </div>
        ))}
      </div>

      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting || customers.length === 0 || products.length === 0} type="submit">{isSubmitting ? 'Saving...' : 'Save draft'}</button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button>
      </div>
    </form>
  )
}
