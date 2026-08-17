# Audit trail architecture

The audit trail uses the existing `ActivityLog` entity and `ActivityLogs` table. No schema change or migration is required. Records contain the nullable actor ID, a stable action and entity type, a textual entity identifier, a UTC occurrence time, and optional JSON metadata.

## Safety and metadata

`ActivityLogService` is the only application writer. It accepts string maps, rejects unknown keys, bounds entry counts and values, and serializes with `System.Text.Json`. Each action has an explicit allowlist. Ticket changes contain identifiers or changed-field names only. Comment and internal-note bodies, cancellation reasons, filenames, attachment contents, hashes, storage keys and paths, emails, passwords, password hashes, JWTs, access and refresh tokens, token hashes, secrets, request DTOs, and exception payloads are excluded.

## Persistence

Business services commit their primary operation before attempting an activity write. Audit persistence is best effort for this project: an audit failure is logged with safe identifiers and does not roll back a successful ticket, attachment, notification, registration, or login operation. A compliance-grade guarantee would require a transactional outbox design.

## Read access

`GET /api/activity-logs` is protected by `AppPolicies.SupportStaff`, so only Admin and IT Support Agent roles receive the organization-wide feed. There is no client write endpoint. `GET /api/tickets/{ticketId}/activity` first invokes the existing ticket detail access path, preserving its not-found behavior and ownership/support visibility rules, and then returns newest-first activity for that ticket.
