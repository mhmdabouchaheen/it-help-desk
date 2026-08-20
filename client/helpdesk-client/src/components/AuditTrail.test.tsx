import {useState} from 'react'
import {render,screen,waitFor} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {beforeEach,describe,expect,it,vi} from 'vitest'
import {AuditTrail} from './AuditTrail'

const {getTicketActivityAsync}=vi.hoisted(()=>({getTicketActivityAsync:vi.fn()}))
vi.mock('../api/activityLogs',()=>({getTicketActivityAsync}))

const item=(id:number,action:string)=>({id,actorUserId:null,actorDisplayName:'Support User',action,entityType:'Ticket',entityIdentifier:'ticket-1',occurredAtUtc:`2026-08-20T12:00:0${id}Z`,metadata:{}})
const page=(pageNumber:number,totalPages:number,items=[item(pageNumber,`ticket.page-${pageNumber}`)])=>({items,pageNumber,pageSize:20,totalCount:totalPages*20,totalPages,hasPreviousPage:pageNumber>1,hasNextPage:pageNumber<totalPages})

describe('AuditTrail',()=>{
  beforeEach(()=>getTicketActivityAsync.mockReset().mockImplementation((_ticketId:string,request:{pageNumber:number}={pageNumber:1})=>Promise.resolve(page(request.pageNumber,2))))

  it('renders the first page with disabled Previous and loads Next then Previous without navigation',async()=>{
    const originalLocation=window.location.href
    render(<AuditTrail ticketId="ticket-1"/>)
    expect(await screen.findByText('Ticket Page-1')).toBeInTheDocument()
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
    expect(screen.getByRole('button',{name:'Previous'})).toBeDisabled()
    await userEvent.click(screen.getByRole('button',{name:'Next'}))
    expect(await screen.findByText('Ticket Page-2')).toBeInTheDocument()
    expect(getTicketActivityAsync).toHaveBeenLastCalledWith('ticket-1',{pageNumber:2,pageSize:20},expect.any(AbortSignal))
    await userEvent.click(screen.getByRole('button',{name:'Previous'}))
    expect(await screen.findByText('Ticket Page-1')).toBeInTheDocument()
    expect(window.location.href).toBe(originalLocation)
  })

  it('returns to page one after a new activity refresh without resetting a sibling draft',async()=>{
    function Harness(){const[version,setVersion]=useState(0);const[draft,setDraft]=useState('');return <><label>Comment draft<input value={draft} onChange={e=>setDraft(e.target.value)}/></label><button onClick={()=>setVersion(x=>x+1)}>Activity created</button><AuditTrail key={version} ticketId="ticket-1"/></>}
    render(<Harness/>)
    await screen.findByText('Ticket Page-1')
    await userEvent.click(screen.getByRole('button',{name:'Next'}))
    await screen.findByText('Ticket Page-2')
    await userEvent.type(screen.getByRole('textbox',{name:'Comment draft'}),'keep this draft')
    await userEvent.click(screen.getByRole('button',{name:'Activity created'}))
    await waitFor(()=>expect(getTicketActivityAsync).toHaveBeenLastCalledWith('ticket-1',{pageNumber:1,pageSize:20},expect.any(AbortSignal)))
    expect(screen.getByRole('textbox',{name:'Comment draft'})).toHaveValue('keep this draft')
    expect(await screen.findByText('Page 1 of 2')).toBeInTheDocument()
  })
})
