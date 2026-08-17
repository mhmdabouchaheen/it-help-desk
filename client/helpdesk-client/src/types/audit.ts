import type { PagedResponse } from './tickets'
export interface ActivityLogResponse{id:number;actorUserId:string|null;actorDisplayName:string|null;action:string;entityType:string;entityIdentifier:string;occurredAtUtc:string;metadata:Record<string,string|null>}
export interface ActivityLogListRequest{pageNumber?:number;pageSize?:number;action?:string;entityType?:string;actorUserId?:string;fromUtc?:string;toUtc?:string}
export type ActivityLogPage=PagedResponse<ActivityLogResponse>
