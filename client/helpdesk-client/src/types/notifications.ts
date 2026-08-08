import type { PagedResponse } from './tickets'
export interface NotificationResponse{id:string;ticketId:string|null;type:string;title:string;message:string;createdAtUtc:string;readAtUtc:string|null;isRead:boolean}
export interface NotificationListRequest{pageNumber?:number;pageSize?:number;unreadOnly?:boolean}
export interface NotificationUnreadCountResponse{unreadCount:number}
export type NotificationPage=PagedResponse<NotificationResponse>
export interface NotificationRealtimeEvent{notificationId:string;ticketId:string|null;type:string;createdAtUtc:string}
