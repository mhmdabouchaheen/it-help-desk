import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import {NotificationsProvider,useNotifications} from '../auth/useNotifications'

export function AppLayout(){return <NotificationsProvider><AuthenticatedLayout/></NotificationsProvider>}
function AuthenticatedLayout() {
  const auth = useAuth(); const navigate = useNavigate(); const [busy, setBusy] = useState(false)
  const notifications=useNotifications()
  async function logout() { if (busy) return; setBusy(true); try { await auth.logout() } finally { navigate('/login', { replace: true }) } }
  return <div className="app-shell">
    <header><strong>IT Help Desk</strong><div className="identity"><span>{auth.user?.displayName}</span><small>{auth.roles.join(', ')}</small><button onClick={logout} disabled={busy}>{busy ? 'Signing out…' : 'Sign out'}</button></div></header>
    <nav aria-label="Primary navigation"><NavLink to="/app/home">Dashboard</NavLink><NavLink to="/app/tickets">Tickets</NavLink><NavLink to="/app/tickets/new">Create Ticket</NavLink><NavLink to="/app/notifications">Notifications{notifications.unreadCount>0&&<span className="notification-badge" aria-label={`${notifications.unreadCount} unread notifications`}>{notifications.unreadCount>99?'99+':notifications.unreadCount}</span>}</NavLink></nav>
    <main><Outlet /></main>
  </div>
}
