import { useState, type FormEvent } from 'react'
import { changePassword } from '../features/auth/usersApi'
import {
  getErrorMessage,
  getFieldError,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { ErrorBanner } from '../shared/components/Feedback'

export function ChangePasswordPage() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  async function handlePasswordSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setIsSaving(true)
    setErrorMessage(null)
    setSuccessMessage(null)
    setFieldErrors({})

    try {
      await changePassword({
        currentPassword,
        newPassword,
        confirmPassword,
      })

      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setSuccessMessage('Password updated.')
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="content-panel" aria-labelledby="change-password-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Account</p>
          <h1 id="change-password-title" className="page-title">Change password</h1>
        </div>
      </div>

      <form className="entity-form profile-form" onSubmit={(event) => void handlePasswordSubmit(event)}>
        {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
        {successMessage ? (
          <p className="feedback-banner success" role="status">
            {successMessage}
          </p>
        ) : null}
        <label className="form-field">
          Current password
          <input
            autoComplete="current-password"
            onChange={(event) => setCurrentPassword(event.target.value)}
            required
            type="password"
            value={currentPassword}
          />
          {getFieldError(fieldErrors, 'currentPassword') ? (
            <span className="field-error">{getFieldError(fieldErrors, 'currentPassword')}</span>
          ) : null}
        </label>
        <label className="form-field">
          New password
          <input
            autoComplete="new-password"
            onChange={(event) => setNewPassword(event.target.value)}
            required
            type="password"
            value={newPassword}
          />
          {getFieldError(fieldErrors, 'newPassword') ? (
            <span className="field-error">{getFieldError(fieldErrors, 'newPassword')}</span>
          ) : null}
        </label>
        <label className="form-field">
          Confirm new password
          <input
            autoComplete="new-password"
            onChange={(event) => setConfirmPassword(event.target.value)}
            required
            type="password"
            value={confirmPassword}
          />
          {getFieldError(fieldErrors, 'confirmPassword') ? (
            <span className="field-error">{getFieldError(fieldErrors, 'confirmPassword')}</span>
          ) : null}
        </label>
        <div className="form-actions">
          <button className="primary-button" disabled={isSaving} type="submit">
            {isSaving ? 'Saving...' : 'Change password'}
          </button>
        </div>
      </form>
    </section>
  )
}
