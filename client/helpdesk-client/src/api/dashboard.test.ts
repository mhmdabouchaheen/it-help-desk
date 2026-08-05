import { afterEach,describe,expect,it,vi } from 'vitest'
import { getDashboardAsync } from './dashboard'

afterEach(()=>vi.unstubAllGlobals())
describe('dashboard API',()=>{it('uses the authenticated dashboard route without identity parameters',async()=>{const fetchMock=vi.fn().mockResolvedValue(new Response(JSON.stringify({summary:{},statusBreakdown:[],priorityBreakdown:[],categoryBreakdown:[],monthlyTrend:[],recentTickets:[]}),{status:200,headers:{'Content-Type':'application/json'}}));vi.stubGlobal('fetch',fetchMock);const controller=new AbortController();await getDashboardAsync(controller.signal);const [url,options]=fetchMock.mock.calls[0];expect(url).toMatch(/\/api\/dashboard$/);expect(url).not.toContain('?');expect(options.method).toBe('GET');expect(options.signal).toBe(controller.signal)})})
