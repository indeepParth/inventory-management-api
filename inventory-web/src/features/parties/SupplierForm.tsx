import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Supplier, SupplierFormValues } from './partiesApi'

type SupplierFormProps = {
  initialValue?: Supplier
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: SupplierFormValues) => Promise<void>
}

export function SupplierForm({
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: SupplierFormProps) {
  const [name, setName] = useState(initialValue?.name ?? '')
  const [contactPerson, setContactPerson] = useState(initialValue?.contactPerson ?? '')
  const [email, setEmail] = useState(initialValue?.email ?? '')
  const [phone, setPhone] = useState(initialValue?.phone ?? '')
  const [address, setAddress] = useState(initialValue?.address ?? '')
  const [gstNumber, setGstNumber] = useState(initialValue?.gstNumber ?? '')
  const [isActive, setIsActive] = useState(initialValue?.isActive ?? true)

  useEffect(() => {
    setName(initialValue?.name ?? '')
    setContactPerson(initialValue?.contactPerson ?? '')
    setEmail(initialValue?.email ?? '')
    setPhone(initialValue?.phone ?? '')
    setAddress(initialValue?.address ?? '')
    setGstNumber(initialValue?.gstNumber ?? '')
    setIsActive(initialValue?.isActive ?? true)
  }, [initialValue])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      name,
      contactPerson,
      email,
      phone,
      address,
      gstNumber,
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
          <span>Email</span>
          <input disabled={isSubmitting} maxLength={254} onChange={(event) => setEmail(event.target.value)} type="email" value={email} />
          {getFieldError(errors, 'Email') ? <span className="field-error">{getFieldError(errors, 'Email')}</span> : null}
        </label>
        <label className="form-field">
          <span>Phone</span>
          <input disabled={isSubmitting} maxLength={30} onChange={(event) => setPhone(event.target.value)} type="tel" value={phone} />
          {getFieldError(errors, 'Phone') ? <span className="field-error">{getFieldError(errors, 'Phone')}</span> : null}
        </label>
        <label className="form-field">
          <span>GST number</span>
          <input disabled={isSubmitting} onChange={(event) => setGstNumber(event.target.value)} type="text" value={gstNumber} />
          {getFieldError(errors, 'GstNumber') ? <span className="field-error">{getFieldError(errors, 'GstNumber')}</span> : null}
        </label>
      </div>
      <label className="form-field">
        <span>Address</span>
        <textarea disabled={isSubmitting} maxLength={500} onChange={(event) => setAddress(event.target.value)} rows={3} value={address} />
        {getFieldError(errors, 'Address') ? <span className="field-error">{getFieldError(errors, 'Address')}</span> : null}
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
