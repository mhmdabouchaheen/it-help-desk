import {apiRequest} from './apiClient'
import type{ActivityLogListRequest,ActivityLogPage}from'../types/audit'
export function serializeActivityQuery(request:ActivityLogListRequest){const q=new URLSearchParams();for(const[key,value]of Object.entries(request))if(value!==undefined&&value!==null&&value!=='')q.set(key,String(value));return q.toString()}
export const getActivityLogsAsync=(request:ActivityLogListRequest={},signal?:AbortSignal)=>{const q=serializeActivityQuery(request);return apiRequest<ActivityLogPage>(`/api/activity-logs${q?`?${q}`:''}`,{signal})}
export const getTicketActivityAsync=(ticketId:string,request:Pick<ActivityLogListRequest,'pageNumber'|'pageSize'>={},signal?:AbortSignal)=>{const q=serializeActivityQuery(request);return apiRequest<ActivityLogPage>(`/api/tickets/${encodeURIComponent(ticketId)}/activity${q?`?${q}`:''}`,{signal})}
