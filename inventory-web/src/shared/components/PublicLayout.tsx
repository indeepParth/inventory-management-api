import { Outlet } from 'react-router-dom'

export function PublicLayout() {
  return (
    <main className="public-layout">
      <Outlet />
    </main>
  )
}
