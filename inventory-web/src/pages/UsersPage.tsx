import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { allRoles, type AppRole } from '../features/auth/roleAccess'
import {
  assignUserRole,
  createUser,
  disableUser,
  enableUser,
  getUsers,
  removeUserRole,
  type UserAccount,
} from '../features/auth/usersApi'
import {
  getErrorMessage,
  getFieldError,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'

type CreateUserForm = {
  userName: string
  email: string
  password: string
  roles: AppRole[]
}

const emptyForm: CreateUserForm = {
  userName: '',
  email: '',
  password: '',
  roles: [],
}

export function UsersPage() {
  const [users, setUsers] = useState<UserAccount[]>([])
  const [form, setForm] = useState<CreateUserForm>(emptyForm)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [updatingUserId, setUpdatingUserId] = useState<string | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadUsers = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      setUsers(await getUsers())
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadUsers()
  }, [loadUsers])

  function resetForm(): void {
    setForm(emptyForm)
    setIsFormOpen(false)
    setFieldErrors({})
    setActionError(null)
  }

  function setRole(role: AppRole, isSelected: boolean): void {
    setForm((current) => ({
      ...current,
      roles: isSelected
        ? [...current.roles, role]
        : current.roles.filter((currentRole) => currentRole !== role),
    }))
  }

  async function handleCreateUser(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      await createUser({
        userName: form.userName.trim(),
        email: form.email.trim(),
        password: form.password,
        roles: form.roles,
      })

      resetForm()
      await loadUsers()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRoleChange(
    user: UserAccount,
    role: AppRole,
    isSelected: boolean,
  ): Promise<void> {
    setUpdatingUserId(user.id)
    setActionError(null)

    try {
      if (isSelected) {
        await assignUserRole(user.id, role)
      } else {
        await removeUserRole(user.id, role)
      }

      await loadUsers()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setUpdatingUserId(null)
    }
  }

  async function handleStatusChange(user: UserAccount): Promise<void> {
    const action = user.isDisabled ? 'enable' : 'disable'
    const confirmed = window.confirm(`${action === 'enable' ? 'Enable' : 'Disable'} user "${user.userName}"?`)

    if (!confirmed) {
      return
    }

    setUpdatingUserId(user.id)
    setActionError(null)

    try {
      if (user.isDisabled) {
        await enableUser(user.id)
      } else {
        await disableUser(user.id)
      }

      await loadUsers()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setUpdatingUserId(null)
    }
  }

  return (
    <section className="content-panel wide-panel" aria-labelledby="users-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Administration</p>
          <h1 id="users-title" className="page-title">
            Users
          </h1>
        </div>
        <button
          className="primary-button"
          onClick={() => {
            setIsFormOpen(true)
            setActionError(null)
            setFieldErrors({})
          }}
          type="button"
        >
          New user
        </button>
      </div>

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

      {isFormOpen ? (
        <form className="entity-form" onSubmit={(event) => void handleCreateUser(event)}>
          <div className="form-grid">
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
              Email
              <input
                autoComplete="email"
                onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
                required
                type="email"
                value={form.email}
              />
              {getFieldError(fieldErrors, 'email') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'email')}</span>
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
            <fieldset className="role-fieldset">
              <legend>Roles</legend>
              <div className="role-checkboxes">
                {allRoles.map((role) => (
                  <label className="checkbox-field" key={role}>
                    <input
                      checked={form.roles.includes(role)}
                      onChange={(event) => setRole(role, event.target.checked)}
                      type="checkbox"
                    />
                    {role}
                  </label>
                ))}
              </div>
            </fieldset>
          </div>
          <div className="form-actions">
            <button className="primary-button" disabled={isSaving} type="submit">
              {isSaving ? 'Saving...' : 'Create user'}
            </button>
            <button className="secondary-button" onClick={resetForm} type="button">
              Cancel
            </button>
          </div>
        </form>
      ) : null}

      {isLoading ? <LoadingState>Loading users...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
      {!isLoading && !errorMessage && users.length === 0 ? (
        <EmptyState>No users found.</EmptyState>
      ) : null}

      {!isLoading && !errorMessage && users.length > 0 ? (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Username</th>
                <th>Email</th>
                <th>Roles</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td>{user.userName}</td>
                  <td>{user.email || '-'}</td>
                  <td>
                    <div className="role-checkboxes compact">
                      {allRoles.map((role) => (
                        <label className="checkbox-field" key={role}>
                          <input
                            checked={user.roles.includes(role)}
                            disabled={updatingUserId === user.id}
                            onChange={(event) => void handleRoleChange(user, role, event.target.checked)}
                            type="checkbox"
                          />
                          {role}
                        </label>
                      ))}
                    </div>
                  </td>
                  <td>{user.isDisabled ? 'Disabled' : 'Active'}</td>
                  <td>
                    <div className="table-actions">
                      <button
                        className={user.isDisabled ? 'text-button' : 'danger-button'}
                        disabled={updatingUserId === user.id}
                        onClick={() => void handleStatusChange(user)}
                        type="button"
                      >
                        {user.isDisabled ? 'Enable' : 'Disable'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  )
}
