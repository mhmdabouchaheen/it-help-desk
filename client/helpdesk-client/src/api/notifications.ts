import {apiRequest} from './apiClient'
import type {NotificationListRequest,NotificationPage,NotificationUnreadCountResponse} from '../types/notifications'
export function serializeNotificationQuery(request:NotificationListRequest){const query=new URLSearchParams();if(request.pageNumber!=null)query.set('pageNumber',String(request.pageNumber));if(request.pageSize!=null)query.set('pageSize',String(request.pageSize));if(request.unreadOnly!=null)query.set('unreadOnly',String(request.unreadOnly));return query.toString()}
export function getNotificationsAsync(request:NotificationListRequest={},signal?:AbortSignal){const query=serializeNotificationQuery(request);return apiRequest<NotificationPage>(`/api/notifications${query?`?${query}`:''}`,{method:'GET',signal})}
export const getUnreadNotificationCountAsync=(signal?:AbortSignal)=>apiRequest<NotificationUnreadCountResponse>('/api/notifications/unread-count',{method:'GET',signal})
export const markNotificationReadAsync=(notificationId:string,signal?:AbortSignal)=>apiRequest<void>(`/api/notifications/${encodeURIComponent(notificationId)}/read`,{method:'POST',signal})
export const markAllNotificationsReadAsync=(signal?:AbortSignal)=>apiRequest<void>('/api/notifications/read-all',{method:'POST',signal})
