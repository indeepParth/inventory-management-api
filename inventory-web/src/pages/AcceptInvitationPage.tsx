import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import {
  acceptCompanyInvitation,
  getCompanyInvitation,
  type CompanyInvitation,
} from '../features/companyInvitations/companyInvitationsApi'
import {
  getErrorMessage,
  getFieldError,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { ErrorBanner, LoadingState } from '../shared/components/Feedback'

type NewUserForm = {
  userName: string
  password: string
  confirmPassword: string
}

const emptyForm: NewUserForm = {
  userName: '',
  password: '',
  confirmPassword: '',
}

export function AcceptInvitationPage() {
  const { token } = useParams()
  const navigate = useNavigate()
  const { isAuthenticated, currentUser, refreshCurrentUser, selectCompany } = useAuth()
  const [invitation, setInvitation] = useState<CompanyInvitation | null>(null)
  const [form, setForm] = useState<NewUserForm>(emptyForm)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadInvitation = useCallback(async (): Promise<void> => {
    if (!token) {
      setErrorMessage('Invitation link is invalid.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      setInvitation(await getCompanyInvitation(token))
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [token])

  useEffect(() => {
    void loadInvitation()
  }, [loadInvitation])

  async function finishAccept(mode: 'existingUser' | 'newUser'): Promise<void> {
    if (!token) {
      setErrorMessage('Invitation link is invalid.')
      return
    }

    setIsSaving(true)
    setErrorMessage(null)
    setFieldErrors({})

    try {
      const company = await acceptCompanyInvitation(
        token,
        mode === 'existingUser'
          ? { mode }
          : {
              mode,
              userName: form.userName.trim(),
              password: form.password,
              confirmPassword: form.confirmPassword,
            },
      )

      if (isAuthenticated) {
        await refreshCurrentUser()
        await selectCompany(company.id)
        navigate('/app/dashboard', { replace: true })
      } else {
        navigate('/login', { replace: true })
      }
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleCreateAccount(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await finishAccept('newUser')
  }

  return (
    <section className="public-panel" aria-labelledby="accept-invitation-title">
      <p className="page-kicker">Company invitation</p>
      <h1 id="accept-invitation-title" className="page-title">
        Accept invitation
      </h1>

      {isLoading ? <LoadingState>Loading invitation...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {!isLoading && invitation ? (
        <div className="invitation-summary">
          <p>
            <strong>{invitation.companyName}</strong>
          </p>
          <p>{invitation.email}</p>
          <p>{invitation.role}</p>
        </div>
      ) : null}

      {!isLoading && invitation && isAuthenticated ? (
        <div className="accept-invitation-actions">
          <button
            className="primary-button"
            disabled={isSaving}
            onClick={() => void finishAccept('existingUser')}
            type="button"
          >
            {isSaving ? 'Accepting...' : 'Accept with current account'}
          </button>
          {currentUser?.email && currentUser.email.toLowerCase() !== invitation.email.toLowerCase() ? (
            <p className="form-error">
              Current account email does not match this invitation.
            </p>
          ) : null}
        </div>
      ) : null}

      {!isLoading && invitation && !isAuthenticated ? (
        <>
          <form className="login-form" onSubmit={(event) => void handleCreateAccount(event)}>
            <label className="form-field">
              Username
              <input
                autoComplete="username"
                onChange={(event) => setForm((current) => ({ ...current, userName: event.target.value }))}
                required
                type="text"
                value={form.userName}
              />
              {getFieldError(fieldErrors, 'userName') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'userName')}</span>
              ) : null}
            </label>
            <label className="form-field">
              Password
              <input
                autoComplete="new-password"
                onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
                required
                type="password"
                value={form.password}
              />
              {getFieldError(fieldErrors, 'password') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'password')}</span>
              ) : null}
            </label>
            <label className="form-field">
              Confirm password
              <input
                autoComplete="new-password"
                onChange={(event) => setForm((current) => ({ ...current, confirmPassword: event.target.value }))}
                required
                type="password"
                value={form.confirmPassword}
              />
              {getFieldError(fieldErrors, 'confirmPassword') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'confirmPassword')}</span>
              ) : null}
            </label>
            <button className="primary-button" disabled={isSaving} type="submit">
              {isSaving ? 'Creating...' : 'Create account and accept'}
            </button>
          </form>
          <p className="public-footnote">
            Already have an account? <Link to="/login">Log in</Link>, then open this invitation again.
          </p>
        </>
      ) : null}
    </section>
  )
}
