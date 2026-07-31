import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { LoadingIndicator } from '../components/Feedback'

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
