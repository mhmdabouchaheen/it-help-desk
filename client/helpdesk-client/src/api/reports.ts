import{apiRequest,apiResponse}from'./apiClient'
import type{TicketReportRequest,TicketReportResponse}from'../types/reports'
export function serializeReportFilters(request:TicketReportRequest){const query=new URLSearchParams();for(const[key,value]of Object.entries(request)){if(value!==undefined&&value!=='')query.set(key,String(value))}return query.toString()}
export function getTicketReportAsync(request:TicketReportRequest={},signal?:AbortSignal){const query=serializeReportFilters(request);return apiRequest<TicketReportResponse>(`/api/reports/tickets${query?`?${query}`:''}`,{method:'GET',signal})}
export interface ReportDownload{blob:Blob;fileName:string}
async function exportReport(format:'pdf'|'excel',request:TicketReportRequest,signal?:AbortSignal):Promise<ReportDownload>{const query=serializeReportFilters(request);const response=await apiResponse(`/api/reports/tickets/export/${format}${query?`?${query}`:''}`,{method:'GET',signal});const fallback=`ticket-report.${format==='pdf'?'pdf':'xlsx'}`;return{blob:await response.blob(),fileName:safeFileName(response.headers.get('content-disposition'),fallback)}}
export const exportTicketReportPdfAsync=(request:TicketReportRequest,signal?:AbortSignal)=>exportReport('pdf',request,signal)
export const exportTicketReportExcelAsync=(request:TicketReportRequest,signal?:AbortSignal)=>exportReport('excel',request,signal)
function safeFileName(disposition:string|null,fallback:string){const encoded=/filename\*=UTF-8''([^;]+)/i.exec(disposition??'')?.[1];const plain=/filename="?([^";]+)"?/i.exec(disposition??'')?.[1];let name:string;try{name=decodeURIComponent(encoded??plain??fallback)}catch{return fallback}return /^[\w.-]+$/.test(name)&&!name.includes('..')?name:fallback}
