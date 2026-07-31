import { beforeEach, describe, expect, it, vi } from 'vitest'
import { tokenStore } from './tokenStore'
describe('tokenStore', () => {
  beforeEach(() => tokenStore.clear())
  it('sets and gets tokens only in memory', () => { const local = vi.spyOn(Storage.prototype, 'setItem'); tokenStore.set({ accessToken:'access',refreshToken:'refresh' }); expect(tokenStore.getAccessToken()).toBe('access'); expect(tokenStore.getRefreshToken()).toBe('refresh'); expect(local).not.toHaveBeenCalled() })
  it('clears tokens', () => { tokenStore.set({accessToken:'a',refreshToken:'r'}); tokenStore.clear(); expect(tokenStore.getAccessToken()).toBeNull(); expect(tokenStore.getRefreshToken()).toBeNull() })
})
