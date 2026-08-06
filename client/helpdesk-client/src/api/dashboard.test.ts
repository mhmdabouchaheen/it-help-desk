import { describe, expect, it, vi } from 'vitest'
import { tokenStore } from '../auth/tokenStore'
import { getDashboardAsync } from './dashboard'

describe('dashboard API',()=>{
  it('uses only the bearer-authenticated dashboard route and forwards cancellation',async()=>{
    tokenStore.set({accessToken:'jwt-value',refreshToken:'refresh-value'})
    const payload={summary:{totalTickets:2},statusBreakdown:[],priorityBreakdown:[],categoryBreakdown:[],monthlyTrend:[],recentTickets:[]}
    const fetchMock=vi.fn().mockResolvedValue(new Response(JSON.stringify(payload),{status:200,headers:{'Content-Type':'application/json'}}))
    vi.stubGlobal('fetch',fetchMock);const controller=new AbortController()
    await expect(getDashboardAsync(controller.signal)).resolves.toEqual(payload)
    const [url,options]=fetchMock.mock.calls[0] as [string,RequestInit]
    const headers=new Headers(options.headers)
    expect(url).toMatch(/\/api\/dashboard$/);expect(url).not.toContain('?')
    expect(options.method).toBe('GET');expect(options.signal).toBe(controller.signal)
    expect(headers.get('Authorization')).toBe('Bearer jwt-value')
    expect([...headers.keys()]).not.toEqual(expect.arrayContaining(['x-user-id','x-roles']))
    expect(options.body).toBeUndefined()
  })
  it('does not persist dashboard analytics',async()=>{
    const setItem=vi.spyOn(Storage.prototype,'setItem')
    vi.stubGlobal('fetch',vi.fn().mockResolvedValue(new Response(JSON.stringify({summary:{},statusBreakdown:[],priorityBreakdown:[],categoryBreakdown:[],monthlyTrend:[],recentTickets:[]}),{status:200,headers:{'Content-Type':'application/json'}})))
    await getDashboardAsync();expect(setItem).not.toHaveBeenCalled()
  })
  it('surfaces safe API errors',async()=>{
    vi.stubGlobal('fetch',vi.fn().mockResolvedValue(new Response(JSON.stringify({title:'Forbidden',traceId:'trace-1'}),{status:403,headers:{'Content-Type':'application/json'}})))
    await expect(getDashboardAsync()).rejects.toMatchObject({status:403,title:'Forbidden',traceId:'trace-1'})
  })
})
