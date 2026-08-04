import { useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { addTicketCommentAsync, assignTicketAsync, cancelTicketAsync, changeTicketStatusAsync, deleteTicketAttachmentAsync, downloadTicketAttachmentAsync, uploadTicketAttachmentAsync } from "../api/tickets";
import { ApiProblemError } from "../api/apiClient";
import { useAuth } from "../auth/AuthProvider";
import { AppRoles, RoleGroups } from "../auth/roles";
import { useLookups } from "../auth/useLookups";
import { useTicketDetail } from "../auth/useTicketDetail";
import { invalidateSupportUsers, useSupportUsers } from "../auth/useSupportUsers";
import { ErrorSummary, LoadingIndicator } from "../components/Feedback";
import { formatDate, isGuid } from "../utils/tickets";
export function TicketDetailPage() {
  const { id } = useParams();
  if (!isGuid(id))
    return (
      <section>
        <h1>Invalid ticket</h1>
        <p>The ticket identifier is invalid.</p>
      </section>
    );
  return <Detail id={id!} />;
}
function Detail({ id }: { id: string }) {
  const auth = useAuth();
  const lookups = useLookups();
  const { ticket, setTicket, loading, error, reload } = useTicketDetail(id);
  const support = auth.hasAnyRole(RoleGroups.SupportStaff);
  const supportUsers = useSupportUsers(support);
  const [statusId, setStatusId] = useState(0);
  const [note, setNote] = useState("");
  const [content, setContent] = useState("");
  const [internal, setInternal] = useState(false);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string>();
  const [assigneeId, setAssigneeId] = useState("");
  const [assignmentNote, setAssignmentNote] = useState("");
  const [assigning, setAssigning] = useState(false);
  const [assignmentAllowed, setAssignmentAllowed] = useState(true);
  const [attachmentBusy, setAttachmentBusy] = useState<string>();
  const [attachmentFile, setAttachmentFile] = useState<File>();
  const fileInput = useRef<HTMLInputElement>(null);
  const [showCancel, setShowCancel] = useState(false);
  const [cancelReason, setCancelReason] = useState("");
  const [cancelling, setCancelling] = useState(false);
  if (loading) return <LoadingIndicator />;
  if (error || !ticket)
    return (
      <section>
        <ErrorSummary message={error} />
        <button onClick={reload}>Retry</button>
      </section>
    );
  const currentTicket = ticket;
  const cancelled = currentTicket.cancelledAtUtc !== null;
  const canEdit = !cancelled && (support || currentTicket.createdByUserId === auth.user?.userId);
  const terminal = lookups.statuses.find(x=>x.id===currentTicket.statusId)?.isTerminal ?? true;
  const ownRecognized = currentTicket.createdByUserId===auth.user?.userId && auth.hasAnyRole([AppRoles.Employee,AppRoles.Manager]);
  const canCancel = !cancelled && (support || ownRecognized && !terminal);
  async function status(e: FormEvent) {
    e.preventDefault();
    if (!statusId || statusId === currentTicket.statusId) return;
    setBusy(true);
    setActionError(undefined);
    try {
      setTicket(
        await changeTicketStatusAsync(id, {
          statusId,
          note: note.trim() || null,
        }),
      );
    } catch {
      setActionError(
        "Status could not be changed. The backend may reject this transition.",
      );
    } finally {
      setBusy(false);
    }
  }
  async function comment(e: FormEvent) {
    e.preventDefault();
    const body = content.trim();
    if (!body) {
      setActionError("Comment cannot be blank.");
      return;
    }
    setBusy(true);
    try {
      await addTicketCommentAsync(id, {
        content: body,
        isInternal: support && internal,
      });
      setContent("");
      setInternal(false);
      await reload();
    } catch {
      setActionError("Comment could not be added.");
    } finally {
      setBusy(false);
    }
  }
  async function assign(e: FormEvent) {
    e.preventDefault();
    if (!support || !assignmentAllowed || assigning || !assigneeId || assigneeId === currentTicket.assignedToUserId ||
        !supportUsers.users.some((user) => user.id === assigneeId)) return;
    setAssigning(true); setActionError(undefined);
    try {
      setTicket(await assignTicketAsync(id, { assignedToUserId: assigneeId, note: assignmentNote.trim() || null }));
      setAssignmentNote("");
    } catch (error) {
      if (error instanceof ApiProblemError && error.code === "assignment_target_not_found") {
        setActionError("The selected support user is no longer available. Reload the support-user list and try again.");
        invalidateSupportUsers(); supportUsers.reload();
      } else if (error instanceof ApiProblemError && error.code === "ticket_state_conflict") {
        setActionError("The ticket changed while you were assigning it. Reload the ticket and try again.");
      } else if (error instanceof ApiProblemError && (error.code === "access_forbidden" || error.code === "ticket_access_denied")) {
        setAssignmentAllowed(false); setActionError("You are no longer authorized to assign this ticket.");
      } else setActionError("The ticket could not be assigned.");
    } finally { setAssigning(false) }
  }
  function chooseAttachment(e: ChangeEvent<HTMLInputElement>) {
    const file=e.target.files?.[0];setActionError(undefined);if(!file){setAttachmentFile(undefined);return}
    const extension=`.${file.name.split('.').pop()?.toLowerCase()}`;
    if(!['.png','.jpg','.jpeg','.webp','.pdf','.txt','.docx','.xlsx'].includes(extension)){setActionError('That file type is not allowed.');e.target.value='';return}
    if(file.size<=0||file.size>10*1024*1024){setActionError('Attachments must be between 1 byte and 10 MB.');e.target.value='';return}setAttachmentFile(file)
  }
  async function uploadAttachment(e:FormEvent){e.preventDefault();if(!attachmentFile||attachmentBusy)return;setAttachmentBusy('upload');setActionError(undefined);try{const uploaded=await uploadTicketAttachmentAsync(id,attachmentFile);setTicket({...currentTicket,attachments:[...currentTicket.attachments,uploaded]});setAttachmentFile(undefined);if(fileInput.current)fileInput.current.value=''}catch(error){setActionError(error instanceof ApiProblemError&&error.code==='attachment_too_large'?'The attachment exceeds the 10 MB limit.':error instanceof ApiProblemError&&error.code==='attachment_validation_failed'?'The attachment did not pass validation.':'The attachment could not be uploaded.')}finally{setAttachmentBusy(undefined)}}
  async function downloadAttachment(attachmentId:string,fileName:string){if(attachmentBusy)return;setAttachmentBusy(attachmentId);setActionError(undefined);try{const blob=await downloadTicketAttachmentAsync(id,attachmentId);const url=URL.createObjectURL(blob);try{const link=document.createElement('a');link.href=url;link.download=fileName;link.click()}finally{URL.revokeObjectURL(url)}}catch{setActionError('The attachment could not be downloaded.')}finally{setAttachmentBusy(undefined)}}
  async function deleteAttachment(attachmentId:string){if(attachmentBusy||!window.confirm('Delete this attachment?'))return;setAttachmentBusy(attachmentId);setActionError(undefined);try{await deleteTicketAttachmentAsync(id,attachmentId);setTicket({...currentTicket,attachments:currentTicket.attachments.filter(x=>x.id!==attachmentId)})}catch{setActionError('The attachment could not be deleted.')}finally{setAttachmentBusy(undefined)}}
  async function cancelTicket(e:FormEvent){e.preventDefault();if(!canCancel||cancelling||cancelReason.length>500)return;setCancelling(true);setActionError(undefined);try{setTicket(await cancelTicketAsync(id,{reason:cancelReason.trim()||null}));setShowCancel(false);setCancelReason('')}catch{setActionError('The ticket could not be cancelled. It may have changed or you may no longer have access.')}finally{setCancelling(false)}}
  return (
    <section className="ticket-detail">
      <div className="page-heading">
        <div>
          <p>{ticket.ticketNumber}</p>
          <h1>{ticket.title}</h1>
        </div>
        {canEdit && (
          <Link className="button-link" to={`/app/tickets/${id}/edit`}>
            Edit details
          </Link>
        )}
      </div>
      <dl className="summary">
        <div>
          <dt>Status</dt>
          <dd>{ticket.statusName}</dd>
        </div>
        <div>
          <dt>Priority</dt>
          <dd>{ticket.priorityName}</dd>
        </div>
        <div>
          <dt>Category</dt>
          <dd>{ticket.categoryName}</dd>
        </div>
        <div>
          <dt>Creator</dt>
          <dd>{ticket.createdByDisplayName}</dd>
        </div>
        <div>
          <dt>Assignee</dt>
          <dd>{ticket.assignedToDisplayName ?? "Unassigned"}</dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd title={ticket.createdAtUtc}>{formatDate(ticket.createdAtUtc)}</dd>
        </div>
      </dl>
      <section>
        <h2>Description</h2>
        <p className="preserve-lines">{ticket.description}</p>
      </section>
      {cancelled&&<section role="status"><h2>Cancelled</h2><p>This cancellation is final. Cancelled on {formatDate(ticket.cancelledAtUtc!)}. The workflow status remains {ticket.statusName}.</p></section>}
      {canCancel&&<section><h2>Cancel ticket</h2>{!showCancel?<button type="button" onClick={()=>setShowCancel(true)}>Cancel ticket</button>:<form onSubmit={cancelTicket}><p>This permanently makes the ticket read-only. Its history and attachments will be preserved.</p><label>Optional cancellation reason<textarea maxLength={500} value={cancelReason} onChange={e=>setCancelReason(e.target.value)}/></label><button disabled={cancelling}>{cancelling?'Cancelling…':'Confirm cancellation'}</button><button type="button" disabled={cancelling} onClick={()=>setShowCancel(false)}>Keep ticket</button></form>}</section>}
      <div id="action-error" aria-live="polite"><ErrorSummary message={actionError} /></div>
      {!cancelled && support && assignmentAllowed && (
        <section>
          <h2>Assignment</h2>
          <p>Current assignee: {ticket.assignedToDisplayName ?? "Unassigned"}</p>
          {supportUsers.isLoading && <p role="status">Loading eligible support users…</p>}
          {supportUsers.error ? <div><p id="assignment-error" role="alert">{supportUsers.error}</p><button type="button" onClick={supportUsers.reload}>Retry support-user list</button></div> :
          <form onSubmit={assign} aria-describedby={actionError ? "action-error" : undefined}>
            <label>Eligible support user
              <select value={assigneeId} onChange={(e) => setAssigneeId(e.target.value)} disabled={supportUsers.isLoading || assigning}>
                <option value="">Select a support user</option>
                {supportUsers.users.map((user) => <option key={user.id} value={user.id}>{user.displayName} — {user.roles.join(", ")}</option>)}
              </select>
            </label>
            <label>Optional assignment note
              <textarea maxLength={1000} value={assignmentNote} onChange={(e) => setAssignmentNote(e.target.value)} disabled={assigning}/>
            </label>
            <button disabled={assigning || supportUsers.isLoading || !assigneeId || assigneeId === ticket.assignedToUserId}>
              {assigning ? "Assigning…" : ticket.assignedToUserId ? "Reassign ticket" : "Assign ticket"}
            </button>
            <span role="status" className="sr-only">{assigning ? "Assignment is being submitted." : ""}</span>
          </form>}
        </section>
      )}
      {!cancelled && support && (
        <section>
          <h2>Change status</h2>
          <form onSubmit={status}>
            <label>
              Status
              <select
                value={statusId || ticket.statusId}
                onChange={(e) => setStatusId(Number(e.target.value))}
              >
                {lookups.statuses.map((x) => (
                  <option key={x.id} value={x.id}>
                    {x.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Optional note
              <textarea
                maxLength={1000}
                value={note}
                onChange={(e) => setNote(e.target.value)}
              />
            </label>
            <button
              disabled={busy || !statusId || statusId === ticket.statusId}
            >
              Change status
            </button>
          </form>
        </section>
      )}
      <section>
        <h2>Comments</h2>
        {ticket.comments.length === 0 ? (
          <p>No comments yet.</p>
        ) : (
          <ul className="history">
            {ticket.comments.map((x) => (
              <li key={x.id}>
                <strong>{x.authorDisplayName}</strong> ·{" "}
                {formatDate(x.createdAtUtc)}{" "}
                {x.visibility === "Internal" && <mark>Internal</mark>}
                <p className="preserve-lines">{x.body}</p>
              </li>
            ))}
          </ul>
        )}
        {(!cancelled||support)&&<form onSubmit={comment}>
          <label>
            Add comment
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />
          </label>
          {support && (
            <label className="checkbox">
              <input
                type="checkbox"
                checked={internal}
                onChange={(e) => setInternal(e.target.checked)}
              />{" "}
              Internal comment (subject to backend visibility rules)
            </label>
          )}
          <button disabled={busy}>Add comment</button>
        </form>}
      </section>
      <section>
        <h2>Assignment history</h2>
        {ticket.assignmentHistory.length === 0 ? (
          <p>No assignment history.</p>
        ) : (
          <ul className="history">
            {ticket.assignmentHistory.map((x) => (
              <li key={x.id}>
                {x.assignedToDisplayName} · {formatDate(x.assignedAtUtc)}
                {x.assignedByDisplayName && ` by ${x.assignedByDisplayName}`}
                {x.endedAtUtc && ` · ended ${formatDate(x.endedAtUtc)}`}
                {x.reason && <p>{x.reason}</p>}
              </li>
            ))}
          </ul>
        )}
      </section>
      <section>
        <h2>Status history</h2>
        {ticket.statusHistory.length === 0 ? (
          <p>No status history.</p>
        ) : (
          <ul className="history">
            {ticket.statusHistory.map((x) => (
              <li key={x.id}>
                {x.fromStatusName ?? "Initial"} → {x.toStatusName} ·{" "}
                {formatDate(x.changedAtUtc)}
                {x.changedByDisplayName && ` by ${x.changedByDisplayName}`}
                {x.reason && <p>{x.reason}</p>}
              </li>
            ))}
          </ul>
        )}
      </section>
      <section>
        <h2>Attachments</h2>
        {!cancelled&&<form onSubmit={uploadAttachment}>
          <label>Upload attachment
            <input ref={fileInput} type="file" accept=".png,.jpg,.jpeg,.webp,.pdf,.txt,.docx,.xlsx" onChange={chooseAttachment} disabled={attachmentBusy!==undefined}/>
          </label>
          <button disabled={!attachmentFile||attachmentBusy!==undefined}>{attachmentBusy==='upload'?'Uploading…':'Upload attachment'}</button>
        </form>}
        {ticket.attachments.length === 0 ? (
          <p>No attachments.</p>
        ) : (
          <ul className="history">
            {ticket.attachments.map((x) => (
              <li key={x.id}>
                <strong>{x.originalFileName}</strong> ({x.contentType}, {formatSize(x.sizeBytes)}) · uploaded by {x.uploadedByDisplayName} on {formatDate(x.createdAtUtc)}
                <div><button type="button" disabled={attachmentBusy!==undefined} onClick={()=>downloadAttachment(x.id,x.originalFileName)}>Download</button>{(support||(ticket.createdByUserId===auth.user?.userId&&x.uploadedByUserId===auth.user?.userId))&&<button type="button" disabled={attachmentBusy!==undefined} onClick={()=>deleteAttachment(x.id)}>Delete</button>}</div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </section>
  );
}

function formatSize(bytes:number){if(bytes<1024)return `${bytes} bytes`;if(bytes<1024*1024)return `${(bytes/1024).toFixed(1)} KB`;return `${(bytes/(1024*1024)).toFixed(1)} MB`}
