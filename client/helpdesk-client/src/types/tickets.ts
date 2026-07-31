export type TicketSortField='CreatedAtUtc'|'UpdatedAtUtc'|'TicketNumber'|'Priority'|'Status'|'Title'
export type SortDirection='asc'|'desc'
export interface TicketListRequest{pageNumber?:number;pageSize?:number;search?:string;categoryId?:number;priorityId?:number;statusId?:number;createdByUserId?:string;assignedToUserId?:string;createdFromUtc?:string;createdToUtc?:string;sortBy?:TicketSortField;sortDirection?:SortDirection}
export interface CreateTicketRequest{title:string;description:string;categoryId:number;priorityId:number}
export type UpdateTicketRequest=CreateTicketRequest
export interface AssignTicketRequest{assignedToUserId:string;note?:string|null}
export interface ChangeTicketStatusRequest{statusId:number;note?:string|null}
export interface AddTicketCommentRequest{content:string;isInternal:boolean}
export interface PagedResponse<T>{items:T[];pageNumber:number;pageSize:number;totalCount:number;totalPages:number;hasPreviousPage:boolean;hasNextPage:boolean}
export interface TicketSummaryResponse{id:string;ticketNumber:string;title:string;categoryId:number;categoryName:string;priorityId:number;priorityName:string;statusId:number;statusName:string;createdByUserId:string;createdByDisplayName:string;assignedToUserId:string|null;assignedToDisplayName:string|null;createdAtUtc:string;updatedAtUtc:string}
export interface TicketCommentResponse{id:string;ticketId:string;authorUserId:string;authorDisplayName:string;body:string;visibility:string;createdAtUtc:string;updatedAtUtc:string|null}
export interface TicketAttachmentResponse{id:string;ticketId:string;commentId:string|null;originalFileName:string;contentType:string;sizeBytes:number;uploadedByUserId:string;uploadedByDisplayName:string;createdAtUtc:string}
export interface TicketAssignmentResponse{id:string;ticketId:string;assignedToUserId:string;assignedToDisplayName:string;assignedByUserId:string|null;assignedByDisplayName:string|null;assignedAtUtc:string;endedAtUtc:string|null;endedByUserId:string|null;endedByDisplayName:string|null;reason:string|null}
export interface TicketStatusHistoryResponse{id:string;ticketId:string;fromStatusId:number|null;fromStatusName:string|null;toStatusId:number;toStatusName:string;changedByUserId:string|null;changedByDisplayName:string|null;changedAtUtc:string;reason:string|null}
export interface TicketDetailResponse extends TicketSummaryResponse{description:string;resolvedAtUtc:string|null;closedAtUtc:string|null;cancelledAtUtc:string|null;comments:TicketCommentResponse[];attachments:TicketAttachmentResponse[];assignmentHistory:TicketAssignmentResponse[];statusHistory:TicketStatusHistoryResponse[]}
export interface TicketCategoryResponse{id:number;name:string;description:string|null;sortOrder:number;isActive:boolean}
export interface TicketPriorityResponse{id:number;name:string;description:string|null;rank:number;isActive:boolean}
export interface TicketStatusResponse{id:number;name:string;description:string|null;sortOrder:number;isTerminal:boolean;isActive:boolean}
