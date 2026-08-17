import { Inbox } from 'lucide-react'

export function ErrorSummary({message,traceId}:{message?:string;traceId?:string}) {
  if (!message) return null
  return <div className="error-summary" role="alert" tabIndex={-1}>{message}{traceId&&<small> Reference: {traceId}</small>}</div>
}

export function FieldError({id,message}:{id:string;message?:string}) {
  return message?<span className="field-error" id={id}>{message}</span>:null
}

export function LoadingIndicator({label='Loading…'}:{label?:string}) {
  return <div className="route-loading" role="status" aria-live="polite"><span className="spinner" aria-hidden="true"/>{label}</div>
}

export function EmptyState({title,detail}:{title:string;detail?:string}) {
  return <div className="empty-state"><Inbox aria-hidden="true"/><strong>{title}</strong>{detail&&<p>{detail}</p>}</div>
}
