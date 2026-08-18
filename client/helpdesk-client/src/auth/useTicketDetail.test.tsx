import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useTicketDetail } from './useTicketDetail'

const getTicketAsync = vi.fn()
vi.mock('../api/tickets', () => ({ getTicketAsync: (id: string, signal: AbortSignal) => getTicketAsync(id, signal) }))

const ticket = { id: 'ticket-1', title: 'Ticket', comments: [] }

describe('useTicketDetail', () => {
  beforeEach(() => getTicketAsync.mockReset().mockResolvedValue(ticket))

  it('keeps the same ticket id and aborts the previous request when reloaded', async () => {
    const { result } = renderHook(() => useTicketDetail('ticket-1'))
    await waitFor(() => expect(result.current.loading).toBe(false))
    const firstSignal = getTicketAsync.mock.calls[0][1] as AbortSignal
    await act(() => result.current.reload())
    expect(firstSignal.aborted).toBe(true)
    expect(getTicketAsync).toHaveBeenCalledTimes(2)
    expect(getTicketAsync.mock.calls[1][0]).toBe('ticket-1')
  })
})
