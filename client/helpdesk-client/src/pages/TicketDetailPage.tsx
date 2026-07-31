import { useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { addTicketCommentAsync, changeTicketStatusAsync } from "../api/tickets";
import { useAuth } from "../auth/AuthProvider";
import { RoleGroups } from "../auth/roles";
import { useLookups } from "../auth/useLookups";
import { useTicketDetail } from "../auth/useTicketDetail";
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
  const [statusId, setStatusId] = useState(0);
  const [note, setNote] = useState("");
  const [content, setContent] = useState("");
  const [internal, setInternal] = useState(false);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string>();
  if (loading) return <LoadingIndicator />;
  if (error || !ticket)
    return (
      <section>
        <ErrorSummary message={error} />
        <button onClick={reload}>Retry</button>
      </section>
    );
  const currentTicket = ticket;
  const canEdit = support || currentTicket.createdByUserId === auth.user?.userId;
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
      <ErrorSummary message={actionError} />
      {support && (
        <section>
          <h2>Assignment</h2>
          <button disabled>Assign ticket</button>
          <p>
            A safe support-user directory is not available yet. Assignment is
            disabled.
          </p>
        </section>
      )}
      {support && (
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
        <form onSubmit={comment}>
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
        </form>
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
        {ticket.attachments.length === 0 ? (
          <p>No attachment metadata.</p>
        ) : (
          <ul>
            {ticket.attachments.map((x) => (
              <li key={x.id}>
                {x.originalFileName} ({x.contentType}, {x.sizeBytes} bytes) —
                upload and download are not available yet.
              </li>
            ))}
          </ul>
        )}
      </section>
    </section>
  );
}
