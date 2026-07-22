import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../shared/api/apiClient'
import { useAuth } from '../features/auth/AuthContext'
import { getAppName } from '../shared/config/appConfig'

function getLoginErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessages = Object.values(error.problemDetails?.errors ?? {}).flat()

    if (validationMessages.length > 0) {
      return validationMessages.join(' ')
    }

    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Login failed. Please try again.'
}

export function LoginPage() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const appName = getAppName()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setErrorMessage(null)
    setIsLoading(true)

    try {
      await login({
        userName,
        password,
      })
      navigate('/app/dashboard', { replace: true })
    } catch (error) {
      setErrorMessage(getLoginErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <section className="public-panel" aria-labelledby="login-title">
      <p className="page-kicker">Sign in</p>
      <h1 id="login-title" className="page-title">
        Login to {appName}.
      </h1>
      <form className="login-form" onSubmit={handleSubmit}>
        <label className="form-field">
          <span>User name</span>
          <input
            autoComplete="username"
            disabled={isLoading}
            onChange={(event) => setUserName(event.target.value)}
            required
            type="text"
            value={userName}
          />
        </label>

        <label className="form-field">
          <span>Password</span>
          <input
            autoComplete="current-password"
            disabled={isLoading}
            onChange={(event) => setPassword(event.target.value)}
            required
            type="password"
            value={password}
          />
        </label>

        {errorMessage ? (
          <p className="form-error" role="alert">
            {errorMessage}
          </p>
        ) : null}

        <button className="primary-button" disabled={isLoading} type="submit">
          {isLoading ? 'Logging in...' : 'Login'}
        </button>
      </form>
    </section>
  )
}
