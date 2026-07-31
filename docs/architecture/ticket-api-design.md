# Ticket API design

This phase implements the ticket application service boundary. It adds no
ticket controller or HTTP endpoint.

## Requests and validation

`CreateTicketRequest` and `UpdateTicketRequest` accept only `Title`,
`Description`, `CategoryId`, and `PriorityId`. Titles are required and limited
to the model's 250 characters. Descriptions are required; the current EF model
does not configure a maximum. Category and priority identifiers are positive
`short` values. Creation never accepts a ticket/reference number, creator,
assignee, status, role, or audit timestamp.

`AssignTicketRequest` accepts a non-empty `AssignedToUserId` and an optional
500-character `Note`. `ChangeTicketStatusRequest` accepts a positive `short`
`StatusId` and an optional 1,000-character `Note`. The service trims and stores
these notes in the model's `Reason` fields. `AddTicketCommentRequest` accepts required
`Content` and `IsInternal`; the service maps content to `Body` and
translate the flag to the model's Public/Internal `Visibility`.
Comment bodies have no configured model maximum.

DTO validation handles shape, required values, lengths, pagination bounds, and
sort allowlists. Existence checks, time-range consistency, state transitions,
assignment eligibility, and access rules are future business validation.

## Lists and pagination

`TicketListRequest` defaults to page 1 and page size 20; page size is limited to
100. Optional filters are search text (maximum 200), category, priority,
status, creator, assignee, and inclusive UTC creation bounds.

Allowed sort fields are `CreatedAtUtc`, `UpdatedAtUtc`, `TicketNumber`,
`Priority`, `Status`, and `Title`. Directions are `asc` and `desc`.
Allowlist matching is ordinal and case-insensitive.

`PagedResponse<T>` carries items, page number, page size, total count, total
pages, and previous/next-page flags. A future application service populates
the derived metadata.

## Responses

`TicketSummaryResponse` exposes ticket identity and number, title, lookup IDs
and names, creator and assignee display information, and audit times.
`TicketDetailResponse` adds description, resolution/closure/cancellation times,
comments, attachment metadata, assignment history, and status history.

Nested responses follow the real model names: comment `Body` and `Visibility`;
assignment `Reason` and `EndedAtUtc`; and status transition `FromStatusId`,
`ToStatusId`, and `Reason`. Attachment responses deliberately omit storage
provider, storage key, hash, paths, and bytes. Responses prefer display names
and expose no email or Identity security fields. No EF entity or navigation
object crosses the application boundary.

Lookup contracts use the actual `short` keys. Categories expose name,
description, sort order, and active state. Priorities expose name, description,
rank, and active state. Statuses expose name, description, sort order, terminal
state, and active state.

## Application interfaces

`TicketAccessContext` contains only the validated caller's `Guid` user ID and
read-only role collection. A future controller will construct it exclusively
from validated JWT claims; it has no HTTP or `ClaimsPrincipal` dependency.

`ITicketService` defines create, paged list, get-by-ID, basic update, assign,
change-status, and add-comment operations. `ITicketLookupService` defines
category, priority, and status lookup operations. Neither interface exposes EF
entities, `IQueryable`, HTTP objects, Identity managers, or controller types.

## Access model

- Employees may create tickets, view their own tickets, update their own
  tickets when business state permits, and comment on accessible tickets.
- IT Support Agents may view support-visible tickets, assign where permitted,
  change statuses, and add support comments.
- Managers may view tickets allowed by future management rules. Management
  access does not imply support-agent privileges.
- Admins have broad administrative access.

Future services or resource authorization handlers must enforce ownership,
assignment, visibility, and ticket-state rules. Role strings remain centralized
in `AppRoles` and are not accepted from ticket requests.

## Implemented core services

`TicketService` now implements creation, paged listing, retrieval by ID, and
basic-detail updates. It reads caller identity and roles only from
`TicketAccessContext`. An empty user ID or an access context without at least
one exact `AppRoles` value is denied.

Admin and IT Support Agent roles grant visibility and basic-update access to
all tickets. Employee and Manager roles are restricted to tickets they created.
Privileges are additive, so either support role overrides narrower roles;
Manager alone does not grant support privileges.

Creation verifies an active creator, category, priority, and the exact active
seeded `Open` status. The creator is always the access-context user. Input text
is trimmed, creation and update times come from `TimeProvider`, and no
assignment or initial history entry is created.

Ticket numbers use `TKT-yyyyMMdd-HHmmss-XXXXXXXX`, combining injected UTC time with
cryptographically secure random bytes. The value is at most 30 characters and
contains no user data. `IX_Tickets_ReferenceNumber` is the model's unique index.
Creation retries at most three total attempts only when PostgreSQL reports
unique violation `23505` for that exact index; unrelated update failures are
not retried and database details do not cross the service boundary.

Paged listing applies role visibility before optional search, category,
priority, status, creator, current-assignee, and UTC creation-range filters.
PostgreSQL search uses escaped `ILIKE` patterns over reference number and title.
The isolated SQLite test path uses normalized containment because SQLite cannot
translate Npgsql `ILIKE`. Counts occur after filtering and before paging.
Empty results have zero total pages and false navigation flags.

