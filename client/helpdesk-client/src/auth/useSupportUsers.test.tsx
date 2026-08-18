import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { SupportUserResponse } from '../types/tickets'
import { invalidateSupportUsers, useSupportUsers } from './useSupportUsers'

const getEligibleSupportUsersAsync = vi.fn()
vi.mock('../api/tickets', () => ({
  getEligibleSupportUsersAsync: () => getEligibleSupportUsersAsync(),
}))

const admin: SupportUserResponse = { id: 'admin', displayName: 'Admin', roles: ['Admin'] }
const promoted: SupportUserResponse = { id: 'agent', displayName: 'New Agent', roles: ['Employee', 'IT Support Agent'] }
const deferred = <T,>() => {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no })
  return { promise, resolve, reject }
}

describe('useSupportUsers', () => {
  beforeEach(() => { invalidateSupportUsers(); getEligibleSupportUsersAsync.mockReset() })

  it('fetches the directory when enabled', async () => {
    getEligibleSupportUsersAsync.mockResolvedValue([admin])
    const { result } = renderHook(() => useSupportUsers(true))
    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.users).toEqual([admin]))
    expect(result.current.isLoading).toBe(false)
    expect(getEligibleSupportUsersAsync).toHaveBeenCalledOnce()
  })

  it('deduplicates concurrent mounts against the same in-flight request', async () => {
    const request = deferred<SupportUserResponse[]>()
    getEligibleSupportUsersAsync.mockReturnValue(request.promise)
    const first = renderHook(() => useSupportUsers(true))
    const second = renderHook(() => useSupportUsers(true))
    expect(getEligibleSupportUsersAsync).toHaveBeenCalledOnce()
    await act(async () => request.resolve([admin]))
    expect(first.result.current.users).toEqual([admin])
    expect(second.result.current.users).toEqual([admin])
  })

  it('fetches fresh data after remount and includes a newly promoted agent', async () => {
    getEligibleSupportUsersAsync.mockResolvedValueOnce([admin]).mockResolvedValueOnce([admin, promoted])
    const first = renderHook(() => useSupportUsers(true))
    await waitFor(() => expect(first.result.current.users).toEqual([admin]))
    first.unmount()
    const second = renderHook(() => useSupportUsers(true))
    await waitFor(() => expect(second.result.current.users).toEqual([admin, promoted]))
    expect(getEligibleSupportUsersAsync).toHaveBeenCalledTimes(2)
  })

  it('session invalidation prevents reuse of an old in-flight directory', async () => {
    const oldSession = deferred<SupportUserResponse[]>()
    getEligibleSupportUsersAsync.mockReturnValueOnce(oldSession.promise).mockResolvedValueOnce([admin, promoted])
    const first = renderHook(() => useSupportUsers(true))
    act(() => invalidateSupportUsers())
    const nextSession = renderHook(() => useSupportUsers(true))
    await waitFor(() => expect(nextSession.result.current.users).toEqual([admin, promoted]))
    expect(getEligibleSupportUsersAsync).toHaveBeenCalledTimes(2)
    await act(async () => oldSession.resolve([admin]))
    expect(first.result.current.users).toEqual([])
    first.unmount()
  })

  it('retries after a failure and clears the error', async () => {
    getEligibleSupportUsersAsync.mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce([admin, promoted])
    const { result } = renderHook(() => useSupportUsers(true))
    await waitFor(() => expect(result.current.error).toBe('Support users could not be loaded.'))
    act(() => result.current.reload())
    expect(result.current.error).toBeUndefined()
    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.users).toEqual([admin, promoted]))
    expect(getEligibleSupportUsersAsync).toHaveBeenCalledTimes(2)
  })

  it('does not refetch on ordinary rerenders', async () => {
    getEligibleSupportUsersAsync.mockResolvedValue([admin])
    const { result, rerender } = renderHook(() => useSupportUsers(true))
    await waitFor(() => expect(result.current.users).toEqual([admin]))
    rerender(); rerender(); rerender()
    expect(getEligibleSupportUsersAsync).toHaveBeenCalledOnce()
  })
})
