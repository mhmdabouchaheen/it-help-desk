import { useState, type FormEvent } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useLookups } from "../auth/useLookups";
import { useReports } from "../auth/useReports";
import { useSupportUsers } from "../auth/useSupportUsers";
import { EmptyState, LoadingIndicator } from "../components/Feedback";
import type { TicketReportRequest } from "../types/reports";
import { FileDown, Sheet } from "lucide-react";
import {
  exportTicketReportExcelAsync,
  exportTicketReportPdfAsync,
} from "../api/reports";
import { downloadBlob } from "../utils/download";

const date = new Intl.DateTimeFormat(undefined, {
  month: "short",
  day: "numeric",
  timeZone: "UTC",
});
function utc(value: string, end = false) {
  return value ? `${value}T${end ? "23:59:59" : "00:00:00"}Z` : undefined;
}
export function ReportsPage() {
  const lookups = useLookups();
  const agents = useSupportUsers(true);
  const [draft, setDraft] = useState({
    from: "",
    to: "",
    categoryId: "",
    priorityId: "",
    statusId: "",
    assignedToUserId: "",
  });
  const [filters, setFilters] = useState<TicketReportRequest>({});
  const [exporting, setExporting] = useState<"pdf" | "excel">();
  const [exportError, setExportError] = useState(false);
  const report = useReports(filters);
  async function exportReport(format: "pdf" | "excel") {
    if (exporting) return;
    setExporting(format);
    setExportError(false);
    try {
      const result =
        format === "pdf"
          ? await exportTicketReportPdfAsync(filters)
          : await exportTicketReportExcelAsync(filters);
      downloadBlob(result.blob, result.fileName);
    } catch {
      setExportError(true);
    } finally {
      setExporting(undefined);
    }
  }
  function apply(event: FormEvent) {
    event.preventDefault();
    setFilters({
      fromUtc: utc(draft.from),
      toUtc: utc(draft.to, true),
      categoryId: draft.categoryId ? Number(draft.categoryId) : undefined,
      priorityId: draft.priorityId ? Number(draft.priorityId) : undefined,
      statusId: draft.statusId ? Number(draft.statusId) : undefined,
      assignedToUserId: draft.assignedToUserId || undefined,
    });
  }
  function reset() {
    setDraft({
      from: "",
      to: "",
      categoryId: "",
      priorityId: "",
      statusId: "",
      assignedToUserId: "",
    });
    setFilters({});
  }
  return (
    <section className="reports" aria-labelledby="reports-heading">
      <div className="page-heading">
        <div>
          <h1 id="reports-heading">Reports</h1>
          <p>
            Analyze ticket volume, workflow distribution, trends, and current
            support workload.
          </p>
        </div>
        <div className="actions report-export-actions">
          <button
            type="button"
            disabled={exporting !== undefined || report.loading}
            onClick={() => void exportReport("pdf")}
          >
            <FileDown aria-hidden="true" />
            {exporting === "pdf" ? "Exporting PDF…" : "Export PDF"}
          </button>
          <button
            type="button"
            disabled={exporting !== undefined || report.loading}
            onClick={() => void exportReport("excel")}
          >
            <Sheet aria-hidden="true" />
            {exporting === "excel" ? "Exporting Excel…" : "Export Excel"}
          </button>
        </div>
      </div>
      {exportError && (
        <div className="error-summary" role="alert" aria-live="assertive">
          The report export could not be downloaded. Please try again.
        </div>
      )}
      <form className="filters report-filters" onSubmit={apply}>
        <label>
          From
          <input
            type="date"
            value={draft.from}
            onChange={(e) => setDraft((x) => ({ ...x, from: e.target.value }))}
          />
        </label>
        <label>
          To
          <input
            type="date"
            value={draft.to}
            onChange={(e) => setDraft((x) => ({ ...x, to: e.target.value }))}
          />
        </label>
        <label>
          Category
          <select
            value={draft.categoryId}
            onChange={(e) =>
              setDraft((x) => ({ ...x, categoryId: e.target.value }))
            }
          >
            <option value="">All categories</option>
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
            value={draft.priorityId}
            onChange={(e) =>
              setDraft((x) => ({ ...x, priorityId: e.target.value }))
            }
          >
            <option value="">All priorities</option>
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
            value={draft.statusId}
            onChange={(e) =>
              setDraft((x) => ({ ...x, statusId: e.target.value }))
            }
          >
            <option value="">All statuses</option>
            {lookups.statuses.map((x) => (
              <option key={x.id} value={x.id}>
                {x.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Agent
          <select
            value={draft.assignedToUserId}
            onChange={(e) =>
              setDraft((x) => ({ ...x, assignedToUserId: e.target.value }))
            }
          >
            <option value="">All agents</option>
            {agents.users.map((x) => (
              <option key={x.id} value={x.id}>
                {x.displayName}
              </option>
            ))}
          </select>
        </label>
        <div className="actions">
          <button type="submit">Apply filters</button>
          <button type="button" onClick={reset}>
            Reset filters
          </button>
        </div>
      </form>
      {report.loading ? (
        <LoadingIndicator label="Loading report…" />
      ) : report.error ? (
        <div className="error-summary" role="alert">
          <p>Report data could not be loaded.</p>
          <button type="button" onClick={report.reload}>
            Retry
          </button>
        </div>
      ) : (
        report.data && <ReportContent data={report.data} />
      )}
    </section>
  );
}

function ReportContent({
  data,
}: {
  data: NonNullable<ReturnType<typeof useReports>["data"]>;
}) {
  const s = data.summary;
  if (s.totalTickets === 0)
    return (
      <EmptyState
        title="No report data"
        detail="No tickets match the selected filters."
      />
    );
  const kpis = [
    ["Total tickets", s.totalTickets],
    ["Open / active", s.openTickets],
    ["Terminal / closed", s.terminalTickets],
    ["Cancelled", s.cancelledTickets],
    ["Assigned", s.assignedTickets],
    ["Unassigned", s.unassignedTickets],
  ];
  return (
    <>
      <section aria-labelledby="report-summary">
        <h2 id="report-summary">Ticket summary</h2>
        <dl className="kpi-grid report-kpis">
          {kpis.map(([label, value]) => (
            <div key={label}>
              <dt>{label}</dt>
              <dd>{value}</dd>
            </div>
          ))}
        </dl>
      </section>
      <div className="chart-grid">
        <Breakdown title="Tickets by status" data={data.statusBreakdown} />
        <Breakdown title="Tickets by priority" data={data.priorityBreakdown} />
        <Breakdown title="Tickets by category" data={data.categoryBreakdown} />
        <section className="chart-card wide">
          <h2>Created and closed trend</h2>
          <div className="chart">
            <ResponsiveContainer>
              <LineChart
                data={data.trend.map((x) => ({
                  ...x,
                  label: date.format(new Date(x.periodStartUtc)),
                }))}
              >
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" />
                <YAxis allowDecimals={false} />
                <Tooltip />
                <Legend />
                <Line dataKey="createdCount" name="Created" stroke="#315bd6" />
                <Line dataKey="closedCount" name="Closed" stroke="#23816f" />
              </LineChart>
            </ResponsiveContainer>
          </div>
          <ul
            className="chart-summary"
            aria-label="Created and closed trend text summary"
          >
            {data.trend.map((x) => (
              <li key={x.periodStartUtc}>
                <span>{date.format(new Date(x.periodStartUtc))}</span>
                <strong>
                  {x.createdCount} created, {x.closedCount} closed
                </strong>
              </li>
            ))}
          </ul>
        </section>
        <section className="chart-card wide">
          <h2>Agent workload</h2>
          <div className="chart">
            <ResponsiveContainer>
              <BarChart layout="vertical" data={data.agentWorkload}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis type="number" allowDecimals={false} />
                <YAxis type="category" dataKey="displayName" width={120} />
                <Tooltip />
                <Bar
                  dataKey="activeTicketCount"
                  name="Active tickets"
                  fill="#315bd6"
                />
              </BarChart>
            </ResponsiveContainer>
          </div>
          <ul
            className="chart-summary"
            aria-label="Agent workload text summary"
          >
            {data.agentWorkload.map((x) => (
              <li key={x.userId}>
                <span>{x.displayName}</span>
                <strong>{x.activeTicketCount}</strong>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </>
  );
}
function Breakdown({
  title,
  data,
}: {
  title: string;
  data: { id: number; name: string; count: number }[];
}) {
  return (
    <section className="chart-card">
      <h2>{title}</h2>
      <div className="chart">
        <ResponsiveContainer>
          <BarChart data={data}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="name" />
            <YAxis allowDecimals={false} />
            <Tooltip />
            <Bar dataKey="count" name="Tickets" fill="#315bd6" />
          </BarChart>
        </ResponsiveContainer>
      </div>
      <ul className="chart-summary" aria-label={`${title} text summary`}>
        {data.map((x) => (
          <li key={x.id}>
            <span>{x.name}</span>
            <strong>{x.count}</strong>
          </li>
        ))}
      </ul>
    </section>
  );
}
