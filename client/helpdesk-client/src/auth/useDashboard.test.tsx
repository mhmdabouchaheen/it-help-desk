import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { DashboardResponse } from '../types/dashboard'
import { useDashboard } from './useDashboard'

const getDashboardAsync=vi.fn()
vi.mock('../api/dashboard',()=>({getDashboardAsync:(signal?:AbortSignal)=>getDashboardAsync(signal)}))
const dashboard={summary:{totalTickets:1,openTickets:1,inProgressTickets:0,pendingTickets:0,resolvedTickets:0,closedTickets:0,cancelledTickets:0,unassignedTickets:1,assignedTickets:0,criticalTickets:0,createdThisMonth:1,closedThisMonth:0},statusBreakdown:[],priorityBreakdown:[],categoryBreakdown:[],monthlyTrend:[],recentTickets:[]} satisfies DashboardResponse

describe('useDashboard',()=>{
  beforeEach(()=>getDashboardAsync.mockReset())
  it('loads dashboard data and exposes successful state',async()=>{getDashboardAsync.mockResolvedValue(dashboard);const {result}=renderHook(()=>useDashboard());expect(result.current.isLoading).toBe(true);await waitFor(()=>expect(result.current.isLoading).toBe(false));expect(result.current.dashboard).toBe(dashboard);expect(result.current.error).toBeUndefined();expect(getDashboardAsync).toHaveBeenCalledOnce();expect(getDashboardAsync.mock.calls[0][0]).toBeInstanceOf(AbortSignal)})
  it('exposes errors and retries with a fresh request',async()=>{getDashboardAsync.mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce(dashboard);const {result}=renderHook(()=>useDashboard());await waitFor(()=>expect(result.current.error).toEqual(new Error('offline')));act(()=>result.current.reload());expect(result.current.isLoading).toBe(true);expect(result.current.error).toBeUndefined();await waitFor(()=>expect(result.current.dashboard).toBe(dashboard));expect(getDashboardAsync).toHaveBeenCalledTimes(2)})
  it('aborts the active request when unmounted',async()=>{getDashboardAsync.mockResolvedValue(dashboard);const {result,unmount}=renderHook(()=>useDashboard());await waitFor(()=>expect(result.current.isLoading).toBe(false));const signal=getDashboardAsync.mock.calls[0][0] as AbortSignal;expect(signal.aborted).toBe(false);unmount();expect(signal.aborted).toBe(true)})
})
