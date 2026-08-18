import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { getTicketsAsync } from "../api/tickets";
import { useLookups } from "../auth/useLookups";
import { EmptyState, ErrorSummary, LoadingIndicator } from "../components/Feedback";
import {
  formatDate,
  parseTicketQuery,
  ticketSortFields,
} from "../utils/tickets";
import type { PagedResponse, TicketSummaryResponse } from "../types/tickets";
import {CancelledBadge,TicketPriorityBadge,TicketStatusBadge} from "../components/Badges";
import { useRefreshOnFocus } from "../auth/useRefreshOnFocus";
export function TicketListPage() {
  const [params, setParams] = useSearchParams();
  const request = useMemo(() => parseTicketQuery(params), [params]);
  const lookups = useLookups();
  const [data, setData] = useState<PagedResponse<TicketSummaryResponse>>();
  const [error, setError] = useState<string>();
  const [search, setSearch] = useState(request.search ?? "");
  const controller = useRef<AbortController>(undefined);
  const sequence = useRef(0);
  const reload = useCallback(() => {
    controller.current?.abort();
    const current = ++sequence.current;
    const abort = new AbortController();
    controller.current = abort;
    setError(undefined);
    getTicketsAsync(request, abort.signal)
      .then((value) => { if (current === sequence.current && !abort.signal.aborted) setData(value) })
      .catch((e) => {
        if (current === sequence.current && !abort.signal.aborted && (e as Error).name !== "AbortError")
          setError("Tickets could not be loaded.");
      });
  }, [request]);
  useRefreshOnFocus(reload);
  useEffect(() => {
    // The initial request intentionally clears stale request errors before loading the current URL query.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    reload();
    const active = controller.current;
    return () => active?.abort();
  }, [reload]);
  function update(values: Record<string, string | undefined>) {
    const next = new URLSearchParams(params);
    for (const [k, v] of Object.entries(values)) {
      if (v) next.set(k, v);
      else next.delete(k);
    }
    if (!("page" in values)) next.set("page", "1");
    setParams(next);
  }
  function searchSubmit(e: FormEvent) {
    e.preventDefault();
    update({ search: search.trim() || undefined });
  }
  return (
    <section>
      <div className="page-heading">
        <h1>Tickets</h1>
        <Link className="button-link" to="/app/tickets/new">
          Create ticket
        </Link>
      </div>
      <form className="filters" onSubmit={searchSubmit}>
        <label>
          Search
          <input value={search} onChange={(e) => setSearch(e.target.value)} />
        </label>
        <label>
          Category
          <select
            disabled={lookups.loading}
            value={request.categoryId ?? ""}
            onChange={(e) => update({ category: e.target.value || undefined })}
          >
            <option value="">All</option>
            {lookups.categories.map((x) => (
              <option key={x.id} value={x.id}>
                {x.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Priority
          <select
            disabled={lookups.loading}
            value={request.priorityId ?? ""}
            onChange={(e) => update({ priority: e.target.value || undefined })}
          >
            <option value="">All</option>
            {lookups.priorities.map((x) => (
              <option key={x.id} value={x.id}>
                {x.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Status
          <select
            disabled={lookups.loading}
            value={request.statusId ?? ""}
            onChange={(e) => update({ status: e.target.value || undefined })}
          >
            <option value="">All</option>
            {lookups.statuses.map((x) => (
              <option key={x.id} value={x.id}>
                {x.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Created from
          <input
            type="datetime-local"
            onChange={(e) =>
              update({
                from: e.target.value
                  ? new Date(e.target.value).toISOString()
                  : undefined,
              })
            }
          />
        </label>
        <label>
          Created to
          <input
            type="datetime-local"
            onChange={(e) =>
              update({
                to: e.target.value
                  ? new Date(e.target.value).toISOString()
                  : undefined,
              })
            }
          />
        </label>
        <label>
          Sort
          <select
            value={request.sortBy}
            onChange={(e) => update({ sortBy: e.target.value })}
          >
            {ticketSortFields.map((x) => (
              <option key={x}>{x}</option>
            ))}
          </select>
        </label>
        <label>
          Direction
          <select
            value={request.sortDirection}
            onChange={(e) => update({ sortDirection: e.target.value })}
          >
            <option value="desc">Descending</option>
            <option value="asc">Ascending</option>
          </select>
        </label>
        <label>
          Page size
          <select
            value={request.pageSize}
            onChange={(e) => update({ pageSize: e.target.value })}
          >
            {[10, 20, 50, 100].map((x) => (
              <option key={x}>{x}</option>
            ))}
          </select>
        </label>
        <button>Apply search</button>
        <button type="button" onClick={() => setParams({})}>
          Reset
        </button>
      </form>
      <ErrorSummary message={error ?? lookups.error} />
      {!data && !error && <LoadingIndicator />}
      {data?.items.length === 0 && <EmptyState title="No tickets found" detail="Try adjusting or clearing the current filters."/>}
      {data && data.items.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Ticket</th>
                <th>Status</th>
                <th>Priority</th>
                <th>Category</th>
                <th>Creator</th>
                <th>Assignee</th>
                <th>Updated</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((t) => (
                <tr key={t.id}>
                  <td>
                    <Link to={`/app/tickets/${t.id}`}>
                      {t.ticketNumber}: {t.title}
                    </Link>
                  </td>
                  <td><TicketStatusBadge name={t.statusName}/> {t.cancelledAtUtc&&<CancelledBadge/>}</td>
                  <td><TicketPriorityBadge name={t.priorityName}/></td>
                  <td>{t.categoryName}</td>
                  <td>{t.createdByDisplayName}</td>
                  <td>{t.assignedToDisplayName ?? "Unassigned"}</td>
                  <td>{formatDate(t.updatedAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {data && (
        <nav className="pagination" aria-label="Ticket pages">
          <button
            disabled={!data.hasPreviousPage}
            onClick={() => update({ page: String(data.pageNumber - 1) })}
          >
            Previous
          </button>
          <span>
            Page {data.totalPages ? data.pageNumber : 0} of {data.totalPages} ·{" "}
            {data.totalCount} results
          </span>
          <button
            disabled={!data.hasNextPage}
            onClick={() => update({ page: String(data.pageNumber + 1) })}
          >
            Next
          </button>
        </nav>
      )}
    </section>
  );
}
