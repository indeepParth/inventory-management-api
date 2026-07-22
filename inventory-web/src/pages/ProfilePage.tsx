import { useAuth } from '../features/auth/AuthContext'

export function ProfilePage() {
  const { currentUser } = useAuth()

  return (
    <section className="content-panel" aria-labelledby="profile-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Account</p>
          <h1 id="profile-title" className="page-title">
            Profile
          </h1>
        </div>
      </div>

      <div className="detail-grid">
        <span>Username</span>
        <strong>{currentUser?.username ?? '-'}</strong>
        <span>Email</span>
        <strong>{currentUser?.email || '-'}</strong>
        <span>Roles</span>
        <strong>{currentUser?.roles.length ? currentUser.roles.join(', ') : '-'}</strong>
        <span>Status</span>
        <strong>{currentUser?.isDisabled ? 'Disabled' : 'Active'}</strong>
      </div>
    </section>
  )
}
