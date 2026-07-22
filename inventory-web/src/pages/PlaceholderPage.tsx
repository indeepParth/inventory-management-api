import { getAppName } from '../shared/config/appConfig'

type PlaceholderPageProps = {
  title: string
}

export function PlaceholderPage({ title }: PlaceholderPageProps) {
  const appName = getAppName()

  return (
    <section className="content-panel" aria-labelledby="placeholder-title">
      <p className="page-kicker">{appName}</p>
      <h1 id="placeholder-title" className="page-title">
        {title}
      </h1>
      <p className="page-copy">This page will be implemented in a future slice.</p>
    </section>
  )
}
