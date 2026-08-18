import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TicketListPage } from './TicketListPage'

const getTicketsAsync = vi.fn()
vi.mock('../api/tickets', () => ({
  getTicketsAsync: (request: unknown, signal: AbortSignal) => getTicketsAsync(request, signal),
}))
vi.mock('../auth/useLookups', () => ({
  useLookups: () => ({ categories: [], priorities: [], statuses: [], loading: false, error: undefined }),
}))

const response = {
  items: [], pageNumber: 3, pageSize: 50, totalCount: 0, totalPages: 0,
  hasPreviousPage: false, hasNextPage: false,
}

describe('TicketListPage focus refresh', () => {
  beforeEach(() => getTicketsAsync.mockReset().mockResolvedValue(response))

  it('refetches the current URL query on focus without resetting filters, page, or draft text', async () => {
    render(<MemoryRouter initialEntries={['/app/tickets?search=printer&page=3&pageSize=50&sortBy=Title&sortDirection=asc']}><TicketListPage/></MemoryRouter>)
    await waitFor(() => expect(getTicketsAsync).toHaveBeenCalledOnce())
    const firstRequest = getTicketsAsync.mock.calls[0][0]
    expect(firstRequest).toMatchObject({ search: 'printer', pageNumber: 3, pageSize: 50, sortBy: 'Title', sortDirection: 'asc' })

    const search = screen.getByLabelText('Search')
    await userEvent.type(search, ' draft')
    act(() => window.dispatchEvent(new Event('focus')))

    await waitFor(() => expect(getTicketsAsync).toHaveBeenCalledTimes(2))
    expect(getTicketsAsync.mock.calls[1][0]).toEqual(firstRequest)
    expect(search).toHaveValue('printer draft')
    expect(screen.getByText(/Page 0 of 0/)).toBeInTheDocument()
  })
})
