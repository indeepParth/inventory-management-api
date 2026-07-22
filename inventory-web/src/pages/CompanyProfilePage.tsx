import { useEffect, useState, type FormEvent } from 'react'
import {
  getCompanyProfile,
  updateCompanyProfile,
  type CompanyProfileFormValues,
} from '../features/companyProfile/companyProfileApi'
import {
  getErrorMessage,
  getFieldError,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { ErrorBanner, LoadingState } from '../shared/components/Feedback'

const emptyProfile: CompanyProfileFormValues = {
  companyName: '',
  address: '',
  gstNumber: '',
  email: '',
  phone: '',
  website: '',
}

export function CompanyProfilePage() {
  const [values, setValues] = useState<CompanyProfileFormValues>(emptyProfile)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  useEffect(() => {
    async function loadCompanyProfile(): Promise<void> {
      setIsLoading(true)
      setErrorMessage(null)

      try {
        const profile = await getCompanyProfile()
        setValues({
          companyName: profile.companyName ?? '',
          address: profile.address ?? '',
          gstNumber: profile.gstNumber ?? '',
          email: profile.email ?? '',
          phone: profile.phone ?? '',
          website: profile.website ?? '',
        })
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsLoading(false)
      }
    }

    void loadCompanyProfile()
  }, [])

  function updateField(field: keyof CompanyProfileFormValues, value: string): void {
    setValues((current) => ({
      ...current,
      [field]: value,
    }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setIsSaving(true)
    setErrorMessage(null)
    setSuccessMessage(null)
    setFieldErrors({})

    try {
      const profile = await updateCompanyProfile(values)
      setValues({
        companyName: profile.companyName ?? '',
        address: profile.address ?? '',
        gstNumber: profile.gstNumber ?? '',
        email: profile.email ?? '',
        phone: profile.phone ?? '',
        website: profile.website ?? '',
      })
      setSuccessMessage('Company profile saved.')
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="content-panel" aria-labelledby="company-profile-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Admin</p>
          <h1 id="company-profile-title" className="page-title">Company Profile</h1>
        </div>
      </div>

      {isLoading ? <LoadingState>Loading company profile...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {!isLoading ? (
        <form className="entity-form profile-form" onSubmit={(event) => void handleSubmit(event)}>
          {successMessage ? (
            <p className="feedback-banner success" role="status">
              {successMessage}
            </p>
          ) : null}
          <label className="form-field">
            Company name
            <input
              maxLength={150}
              onChange={(event) => updateField('companyName', event.target.value)}
              required
              type="text"
              value={values.companyName}
            />
            {getFieldError(fieldErrors, 'companyName') ? (
              <span className="field-error">{getFieldError(fieldErrors, 'companyName')}</span>
            ) : null}
          </label>
          <label className="form-field">
            Address
            <textarea
              maxLength={500}
              onChange={(event) => updateField('address', event.target.value)}
              rows={4}
              value={values.address}
            />
            {getFieldError(fieldErrors, 'address') ? (
              <span className="field-error">{getFieldError(fieldErrors, 'address')}</span>
            ) : null}
          </label>
          <div className="form-grid">
            <label className="form-field">
              GST number
              <input
                maxLength={15}
                onChange={(event) => updateField('gstNumber', event.target.value)}
                type="text"
                value={values.gstNumber}
              />
              {getFieldError(fieldErrors, 'gstNumber') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'gstNumber')}</span>
              ) : null}
            </label>
            <label className="form-field">
              Email
              <input
                maxLength={254}
                onChange={(event) => updateField('email', event.target.value)}
                type="email"
                value={values.email}
              />
              {getFieldError(fieldErrors, 'email') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'email')}</span>
              ) : null}
            </label>
            <label className="form-field">
              Phone
              <input
                maxLength={30}
                onChange={(event) => updateField('phone', event.target.value)}
                type="tel"
                value={values.phone}
              />
              {getFieldError(fieldErrors, 'phone') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'phone')}</span>
              ) : null}
            </label>
            <label className="form-field">
              Website
              <input
                maxLength={200}
                onChange={(event) => updateField('website', event.target.value)}
                type="url"
                value={values.website}
              />
              {getFieldError(fieldErrors, 'website') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'website')}</span>
              ) : null}
            </label>
          </div>
          <div className="form-actions">
            <button className="primary-button" disabled={isSaving} type="submit">
              {isSaving ? 'Saving...' : 'Save company profile'}
            </button>
          </div>
        </form>
      ) : null}
    </section>
  )
}
