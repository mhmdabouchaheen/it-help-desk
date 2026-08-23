export interface TicketReportRequest{fromUtc?:string;toUtc?:string;categoryId?:number;priorityId?:number;statusId?:number;assignedToUserId?:string}
export interface ReportSummary{totalTickets:number;openTickets:number;terminalTickets:number;cancelledTickets:number;assignedTickets:number;unassignedTickets:number;averageResolutionMinutes:number|null}
export interface ReportBreakdown{id:number;name:string;count:number}
export interface ReportTrend{periodStartUtc:string;createdCount:number;closedCount:number}
export interface AgentWorkload{userId:string;displayName:string;activeTicketCount:number}
export interface TicketReportResponse{summary:ReportSummary;statusBreakdown:ReportBreakdown[];priorityBreakdown:ReportBreakdown[];categoryBreakdown:ReportBreakdown[];trend:ReportTrend[];agentWorkload:AgentWorkload[]}
