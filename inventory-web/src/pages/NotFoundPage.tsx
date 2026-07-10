import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main className="public-layout">
      <section className="not-found-panel" aria-labelledby="not-found-title">
        <p className="page-kicker">404</p>
        <h1 id="not-found-title" className="page-title">
          Page not found.
        </h1>
        <p className="page-copy">
          Return to{' '}
          <Link className="text-link" to="/">
            home
          </Link>
          .
        </p>
      </section>
    </main>
  )
}
