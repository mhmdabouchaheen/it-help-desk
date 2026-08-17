import{apiRequest}from'./apiClient';import type{AiTicketAnalysisResponse}from'../types/ai'
export function analyzeTicketAsync(ticketId:string,signal?:AbortSignal){return apiRequest<AiTicketAnalysisResponse>(`/api/tickets/${encodeURIComponent(ticketId)}/ai-analysis`,{method:'POST',signal})}
