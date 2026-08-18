import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useRefreshOnFocus } from './useRefreshOnFocus'
import { tokenStore } from './tokenStore'

describe('useRefreshOnFocus', () => {
  beforeEach(() => vi.useFakeTimers({ now: new Date('2026-08-18T12:00:00Z') }))
  afterEach(() => { tokenStore.clear(); vi.useRealTimers(); vi.restoreAllMocks() })

  it('does not invoke refresh during initial render or ordinary rerenders', () => {
    const refresh = vi.fn()
    const { rerender } = renderHook(() => useRefreshOnFocus(refresh))
    rerender(); rerender()
    expect(refresh).not.toHaveBeenCalled()
  })

  it('refreshes on window focus without navigating or reloading', () => {
    const refresh = vi.fn()
    const href = window.location.href
    tokenStore.set({ accessToken: 'still-in-memory', refreshToken: 'refresh' })
    renderHook(() => useRefreshOnFocus(refresh))
    act(() => window.dispatchEvent(new Event('focus')))
    expect(refresh).toHaveBeenCalledOnce()
    expect(window.location.href).toBe(href)
    expect(tokenStore.getAccessToken()).toBe('still-in-memory')
  })

  it('refreshes only when visibility changes to visible', () => {
    const refresh = vi.fn()
    const visibility = vi.spyOn(document, 'visibilityState', 'get')
    renderHook(() => useRefreshOnFocus(refresh))
    visibility.mockReturnValue('hidden')
    act(() => document.dispatchEvent(new Event('visibilitychange')))
    expect(refresh).not.toHaveBeenCalled()
    visibility.mockReturnValue('visible')
    act(() => document.dispatchEvent(new Event('visibilitychange')))
    expect(refresh).toHaveBeenCalledOnce()
  })

  it('deduplicates focus and visibility events that arrive together', () => {
    const refresh = vi.fn()
    vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
    renderHook(() => useRefreshOnFocus(refresh))
    act(() => {
      window.dispatchEvent(new Event('focus'))
      document.dispatchEvent(new Event('visibilitychange'))
      window.dispatchEvent(new Event('focus'))
    })
    expect(refresh).toHaveBeenCalledOnce()
    act(() => vi.advanceTimersByTime(301))
    act(() => window.dispatchEvent(new Event('focus')))
    expect(refresh).toHaveBeenCalledTimes(2)
  })

  it('does not refresh while disabled', () => {
    const refresh = vi.fn()
    renderHook(() => useRefreshOnFocus(refresh, false))
    act(() => window.dispatchEvent(new Event('focus')))
    expect(refresh).not.toHaveBeenCalled()
  })

  it('removes focus and visibility listeners on unmount', () => {
    const refresh = vi.fn()
    vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
    const { unmount } = renderHook(() => useRefreshOnFocus(refresh))
    unmount()
    act(() => {
      window.dispatchEvent(new Event('focus'))
      document.dispatchEvent(new Event('visibilitychange'))
    })
    expect(refresh).not.toHaveBeenCalled()
  })
})
