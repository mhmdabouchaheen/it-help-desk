import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import * as authApi from '../api/authApi'
import { ApiProblemError } from '../api/apiClient'
import { tokenStore } from './tokenStore'
import type { CurrentUserResponse, LoginRequest, RegisterRequest } from '../types/auth'

interface AuthContextValue {
  user: CurrentUserResponse | null
  roles: string[]
  isAuthenticated: boolean
  isInitializing: boolean
  login(request: LoginRequest): Promise<void>
  register(request: RegisterRequest): Promise<void>
  logout(): Promise<void>
  reloadCurrentUser(): Promise<void>
  hasRole(role: string): boolean
  hasAnyRole(roles: readonly string[]): boolean
}
const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUserResponse | null>(null)
  const [isInitializing] = useState(false)

  const acceptAuth = useCallback(async (auth: Awaited<ReturnType<typeof authApi.loginAsync>>) => {
    tokenStore.set({ accessToken: auth.accessToken, refreshToken: auth.refreshToken })
    setUser({ userId: auth.userId, email: auth.email, displayName: auth.displayName, roles: auth.roles, isActive: true })
  }, [])
  const login = useCallback(async (request: LoginRequest) => acceptAuth(await authApi.loginAsync(request)), [acceptAuth])
  const register = useCallback(async (request: RegisterRequest) => acceptAuth(await authApi.registerAsync(request)), [acceptAuth])
  const logout = useCallback(async () => { try { await authApi.logoutAsync() } finally { tokenStore.clear(); setUser(null) } }, [])
  const reloadCurrentUser = useCallback(async () => {
    try { setUser(await authApi.getCurrentUserAsync()) }
    catch (error) { if (error instanceof ApiProblemError && error.status === 401) { tokenStore.clear(); setUser(null) } throw error }
  }, [])
  const hasRole = useCallback((role: string) => user?.roles.includes(role) ?? false, [user])
  const hasAnyRole = useCallback((roles: readonly string[]) => roles.some(hasRole), [hasRole])
  const value = useMemo<AuthContextValue>(() => ({ user, roles: user?.roles ?? [], isAuthenticated: user !== null,
    isInitializing, login, register, logout, reloadCurrentUser, hasRole, hasAnyRole }),
  [user, isInitializing, login, register, logout, reloadCurrentUser, hasRole, hasAnyRole])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// The hook intentionally shares the provider's private context.
// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider.')
  return context
}
