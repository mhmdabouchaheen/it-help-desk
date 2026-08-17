import {useEffect,useState} from 'react'
import {getTicketActivityAsync} from '../api/activityLogs'
import type {ActivityLogResponse} from '../types/audit'
import {EmptyState,LoadingIndicator} from './Feedback'
import {friendlyActivityLabel} from '../utils/activity'

export function ActivityMetadata({metadata}:{metadata:Record<string,string|null>}){const entries=Object.entries(metadata);return entries.length?<dl className="activity-metadata">{entries.map(([key,value])=><div key={key}><dt>{friendly(key)}</dt><dd>{value??'None'}</dd></div>)}</dl>:null}
export function AuditTrail({ticketId}:{ticketId:string}){const[items,setItems]=useState<ActivityLogResponse[]>();const[error,setError]=useState(false);const[retry,setRetry]=useState(0);useEffect(()=>{const c=new AbortController();getTicketActivityAsync(ticketId,c.signal).then(setItems).catch(e=>{if(e.name!=='AbortError')setError(true)});return()=>c.abort()},[ticketId,retry]);function retryLoad(){setItems(undefined);setError(false);setRetry(x=>x+1)}return <section><h2>Audit Trail</h2>{error?<div className="error-summary" role="alert"><p>Activity could not be loaded.</p><button type="button" onClick={retryLoad}>Retry</button></div>:items===undefined?<LoadingIndicator/>:items.length===0?<EmptyState title="No activity recorded"/>:<ol className="activity-timeline">{items.map(x=><li key={x.id}><strong>{friendlyActivityLabel(x.action)}</strong><span>{x.actorDisplayName??'System'} · <time dateTime={x.occurredAtUtc}>{new Date(x.occurredAtUtc).toLocaleString()}</time></span><ActivityMetadata metadata={x.metadata}/></li>)}</ol>}</section>}
function friendly(value:string){return value.replace(/[._]/g,' ').replace(/\b\w/g,x=>x.toUpperCase())}
