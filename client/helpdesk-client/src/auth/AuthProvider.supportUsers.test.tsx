import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider, useAuth } from './AuthProvider'

const mocks = vi.hoisted(() => ({
  invalidateSupportUsers: vi.fn(),
  loginAsync: vi.fn(),
  logoutAsync: vi.fn(),
}))

vi.mock('./useSupportUsers', () => ({ invalidateSupportUsers: mocks.invalidateSupportUsers }))
vi.mock('../api/authApi', () => ({
  loginAsync: mocks.loginAsync,
  logoutAsync: mocks.logoutAsync,
}))

const authResponse = {
  accessToken: 'access', refreshToken: 'refresh',
  expiresAtUtc: '2026-08-18T12:00:00Z', refreshTokenExpiresAtUtc: '2026-08-19T12:00:00Z',
  userId: 'user-1', email: 'admin@example.test', displayName: 'Admin', roles: ['Admin'],
}

describe('AuthProvider support-user invalidation', () => {
  beforeEach(() => {
    mocks.invalidateSupportUsers.mockReset()
    mocks.loginAsync.mockReset().mockResolvedValue(authResponse)
    mocks.logoutAsync.mockReset().mockResolvedValue(undefined)
  })

  it('invalidates the directory on login and logout session changes', async () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => <AuthProvider>{children}</AuthProvider>
    const { result } = renderHook(() => useAuth(), { wrapper })

    await act(() => result.current.login({ email: 'admin@example.test', password: 'Password1!' }))
    expect(mocks.invalidateSupportUsers).toHaveBeenCalledTimes(1)
    expect(result.current.isAuthenticated).toBe(true)

    await act(() => result.current.logout())
    expect(mocks.invalidateSupportUsers).toHaveBeenCalledTimes(2)
    expect(result.current.isAuthenticated).toBe(false)
  })
})
