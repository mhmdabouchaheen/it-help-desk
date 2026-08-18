import { useCallback, useEffect, useRef, useState } from 'react'
import { getDashboardAsync } from '../api/dashboard';import { ApiProblemError } from '../api/apiClient';import type { DashboardResponse } from '../types/dashboard'
import { useRefreshOnFocus } from './useRefreshOnFocus'
export function useDashboard(){const[dashboard,setDashboard]=useState<DashboardResponse>();const[isLoading,setLoading]=useState(true);const[error,setError]=useState<ApiProblemError|Error>();const sequence=useRef(0);const controller=useRef<AbortController>(undefined);const reload=useCallback(()=>{controller.current?.abort();const current=++sequence.current;const abort=new AbortController();controller.current=abort;setLoading(true);setError(undefined);getDashboardAsync(abort.signal).then(data=>{if(current===sequence.current&&!abort.signal.aborted)setDashboard(data)}).catch(reason=>{if(current===sequence.current&&!abort.signal.aborted){setDashboard(undefined);setError(reason instanceof Error?reason:new Error('Dashboard request failed.'))}}).finally(()=>{if(current===sequence.current&&!abort.signal.aborted)setLoading(false)})},[]);useRefreshOnFocus(reload,!isLoading);useEffect(()=>{
  // The initial request intentionally transitions this external request lifecycle to loading.
  // eslint-disable-next-line react-hooks/set-state-in-effect
  reload();const active=controller.current;return()=>{active?.abort()}
},[reload]);return{dashboard,isLoading,error,reload}}
