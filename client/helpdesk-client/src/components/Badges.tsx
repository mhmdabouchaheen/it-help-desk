type BadgeTone='neutral'|'info'|'success'|'warning'|'danger'|'accent'
const normalize=(value:string)=>value.trim().toLowerCase().replace(/[ _]+/g,'-')
function statusTone(name:string):BadgeTone{const value=normalize(name);if(['closed','resolved','completed'].includes(value))return'success';if(['open','new'].includes(value))return'info';if(['in-progress','pending'].includes(value))return'warning';return'neutral'}
function priorityTone(name:string):BadgeTone{const value=normalize(name);if(['critical','urgent'].includes(value))return'danger';if(value==='high')return'warning';if(value==='low')return'success';return'neutral'}
export function Badge({children,tone='neutral'}:{children:React.ReactNode;tone?:BadgeTone}){return <span className={`badge badge-${tone}`}>{children}</span>}
export function TicketStatusBadge({name}:{name:string}){return <Badge tone={statusTone(name)}>{name}</Badge>}
export function TicketPriorityBadge({name}:{name:string}){return <Badge tone={priorityTone(name)}>{name}</Badge>}
export function CancelledBadge(){return <span className="cancelled-badge badge badge-danger">Cancelled</span>}
export function RoleBadge({name}:{name:string}){return <Badge tone="accent">{name}</Badge>}
export function VisibilityBadge({visibility}:{visibility:string}){return <Badge tone={normalize(visibility)==='internal'?'warning':'neutral'}>{visibility}</Badge>}
