import { Link } from 'react-router-dom'

export function HomePage() {
  return (
    <section className="public-panel" aria-labelledby="home-title">
      <p className="page-kicker">Inventory Management</p>
      <h1 id="home-title" className="page-title">
          Frontend setup is ready.
      </h1>
      <p className="page-copy">
        This placeholder will become the inventory management web app in future slices.
      </p>
      <p className="page-copy">
        <Link className="text-link" to="/app/dashboard">
          Open dashboard
        </Link>
      </p>
    </section>
  )
}
