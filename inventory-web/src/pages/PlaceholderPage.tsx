type PlaceholderPageProps = {
  title: string
}

export function PlaceholderPage({ title }: PlaceholderPageProps) {
  return (
    <section className="content-panel" aria-labelledby="placeholder-title">
      <p className="page-kicker">Inventory Web</p>
      <h1 id="placeholder-title" className="page-title">
        {title}
      </h1>
      <p className="page-copy">This page will be implemented in a future slice.</p>
    </section>
  )
}
