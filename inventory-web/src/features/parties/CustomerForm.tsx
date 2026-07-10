import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Customer, CustomerFormValues } from './partiesApi'

type CustomerFormProps = {
  initialValue?: Customer
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: CustomerFormValues) => Promise<void>
}

export function CustomerForm({
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: CustomerFormProps) {
  const [name, setName] = useState(initialValue?.name ?? '')
  const [contactPerson, setContactPerson] = useState(initialValue?.contactPerson ?? '')
  const [phone, setPhone] = useState(initialValue?.phone ?? '')
  const [email, setEmail] = useState(initialValue?.email ?? '')
  const [billingAddress, setBillingAddress] = useState(initialValue?.billingAddress ?? '')
  const [deliveryAddress, setDeliveryAddress] = useState(initialValue?.deliveryAddress ?? '')
  const [gstNumber, setGstNumber] = useState(initialValue?.gstNumber ?? '')
  const [creditLimit, setCreditLimit] = useState(initialValue?.creditLimit.toString() ?? '0')
  const [isActive, setIsActive] = useState(initialValue?.isActive ?? true)

  useEffect(() => {
    setName(initialValue?.name ?? '')
    setContactPerson(initialValue?.contactPerson ?? '')
    setPhone(initialValue?.phone ?? '')
    setEmail(initialValue?.email ?? '')
    setBillingAddress(initialValue?.billingAddress ?? '')
    setDeliveryAddress(initialValue?.deliveryAddress ?? '')
    setGstNumber(initialValue?.gstNumber ?? '')
    setCreditLimit(initialValue?.creditLimit.toString() ?? '0')
    setIsActive(initialValue?.isActive ?? true)
  }, [initialValue])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      name,
      contactPerson,
      phone,
      email,
      billingAddress,
      deliveryAddress,
      gstNumber,
      creditLimit: Number(creditLimit),
      isActive,
    })
  }

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <label className="form-field">
          <span>Name</span>
          <input disabled={isSubmitting} maxLength={150} onChange={(event) => setName(event.target.value)} required type="text" value={name} />
          {getFieldError(errors, 'Name') ? <span className="field-error">{getFieldError(errors, 'Name')}</span> : null}
        </label>
        <label className="form-field">
          <span>Contact person</span>
          <input disabled={isSubmitting} maxLength={150} onChange={(event) => setContactPerson(event.target.value)} type="text" value={contactPerson} />
          {getFieldError(errors, 'ContactPerson') ? <span className="field-error">{getFieldError(errors, 'ContactPerson')}</span> : null}
        </label>
        <label className="form-field">
          <span>Phone</span>
          <input disabled={isSubmitting} maxLength={30} onChange={(event) => setPhone(event.target.value)} type="tel" value={phone} />
          {getFieldError(errors, 'Phone') ? <span className="field-error">{getFieldError(errors, 'Phone')}</span> : null}
        </label>
        <label className="form-field">
          <span>Email</span>
          <input disabled={isSubmitting} maxLength={254} onChange={(event) => setEmail(event.target.value)} type="email" value={email} />
          {getFieldError(errors, 'Email') ? <span className="field-error">{getFieldError(errors, 'Email')}</span> : null}
        </label>
        <label className="form-field">
          <span>GST number</span>
          <input disabled={isSubmitting} onChange={(event) => setGstNumber(event.target.value)} type="text" value={gstNumber} />
          {getFieldError(errors, 'GstNumber') ? <span className="field-error">{getFieldError(errors, 'GstNumber')}</span> : null}
        </label>
        <label className="form-field">
          <span>Credit limit</span>
          <input disabled={isSubmitting} min="0" onChange={(event) => setCreditLimit(event.target.value)} step="0.01" type="number" value={creditLimit} />
          {getFieldError(errors, 'CreditLimit') ? <span className="field-error">{getFieldError(errors, 'CreditLimit')}</span> : null}
        </label>
      </div>
      <label className="form-field">
        <span>Billing address</span>
        <textarea disabled={isSubmitting} maxLength={500} onChange={(event) => setBillingAddress(event.target.value)} rows={3} value={billingAddress} />
        {getFieldError(errors, 'BillingAddress') ? <span className="field-error">{getFieldError(errors, 'BillingAddress')}</span> : null}
      </label>
      <label className="form-field">
        <span>Delivery address</span>
        <textarea disabled={isSubmitting} maxLength={500} onChange={(event) => setDeliveryAddress(event.target.value)} rows={3} value={deliveryAddress} />
        {getFieldError(errors, 'DeliveryAddress') ? <span className="field-error">{getFieldError(errors, 'DeliveryAddress')}</span> : null}
      </label>
      {initialValue ? (
        <label className="checkbox-field">
          <input checked={isActive} disabled={isSubmitting} onChange={(event) => setIsActive(event.target.checked)} type="checkbox" />
          <span>Active</span>
        </label>
      ) : null}
      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting} type="submit">{isSubmitting ? 'Saving...' : 'Save'}</button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">Cancel</button>
      </div>
    </form>
  )
}
