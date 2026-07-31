export interface AuthTokens { accessToken: string; refreshToken: string }

let tokens: AuthTokens | null = null
const listeners = new Set<() => void>()

export const tokenStore = {
  getAccessToken: () => tokens?.accessToken ?? null,
  getRefreshToken: () => tokens?.refreshToken ?? null,
  set(next: AuthTokens) { tokens = { ...next }; listeners.forEach((listener) => listener()) },
  clear() { tokens = null; listeners.forEach((listener) => listener()) },
  subscribe(listener: () => void) { listeners.add(listener); return () => listeners.delete(listener) },
}
