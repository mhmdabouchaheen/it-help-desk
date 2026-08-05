# Dashboard analytics

`GET /api/dashboard` is authenticated and accepts no query parameters. The controller creates a `TicketAccessContext` exclusively from validated JWT claims. Admin and IT Support Agent roles see all tickets; Employee and Manager callers without a support role see only tickets they created. A support role wins in a multi-role context. Unknown roles and empty user IDs use the existing access-denied behavior.

The response contains summary KPIs, active lookup-backed status/priority/category breakdowns, six chronological UTC months, and the eight most recently updated visible tickets. Active lookups remain present with a zero count. Statuses use `SortOrder`, priorities use `Rank` through the neutral `DisplayOrder` response field, and categories use `SortOrder`. Inactive lookups are omitted even if historical tickets reference them.

Total includes cancelled tickets. Cancellation is determined only by `CancelledAtUtc` and is a separate KPI/trend series; no Cancelled status exists. A cancelled ticket remains counted in its actual current status bucket. Assigned/unassigned use `AssignedToUserId`, Critical uses the active lookup named exactly `Critical`, and current-month KPIs use the first day of the UTC month. Missing named lookups produce zero and a safe warning.

The service applies visibility before aggregation, uses no tracking, groups counts in the database, projects recent rows directly, and reads only the three bounded six-month timestamp columns needed to fill portable zero-month trend points. It does not load ticket entities, use raw SQL, or expose email, descriptions, comments, attachments, histories, or EF objects.
