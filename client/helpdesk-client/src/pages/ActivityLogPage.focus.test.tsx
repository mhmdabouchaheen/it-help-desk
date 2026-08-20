import {act,render,screen,waitFor} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {afterEach,beforeEach,describe,expect,it,vi} from 'vitest'
import {ActivityLogPage} from './ActivityLogPage'

const {getActivityLogsAsync}=vi.hoisted(()=>({getActivityLogsAsync:vi.fn()}))
vi.mock('../api/activityLogs',()=>({getActivityLogsAsync}))

const response={items:[{id:1,actorUserId:null,actorDisplayName:'System',action:'ticket.created',entityType:'Ticket',entityIdentifier:'ticket-1',occurredAtUtc:'2026-08-20T12:00:00Z',metadata:{}}],pageNumber:1,pageSize:20,totalCount:2,totalPages:2,hasPreviousPage:false,hasNextPage:true}

describe('ActivityLogPage focus refresh',()=>{
  beforeEach(()=>getActivityLogsAsync.mockReset().mockImplementation((request:{pageNumber?:number}={})=>Promise.resolve({...response,pageNumber:request.pageNumber??1,hasPreviousPage:(request.pageNumber??1)>1,hasNextPage:(request.pageNumber??1)<2})))
  afterEach(()=>vi.restoreAllMocks())

  it('loads once initially and ordinary rerenders do not create requests',async()=>{
    const view=render(<ActivityLogPage/>);await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledOnce());expect(getActivityLogsAsync.mock.calls[0][0]).toEqual({pageNumber:1,pageSize:20});view.rerender(<ActivityLogPage/>);view.rerender(<ActivityLogPage/>);expect(getActivityLogsAsync).toHaveBeenCalledOnce()
  })

  it('refreshes once for a focus and visible event pair without navigation',async()=>{
    vi.spyOn(document,'visibilityState','get').mockReturnValue('visible');const href=window.location.href;render(<ActivityLogPage/>);await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledOnce());act(()=>{window.dispatchEvent(new Event('focus'));document.dispatchEvent(new Event('visibilitychange'))});await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledTimes(2));expect(window.location.href).toBe(href)
  })

  it('refreshes only when visibility becomes visible',async()=>{
    const visibility=vi.spyOn(document,'visibilityState','get');render(<ActivityLogPage/>);await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledOnce());visibility.mockReturnValue('hidden');act(()=>document.dispatchEvent(new Event('visibilitychange')));expect(getActivityLogsAsync).toHaveBeenCalledOnce();visibility.mockReturnValue('visible');act(()=>document.dispatchEvent(new Event('visibilitychange')));await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledTimes(2))
  })

  it('preserves the current page, applied filters, and filter drafts on focus',async()=>{
    const user=userEvent.setup();render(<ActivityLogPage/>);await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledOnce());await user.type(screen.getByLabelText('Action'),'ticket.updated');await user.type(screen.getByLabelText('Entity type'),'Ticket');await user.type(screen.getByLabelText('From'),'2026-08-01T10:00');await user.type(screen.getByLabelText('To'),'2026-08-20T18:00');await user.click(screen.getByRole('button',{name:'Apply filters'}));await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledTimes(2));await user.click(await screen.findByRole('button',{name:'Next'}));await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledTimes(3));const applied=getActivityLogsAsync.mock.calls[2][0];expect(applied).toMatchObject({pageNumber:2,pageSize:20,action:'ticket.updated',entityType:'Ticket'});act(()=>window.dispatchEvent(new Event('focus')));await waitFor(()=>expect(getActivityLogsAsync).toHaveBeenCalledTimes(4));expect(getActivityLogsAsync.mock.calls[3][0]).toEqual(applied);expect(screen.getByLabelText('Action')).toHaveValue('ticket.updated');expect(screen.getByLabelText('Entity type')).toHaveValue('Ticket');expect(screen.getByLabelText('From')).toHaveValue('2026-08-01T10:00');expect(screen.getByLabelText('To')).toHaveValue('2026-08-20T18:00')
  })
})
