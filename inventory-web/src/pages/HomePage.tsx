import { Link } from 'react-router-dom'

const benefits = [
  'Stock',
  'Purchases',
  'Sales',
  'Challans',
  'Invoices',
  'Payments',
  'Reports',
]

export function HomePage() {
  return (
    <section className="public-panel landing-panel" aria-labelledby="home-title">
      <p className="page-kicker">Inventory Management</p>
      <h1 id="home-title" className="page-title">
        Inventory Web
      </h1>
      <p className="page-copy">
        A focused workspace for managing inventory operations from stock movement to
        financial reporting.
      </p>

      <ul className="benefit-list" aria-label="Inventory Web benefits">
        {benefits.map((benefit) => (
          <li className="benefit-item" key={benefit}>
            {benefit}
          </li>
        ))}
      </ul>

      <p className="page-action">
        <Link className="primary-link" to="/login">
          Login
        </Link>
      </p>
    </section>
  )
}
