import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'

export function AppLayout() {
  const auth = useAuth(); const navigate = useNavigate(); const [busy, setBusy] = useState(false)
  async function logout() { if (busy) return; setBusy(true); try { await auth.logout() } finally { navigate('/login', { replace: true }) } }
  return <div className="app-shell">
    <header><strong>IT Help Desk</strong><div className="identity"><span>{auth.user?.displayName}</span><small>{auth.roles.join(', ')}</small><button onClick={logout} disabled={busy}>{busy ? 'Signing out…' : 'Sign out'}</button></div></header>
    <nav aria-label="Primary navigation"><NavLink to="/app/home">Dashboard</NavLink><NavLink to="/app/tickets">Tickets</NavLink><NavLink to="/app/tickets/new">Create Ticket</NavLink></nav>
    <main><Outlet /></main>
  </div>
}
