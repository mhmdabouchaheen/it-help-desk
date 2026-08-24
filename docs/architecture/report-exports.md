# Report exports

Ticket reports and exports are restricted by the backend `SupportStaff` policy. `GET /api/reports/tickets/export/pdf` returns `application/pdf`; `GET /api/reports/tickets/export/excel` returns `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`. Both accept the same `TicketReportRequest` query contract as the interactive report and call `IReportService`; clients cannot submit calculated totals.

`IReportExportService` generates bounded in-memory files because the report contract contains aggregates capped to a 366-day trend. Files are returned directly and never persisted. UTC filenames use `ticket-report-yyyyMMdd-HHmmss.pdf` or `.xlsx`. Exports contain summary metrics, safe lookup names, breakdowns, trend points, and support-user ID/display-name workload data. Average Resolution Time uses `ResolvedAtUtc - CreatedAtUtc` for non-cancelled tickets with valid non-negative durations and is exported as `N/A` when no eligible ticket matches. Emails, Identity security fields, tokens, comments/internal notes, attachments, and storage keys are not queried or exported.

PDF generation uses PDFsharp-MigraDoc 6.2.4. The project uses table-oriented PDF output without browser processes or remote assets. PDFsharp’s official documentation states that all PDFsharp projects use the MIT License.

Excel generation uses ClosedXML 0.105.1, also distributed under the MIT License. It produces a macro-free `.xlsx` workbook with Summary, Status, Priority, Categories, Trend, and Agent Workload sheets.
