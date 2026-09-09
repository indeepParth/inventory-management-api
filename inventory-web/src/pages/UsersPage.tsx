import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useAuth } from '../features/auth/AuthContext'
import { allRoles, type AppRole } from '../features/auth/roleAccess'
import {
  assignUserRole,
  disableUser,
  enableUser,
  getUsers,
  removeUserRole,
  type UserAccount,
} from '../features/auth/usersApi'
import {
  createCompanyInvitation,
  getCompanyInvitations,
  revokeCompanyInvitation,
  type CompanyInvitation,
} from '../features/companyInvitations/companyInvitationsApi'
import {
  getErrorMessage,
  getFieldError,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatDate } from '../shared/utils/formatters'

type InviteForm = {
  email: string
  role: AppRole | ''
}

const emptyForm: InviteForm = {
  email: '',
  role: '',
}

function isPrivilegedRole(role: string | undefined): boolean {
  return role === 'Owner' || role === 'Admin'
}

export function UsersPage() {
  const { activeCompany } = useAuth()
  const [users, setUsers] = useState<UserAccount[]>([])
  const [invitations, setInvitations] = useState<CompanyInvitation[]>([])
  const [form, setForm] = useState<InviteForm>(emptyForm)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [updatingKey, setUpdatingKey] = useState<string | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [latestInviteUrl, setLatestInviteUrl] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const canManagePrivilegedRoles = activeCompany?.role === 'Owner'
  const availableRoles = useMemo(
    () => allRoles.filter((role) => canManagePrivilegedRoles || !isPrivilegedRole(role)),
    [canManagePrivilegedRoles],
  )

  const loadAccess = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [loadedUsers, loadedInvitations] = await Promise.all([
        getUsers(),
        getCompanyInvitations(),
      ])

      setUsers(loadedUsers)
      setInvitations(loadedInvitations)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadAccess()
  }, [loadAccess])

  function resetForm(): void {
    setForm(emptyForm)
    setIsFormOpen(false)
    setFieldErrors({})
    setActionError(null)
  }

  async function handleCreateInvitation(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)
    setLatestInviteUrl(null)

    try {
      const invitation = await createCompanyInvitation({
        email: form.email.trim(),
        role: form.role as AppRole,
      })

      setLatestInviteUrl(new URL(invitation.acceptUrl, window.location.origin).toString())
      resetForm()
      await loadAccess()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRoleChange(user: UserAccount, role: AppRole): Promise<void> {
    setUpdatingKey(`role-${user.id}`)
    setActionError(null)

    try {
      await assignUserRole(user.id, role)
      await loadAccess()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setUpdatingKey(null)
    }
  }

  async function handleRemoveAccess(user: UserAccount): Promise<void> {
    const role = user.roles[0] as AppRole | undefined

    if (!role) {
      return
    }

    const confirmed = window.confirm(`Remove access for "${user.userName}" from this company?`)

    if (!confirmed) {
      return
    }

    setUpdatingKey(`remove-${user.id}`)
    setActionError(null)

    try {
      await removeUserRole(user.id, role)
      await loadAccess()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setUpdatingKey(null)
    }
  }

  async function handleStatusChange(user: UserAccount): Promise<void> {
    const action = user.isDisabled ? 'enable' : 'disable'
    const confirmed = window.confirm(`${action === 'enable' ? 'Enable' : 'Disable'} user "${user.userName}"?`)

    if (!confirmed) {
      return
    }

    setUpdatingKey(`status-${user.id}`)
    setActionError(null)

    try {
      if (user.isDisabled) {
        await enableUser(user.id)
      } else {
        await disableUser(user.id)
      }

      await loadAccess()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setUpdatingKey(null)
    }
  }

  async function handleRevokeInvitation(invitation: CompanyInvitation): Promise<void> {
    const confirmed = window.confirm(`Revoke invitation for "${invitation.email}"?`)

    if (!confirmed) {
      return
    }

    setUpdatingKey(`invite-${invitation.id}`)
    setActionError(null)

    try {
      await revokeCompanyInvitation(invitation.id)
      await loadAccess()
    } catch (error) {
      setActionError(getErrorMessage(error))
    } finally {
      setUpdatingKey(null)
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
            setLatestInviteUrl(null)
          }}
          type="button"
        >
          Invite user
        </button>
      </div>

      {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}
      {latestInviteUrl ? (
        <div className="feedback-banner success">
          <span>Invitation link created.</span>
          <input className="copyable-text" readOnly value={latestInviteUrl} />
        </div>
      ) : null}

      {isFormOpen ? (
        <form className="entity-form" onSubmit={(event) => void handleCreateInvitation(event)}>
          <div className="form-grid">
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
              Role
              <select
                onChange={(event) =>
                  setForm((current) => ({ ...current, role: event.target.value as AppRole | '' }))
                }
                required
                value={form.role}
              >
                <option value="">Select role</option>
                {availableRoles.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
              {getFieldError(fieldErrors, 'role') ? (
                <span className="field-error">{getFieldError(fieldErrors, 'role')}</span>
              ) : null}
            </label>
          </div>
          <div className="form-actions">
            <button className="primary-button" disabled={isSaving} type="submit">
              {isSaving ? 'Sending...' : 'Create invite'}
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
        <EmptyState>No company members found.</EmptyState>
      ) : null}

      {!isLoading && !errorMessage && users.length > 0 ? (
        <>
          <h2 className="users-section-title">Members</h2>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Username</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => {
                  const userRole = user.roles[0] as AppRole | undefined
                  const roleIsRestricted = isPrivilegedRole(userRole) && !canManagePrivilegedRoles
                  const canRemove = Boolean(userRole) && !roleIsRestricted

                  return (
                    <tr key={user.id}>
                      <td>{user.userName}</td>
                      <td>{user.email || '-'}</td>
                      <td>
                        <select
                          aria-label={`Role for ${user.userName}`}
                          disabled={updatingKey !== null || roleIsRestricted}
                          onChange={(event) => void handleRoleChange(user, event.target.value as AppRole)}
                          value={userRole ?? ''}
                        >
                          <option value="" disabled>
                            No role
                          </option>
                          {availableRoles.map((role) => (
                            <option key={role} value={role}>
                              {role}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td>{user.isDisabled ? 'Disabled' : 'Active'}</td>
                      <td>
                        <div className="table-actions">
                          <button
                            className={user.isDisabled ? 'text-button' : 'danger-button'}
                            disabled={updatingKey !== null}
                            onClick={() => void handleStatusChange(user)}
                            type="button"
                          >
                            {user.isDisabled ? 'Enable' : 'Disable'}
                          </button>
                          <button
                            className="text-button"
                            disabled={updatingKey !== null || !canRemove}
                            onClick={() => void handleRemoveAccess(user)}
                            type="button"
                          >
                            Remove access
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </>
      ) : null}

      {!isLoading && !errorMessage ? (
        <>
          <h2 className="users-section-title">Invitations</h2>
          {invitations.length === 0 ? (
            <EmptyState>No invitations found.</EmptyState>
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Email</th>
                    <th>Role</th>
                    <th>Status</th>
                    <th>Invited by</th>
                    <th>Expires</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {invitations.map((invitation) => {
                    const canRevoke = invitation.status === 'Pending' &&
                      (canManagePrivilegedRoles || !isPrivilegedRole(invitation.role))

                    return (
                      <tr key={invitation.id}>
                        <td>{invitation.email}</td>
                        <td>{invitation.role}</td>
                        <td>{invitation.status}</td>
                        <td>{invitation.invitedByUserName || '-'}</td>
                        <td>{formatDate(invitation.expiresAtUtc)}</td>
                        <td>
                          <button
                            className="text-button"
                            disabled={updatingKey !== null || !canRevoke}
                            onClick={() => void handleRevokeInvitation(invitation)}
                            type="button"
                          >
                            Revoke
                          </button>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      ) : null}
    </section>
  )
}