Sorting is explicit rather than raw SQL. Priority uses `Rank`, status uses
`SortOrder`, and every supported ordering uses ticket ID as a deterministic
secondary key. Current assignment summary data comes from the ticket's direct
`AssignedToUserId`; assignment history remains projected from
`TicketAssignments`.

Detailed reads use no-tracking projections and ordered, bounded queries for
comments, safe attachment metadata, assignment history, and status history.
Non-support callers receive `TicketNotFoundException` for inaccessible tickets,
hiding whether another user's ticket exists. `TicketAccessDeniedException` is
reserved for an invalid caller context.

Basic updates change only title, description, category, priority, and—when a
value actually changes—`UpdatedAtUtc`. Employee and Manager updates are blocked
when the current status has `IsTerminal`; Admin and IT Support Agent updates are
not blocked by terminal state. Assignment and status cannot be changed through
this operation.

## Assignment, status changes, and comments

Only Admin and IT Support Agent callers may assign. The target must be active
and hold Admin or IT Support Agent in the authoritative Identity
`UserRoles`/`Roles` tables. Missing, inactive, and ineligible targets share a
generic error. Terminal tickets cannot be assigned. Actor IDs come from
`TicketAccessContext`, notes are trimmed into `Reason`, and timestamps come from
`TimeProvider`. Reassignment ends the active record and appends a replacement
without deleting history. Assigning the current user is idempotent. The direct
assignee and history are saved atomically. The existing filtered unique index
enforces one active assignment; only a PostgreSQL violation of that exact
constraint becomes a ticket-state conflict.

Only Admin and IT Support Agent callers may change status, and the target must
be an active status resolved by ID. Any different active status is reachable
from a non-terminal status. IT Support Agent cannot leave a terminal status;
Admin may reopen it. Same-status requests are idempotent. Actual changes update
the ticket and append immutable status history in one save. Entering or leaving
a named terminal status sets or clears its corresponding `ResolvedAtUtc`,
`ClosedAtUtc`, or `CancelledAtUtc` value while preserving unrelated historical
timestamps. In the current seed data only `Closed` is terminal.

Public comments may be added by any caller with ticket access. Employee and
Manager access is creator-only, and another creator's ticket is reported as not
found. Internal comments use the exact `Internal` visibility and are restricted
to Admin and IT Support Agent; public comments use `Public`. Non-support users
cannot comment on terminal tickets, while support users can. The active author
is resolved from the access-context user, content is trimmed, and comment
creation plus ticket `UpdatedAtUtc` use one save.

Notes and comment bodies are not logged. Logs contain identifiers only.
Responses are refreshed DTO projections with deterministic history ordering and
expose neither email nor Identity security fields.

## HTTP API

All ticket and ticket-lookup routes require a validated bearer token. The
stateless `TicketAccessContextFactory` reads the actor only from the validated
JWT subject (`sub`, including its framework-mapped name-identifier form) and
reads roles only from role claims. It preserves exact role text and removes
duplicates. Missing or malformed authenticated identity data produces a safe
401 `invalid_authenticated_principal` response. Headers, query strings,
cookies, and request bodies cannot supply actor or role data.

| Method | Route | Request | Success | Additional authorization |
|---|---|---|---|---|
| POST | `/api/tickets` | `CreateTicketRequest` | 201 `TicketDetailResponse` | Authenticated |
| GET | `/api/tickets` | `TicketListRequest` query | 200 paged summaries | Authenticated |
| GET | `/api/tickets/{ticketId}` | — | 200 `TicketDetailResponse` | Authenticated |
| PUT | `/api/tickets/{ticketId}` | `UpdateTicketRequest` | 200 `TicketDetailResponse` | Authenticated |
| POST | `/api/tickets/{ticketId}/assignment` | `AssignTicketRequest` | 200 `TicketDetailResponse` | `SupportStaff` policy |
| POST | `/api/tickets/{ticketId}/status` | `ChangeTicketStatusRequest` | 200 `TicketDetailResponse` | `SupportStaff` policy |
| POST | `/api/tickets/{ticketId}/comments` | `AddTicketCommentRequest` | 201 `TicketCommentResponse` | Authenticated |
| GET | `/api/ticket-lookups/categories` | — | 200 category list | Authenticated |
| GET | `/api/ticket-lookups/priorities` | — | 200 priority list | Authenticated |
| GET | `/api/ticket-lookups/statuses` | — | 200 status list | Authenticated |

Controllers perform binding and delegation only. They contain no EF queries,
Identity lookup, ownership rule, transition rule, or existence check. Resource
authorization and existence hiding remain service responsibilities. Assignment
and status routes additionally use the coarse `SupportStaff` policy, while the
service remains authoritative for business rules. Lookup controllers delegate
directly to `ITicketLookupService`.

Controlled ticket failures use ProblemDetails codes `ticket_not_found`,
`category_not_found`, `priority_not_found`, `status_not_found`,
`assignment_target_not_found`, `ticket_access_denied`,
`ticket_validation_failed`, and `ticket_state_conflict`. Bearer challenge and
forbidden responses retain `authentication_required` and `access_forbidden`.
Attachment upload and download remain deferred.

`TicketLookupService` returns active categories ordered by sort order/name,
active priorities ordered by rank/name, and active statuses ordered by sort
order/name. All lookup and ticket read queries are no-tracking and return DTOs,
never EF entities.
