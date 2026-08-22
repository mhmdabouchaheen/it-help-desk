import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { LoadingIndicator } from '../components/Feedback'
import {AppRoles,RoleGroups} from '../auth/roles'

export function ProtectedRoute() {
  const auth = useAuth(); const location = useLocation()
  if (auth.isInitializing) return <LoadingIndicator />
  return auth.isAuthenticated ? <Outlet /> : <Navigate to="/login" replace state={{ from: location.pathname }} />
}
export function PublicOnlyRoute() {
  const auth = useAuth()
  if (auth.isInitializing) return <LoadingIndicator />
  return auth.isAuthenticated ? <Navigate to="/app/home" replace /> : <Outlet />
}
export function SupportOnlyRoute(){const auth=useAuth();return RoleGroups.SupportStaff.some(role=>auth.roles.includes(role))?<Outlet/>:<Navigate to="/app/home" replace/>}
export function ReportsRoute(){const auth=useAuth();return RoleGroups.Reports.some(role=>auth.roles.includes(role))?<Outlet/>:<Navigate to="/app/home" replace/>}
export function AdminOnlyRoute(){const auth=useAuth();return auth.roles.includes(AppRoles.Admin)?<Outlet/>:<Navigate to="/app/home" replace/>}
