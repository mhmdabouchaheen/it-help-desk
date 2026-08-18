import{useCallback,useEffect,useRef,useState}from'react'
import{getTicketReportAsync}from'../api/reports'
import type{TicketReportRequest,TicketReportResponse}from'../types/reports'
import{useRefreshOnFocus}from'./useRefreshOnFocus'
export function useReports(filters:TicketReportRequest,refreshEnabled=true){const[data,setData]=useState<TicketReportResponse>();const[loading,setLoading]=useState(true);const[error,setError]=useState<Error>();const sequence=useRef(0);const controller=useRef<AbortController>(undefined);const stable=JSON.stringify(filters);const reload=useCallback(()=>{controller.current?.abort();const id=++sequence.current;const abort=new AbortController();controller.current=abort;setLoading(true);setError(undefined);getTicketReportAsync(JSON.parse(stable)as TicketReportRequest,abort.signal).then(x=>{if(id===sequence.current&&!abort.signal.aborted)setData(x)}).catch(x=>{if(id===sequence.current&&!abort.signal.aborted){setData(undefined);setError(x instanceof Error?x:new Error('Report request failed.'))}}).finally(()=>{if(id===sequence.current&&!abort.signal.aborted)setLoading(false)})},[stable]);useRefreshOnFocus(reload,refreshEnabled&&!loading);useEffect(()=>{
  // The initial request intentionally transitions this external request lifecycle to loading.
  // eslint-disable-next-line react-hooks/set-state-in-effect
  reload();const active=controller.current;return()=>active?.abort()
},[reload]);return{data,loading,error,reload}}
