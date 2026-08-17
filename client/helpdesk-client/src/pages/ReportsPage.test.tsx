import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ReportsPage } from "./ReportsPage";
const reload = vi.fn();
let report: Record<string, unknown>;
const { exportPdf, exportExcel, downloadBlob } = vi.hoisted(() => ({
  exportPdf: vi.fn(),
  exportExcel: vi.fn(),
  downloadBlob: vi.fn(),
}));
vi.mock("../auth/useReports", () => ({ useReports: () => report }));
vi.mock("../api/reports", () => ({
  exportTicketReportPdfAsync: exportPdf,
  exportTicketReportExcelAsync: exportExcel,
}));
vi.mock("../utils/download", () => ({ downloadBlob }));
vi.mock("../auth/useLookups", () => ({
  useLookups: () => ({
    categories: [],
    priorities: [],
    statuses: [],
    loading: false,
  }),
}));
vi.mock("../auth/useSupportUsers", () => ({
  useSupportUsers: () => ({ users: [], isLoading: false }),
}));
vi.mock("recharts", () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
  BarChart: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
  CartesianGrid: () => null,
  Legend: () => null,
  Tooltip: () => null,
  XAxis: () => null,
  YAxis: () => null,
  Bar: () => null,
  Line: () => null,
}));
const data = {
  summary: {
    totalTickets: 4,
    openTickets: 2,
    terminalTickets: 2,
    cancelledTickets: 1,
    assignedTickets: 3,
    unassignedTickets: 1,
  },
  statusBreakdown: [{ id: 1, name: "Open", count: 2 }],
  priorityBreakdown: [{ id: 1, name: "Low", count: 4 }],
  categoryBreakdown: [{ id: 1, name: "Hardware", count: 4 }],
  trend: [
    { periodStartUtc: "2026-08-01T00:00:00Z", createdCount: 2, closedCount: 1 },
  ],
  agentWorkload: [
    { userId: "a", displayName: "Agent One", activeTicketCount: 2 },
  ],
};
describe("ReportsPage", () => {
  beforeEach(() => {
    reload.mockReset();
    exportPdf.mockReset().mockResolvedValue({blob:new Blob(),fileName:"report.pdf"});
    exportExcel.mockReset().mockResolvedValue({blob:new Blob(),fileName:"report.xlsx"});
    downloadBlob.mockReset();
    report = { data: undefined, loading: false, error: undefined, reload };
  });
  it("renders loading and safe retry states", async () => {
    report.loading = true;
    const loading = render(<ReportsPage />);
    expect(screen.getByRole("status")).toHaveTextContent("Loading report");
    loading.unmount();
    report.loading = false;
    report.error = new Error("secret");
    render(<ReportsPage />);
    expect(screen.getByRole("alert")).not.toHaveTextContent("secret");
    await userEvent.click(screen.getByRole("button", { name: "Retry" }));
    expect(reload).toHaveBeenCalled();
  });
  it("renders an honest empty result", () => {
    report.data = { ...data, summary: { ...data.summary, totalTickets: 0 } };
    render(<ReportsPage />);
    expect(screen.getByText("No report data")).toBeInTheDocument();
  });
  it("renders KPIs, chart headings, and accessible alternatives", () => {
    report.data = data;
    render(<ReportsPage />);
    expect(screen.getByText("Total tickets")).toBeInTheDocument();
    expect(screen.getAllByText("4").length).toBeGreaterThan(0);
    expect(
      screen.getByRole("heading", { name: "Tickets by status" }),
    ).toBeInTheDocument();
    expect(
      screen.getByLabelText("Agent workload text summary"),
    ).toHaveTextContent("Agent One");
  });
  it("exports only the currently applied filters and downloads the result", async()=>{
    report.data=data;render(<ReportsPage/>);const user=userEvent.setup();
    await user.type(screen.getByLabelText("From"),"2026-08-01");
    await user.click(screen.getByRole("button",{name:"Apply filters"}));
    await user.clear(screen.getByLabelText("From"));
    await user.type(screen.getByLabelText("From"),"2026-09-01");
    await user.click(screen.getByRole("button",{name:"Export PDF"}));
    expect(exportPdf).toHaveBeenCalledWith({fromUtc:"2026-08-01T00:00:00Z",toUtc:undefined,categoryId:undefined,priorityId:undefined,statusId:undefined,assignedToUserId:undefined});
    expect(downloadBlob).toHaveBeenCalledWith(expect.any(Blob),"report.pdf");
  });
  it("prevents duplicate exports and exposes a safe failure",async()=>{
    report.data=data;let reject:(reason?:unknown)=>void=()=>undefined;exportExcel.mockReturnValue(new Promise((_,r)=>{reject=r}));render(<ReportsPage/>);const button=screen.getByRole("button",{name:"Export Excel"});await userEvent.click(button);await userEvent.click(button);expect(exportExcel).toHaveBeenCalledOnce();reject(new Error("secret"));expect(await screen.findByRole("alert")).toHaveTextContent("could not be downloaded");expect(screen.getByRole("alert")).not.toHaveTextContent("secret");
  });
});
