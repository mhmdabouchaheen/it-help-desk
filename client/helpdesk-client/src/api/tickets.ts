import {apiRequest} from './apiClient'
import type * as T from '../types/tickets'
export function serializeTicketQuery(request:T.TicketListRequest){const q=new URLSearchParams();for(const [key,value] of Object.entries(request)){if(value==null||value===''||(key==='search'&&String(value).trim()===''))continue;q.set(key,key==='search'?String(value).trim():String(value))}return q.toString()}
export const getTicketsAsync=(request:T.TicketListRequest={},signal?:AbortSignal)=>{const q=serializeTicketQuery(request);return apiRequest<T.PagedResponse<T.TicketSummaryResponse>>(`/api/tickets${q?`?${q}`:''}`,{signal})}
export const createTicketAsync=(request:T.CreateTicketRequest,signal?:AbortSignal)=>apiRequest<T.TicketDetailResponse>('/api/tickets',{method:'POST',body:JSON.stringify(request),signal})
export const getTicketAsync=(id:string,signal?:AbortSignal)=>apiRequest<T.TicketDetailResponse>(`/api/tickets/${encodeURIComponent(id)}`,{signal})
export const updateTicketAsync=(id:string,request:T.UpdateTicketRequest,signal?:AbortSignal)=>apiRequest<T.TicketDetailResponse>(`/api/tickets/${encodeURIComponent(id)}`,{method:'PUT',body:JSON.stringify(request),signal})
export const assignTicketAsync=(id:string,request:T.AssignTicketRequest,signal?:AbortSignal)=>apiRequest<T.TicketDetailResponse>(`/api/tickets/${encodeURIComponent(id)}/assignment`,{method:'POST',body:JSON.stringify(request),signal})
export const changeTicketStatusAsync=(id:string,request:T.ChangeTicketStatusRequest,signal?:AbortSignal)=>apiRequest<T.TicketDetailResponse>(`/api/tickets/${encodeURIComponent(id)}/status`,{method:'POST',body:JSON.stringify(request),signal})
export const addTicketCommentAsync=(id:string,request:T.AddTicketCommentRequest,signal?:AbortSignal)=>apiRequest<T.TicketCommentResponse>(`/api/tickets/${encodeURIComponent(id)}/comments`,{method:'POST',body:JSON.stringify(request),signal})
export const getTicketCategoriesAsync=(signal?:AbortSignal)=>apiRequest<T.TicketCategoryResponse[]>('/api/ticket-lookups/categories',{signal})
export const getTicketPrioritiesAsync=(signal?:AbortSignal)=>apiRequest<T.TicketPriorityResponse[]>('/api/ticket-lookups/priorities',{signal})
export const getTicketStatusesAsync=(signal?:AbortSignal)=>apiRequest<T.TicketStatusResponse[]>('/api/ticket-lookups/statuses',{signal})
