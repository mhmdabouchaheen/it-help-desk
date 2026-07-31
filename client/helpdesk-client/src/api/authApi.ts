import { apiRequest } from './apiClient'
import { tokenStore } from '../auth/tokenStore'
import type { AuthResponse, CurrentUserResponse, LoginRequest, RegisterRequest } from '../types/auth'

export const loginAsync = (request: LoginRequest, signal?: AbortSignal) =>
  apiRequest<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(request), signal, skipRefresh: true })
export const registerAsync = (request: RegisterRequest, signal?: AbortSignal) =>
  apiRequest<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify(request), signal, skipRefresh: true })
export const getCurrentUserAsync = (signal?: AbortSignal) =>
  apiRequest<CurrentUserResponse>('/api/auth/me', { signal })
export const refreshAsync = (refreshToken: string, signal?: AbortSignal) =>
  apiRequest<AuthResponse>('/api/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }), signal, skipRefresh: true })
export async function logoutAsync(signal?: AbortSignal): Promise<void> {
  const refreshToken = tokenStore.getRefreshToken()
  try {
    if (refreshToken) await apiRequest<void>('/api/auth/logout', {
      method: 'POST', body: JSON.stringify({ refreshToken }), signal, skipRefresh: true,
    })
  } finally { tokenStore.clear() }
}
