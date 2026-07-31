import{beforeEach,describe,expect,it,vi}from'vitest';import*as api from'./tickets';import{tokenStore}from'../auth/tokenStore'
const response=(body:unknown)=>new Response(JSON.stringify(body),{status:200,headers:{'content-type':'application/json'}})
describe('ticket query',()=>{it('omits undefined and blank values',()=>expect(api.serializeTicketQuery({search:' ',categoryId:undefined})).toBe(''));it('trims search and preserves exact sort',()=>expect(api.serializeTicketQuery({search:' printer ',sortBy:'TicketNumber',sortDirection:'asc'})).toBe('search=printer&sortBy=TicketNumber&sortDirection=asc'));it('preserves ISO dates',()=>{const date='2026-01-01T00:00:00.000Z';expect(new URLSearchParams(api.serializeTicketQuery({createdFromUtc:date})).get('createdFromUtc')).toBe(date)})})
describe('ticket API routes',()=>{beforeEach(()=>{tokenStore.clear();vi.stubGlobal('fetch',vi.fn().mockResolvedValue(response({items:[]})))});it.each([
 ['get',()=>api.getTicketsAsync({}),'https://api.test/api/tickets','GET'],
 ['create',()=>api.createTicketAsync({title:'x',description:'x',categoryId:1,priorityId:1}),'https://api.test/api/tickets','POST'],
 ['detail',()=>api.getTicketAsync('abc'),'https://api.test/api/tickets/abc','GET'],
 ['update',()=>api.updateTicketAsync('abc',{title:'x',description:'x',categoryId:1,priorityId:1}),'https://api.test/api/tickets/abc','PUT'],
 ['assign',()=>api.assignTicketAsync('abc',{assignedToUserId:'u'}),'https://api.test/api/tickets/abc/assignment','POST'],
 ['status',()=>api.changeTicketStatusAsync('abc',{statusId:2}),'https://api.test/api/tickets/abc/status','POST'],
 ['comment',()=>api.addTicketCommentAsync('abc',{content:'x',isInternal:false}),'https://api.test/api/tickets/abc/comments','POST'],
 ['categories',()=>api.getTicketCategoriesAsync(),'https://api.test/api/ticket-lookups/categories','GET'],
 ['priorities',()=>api.getTicketPrioritiesAsync(),'https://api.test/api/ticket-lookups/priorities','GET'],
 ['statuses',()=>api.getTicketStatusesAsync(),'https://api.test/api/ticket-lookups/statuses','GET'],
 ])('%s uses exact route',async(_,call,url,method)=>{await call();const[actual,init]=vi.mocked(fetch).mock.calls[0];expect(actual).toBe(url);expect(init?.method??'GET').toBe(method)});it('forwards AbortSignal',async()=>{const signal=new AbortController().signal;await api.getTicketAsync('abc',signal);expect(vi.mocked(fetch).mock.calls[0][1]?.signal).toBe(signal)});it('never places a token in ticket URLs',async()=>{tokenStore.set({accessToken:'secret',refreshToken:'refresh'});await api.getTicketsAsync({search:'safe'});expect(String(vi.mocked(fetch).mock.calls[0][0])).not.toContain('secret')})})
