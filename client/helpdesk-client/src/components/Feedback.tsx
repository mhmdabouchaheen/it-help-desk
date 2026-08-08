export function ErrorSummary({message,traceId}:{message?:string;traceId?:string}){if(!message)return null;return <div className="error-summary" role="alert" tabIndex={-1}>{message}{traceId&&<small> Reference: {traceId}</small>}</div>}
export function FieldError({id,message}:{id:string;message?:string}){return message?<span className="field-error" id={id}>{message}</span>:null}
export function LoadingIndicator(){return <div className="route-loading" role="status" aria-live="polite"><span className="spinner" aria-hidden="true"/>Loadingâ€¦</div>}
