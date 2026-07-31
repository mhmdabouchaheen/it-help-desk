export interface LoginRequest { email: string; password: string }
export interface RegisterRequest { email: string; password: string; confirmPassword: string; displayName: string }
export interface RefreshTokenRequest { refreshToken: string }
export interface LogoutRequest { refreshToken: string }
export interface AuthResponse {
  accessToken: string
  expiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
  userId: string
  email: string
  displayName: string
  roles: string[]
}
export interface CurrentUserResponse {
  userId: string
  email: string
  displayName: string
  roles: string[]
  isActive: boolean
}
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  code?: string
  traceId?: string
  errors?: Record<string, string[]>
}
