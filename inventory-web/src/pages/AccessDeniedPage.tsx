import { Link } from 'react-router-dom'

export function AccessDeniedPage() {
  return (
    <section className="content-panel" aria-labelledby="access-denied-title">
      <p className="page-kicker">Access denied</p>
      <h1 id="access-denied-title" className="page-title">
        You do not have access to this page.
      </h1>
      <p className="page-copy">
        Your account is signed in, but this section requires a different role.
      </p>
      <p className="page-action">
        <Link className="text-link" to="/app/dashboard">
          Back to dashboard
        </Link>
      </p>
    </section>
  )
}
