import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Driver, DriverFormValues } from './driversApi'

type DriverFormProps = {
  initialValue?: Driver
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: DriverFormValues) => Promise<void>
}

export function DriverForm({
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: DriverFormProps) {
  const [name, setName] = useState(initialValue?.name ?? '')
  const [phone, setPhone] = useState(initialValue?.phone ?? '')
  const [licenseNumber, setLicenseNumber] = useState(initialValue?.licenseNumber ?? '')
  const [isActive, setIsActive] = useState(initialValue?.isActive ?? true)

  useEffect(() => {
    setName(initialValue?.name ?? '')
    setPhone(initialValue?.phone ?? '')
    setLicenseNumber(initialValue?.licenseNumber ?? '')
    setIsActive(initialValue?.isActive ?? true)
  }, [initialValue])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      name,
      phone,
      licenseNumber,
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
          <span>Phone</span>
          <input disabled={isSubmitting} maxLength={30} onChange={(event) => setPhone(event.target.value)} type="tel" value={phone} />
          {getFieldError(errors, 'Phone') ? <span className="field-error">{getFieldError(errors, 'Phone')}</span> : null}
        </label>
        <label className="form-field">
          <span>License number</span>
          <input disabled={isSubmitting} maxLength={100} onChange={(event) => setLicenseNumber(event.target.value)} type="text" value={licenseNumber} />
          {getFieldError(errors, 'LicenseNumber') ? <span className="field-error">{getFieldError(errors, 'LicenseNumber')}</span> : null}
        </label>
      </div>
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
