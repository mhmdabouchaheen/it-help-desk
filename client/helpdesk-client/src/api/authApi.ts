import { apiRequest } from './apiClient'
import { tokenStore } from '../auth/tokenStore'
import type { AuthResponse, ChangePasswordRequest, CurrentUserResponse, ForgotPasswordRequest, LoginRequest, MessageResponse, RegisterRequest, ResetPasswordRequest, UpdateProfileRequest } from '../types/auth'

export const loginAsync = (request: LoginRequest, signal?: AbortSignal) =>
  apiRequest<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(request), signal, skipRefresh: true })
export const registerAsync = (request: RegisterRequest, signal?: AbortSignal) =>
  apiRequest<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify(request), signal, skipRefresh: true })
export const getCurrentUserAsync = (signal?: AbortSignal) =>
  apiRequest<CurrentUserResponse>('/api/auth/me', { signal })
export const forgotPasswordAsync = (request: ForgotPasswordRequest, signal?: AbortSignal) =>
  apiRequest<MessageResponse>('/api/auth/forgot-password', { method: 'POST', body: JSON.stringify(request), signal, skipRefresh: true })
export const resetPasswordAsync = (request: ResetPasswordRequest, signal?: AbortSignal) =>
  apiRequest<MessageResponse>('/api/auth/reset-password', { method: 'POST', body: JSON.stringify(request), signal, skipRefresh: true })
export const getProfileAsync = (signal?: AbortSignal) => apiRequest<CurrentUserResponse>('/api/profile', { signal })
export const updateProfileAsync = (request: UpdateProfileRequest, signal?: AbortSignal) =>
  apiRequest<CurrentUserResponse>('/api/profile', { method: 'PUT', body: JSON.stringify(request), signal })
export const changePasswordAsync = (request: ChangePasswordRequest, signal?: AbortSignal) =>
  apiRequest<void>('/api/profile/change-password', { method: 'POST', body: JSON.stringify(request), signal })
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
