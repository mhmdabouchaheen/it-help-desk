# Frontend architecture

## Final UI system

The portfolio UI uses centralized semantic tokens in `src/index.css` and reusable application, component, and dashboard treatments in `App.css` and `dashboard.css`. It uses a system font stack, soft surfaces, subtle borders/shadows, consistent radii, shared form/button states, readable semantic badges, announced loading, safe error alerts, and real empty-state copy.

The authenticated shell has a fixed desktop sidebar, sticky utility header, notification count, quick-create action, and bottom account area. Below 850px it becomes a labelled drawer that closes by overlay, navigation, close button, or Escape. Responsive filters, cards, charts, actions, summaries, and table-to-card layouts prevent horizontal page overflow down to 320px.

`lucide-react` supplies consistent decorative/navigation icons; icons do not replace accessible labels. Known status and priority names map to semantic badge tones with neutral fallbacks and never depend on lookup IDs. All untrusted message, comment, and ticket content remains normal React text.

Authenticated feature routes are loaded with `React.lazy` and an accessible `Suspense` fallback. This isolates the Recharts-heavy dashboard from the initial application chunk while preserving route guards and URLs. Recharts retains nearby list/table alternatives, restrained colors, responsive containers, and textual headings.

The detailed design rules and responsive/accessibility decisions are recorded in `docs/frontend/ui-style-guide.md`.

## Notifications

`/app/notifications` is protected by the authenticated application layout. The layout loads the server unread count and displays an accessible badge, while the center provides all/unread filtering, pagination, retry, individual and bulk read operations, and ticket links. State is shared in memory for the authenticated layout and is discarded on logout/unmount; it is never written to local or session storage.

Notification messages render as plain React text with no HTML injection. Requests use the shared bearer client and never send a user or recipient identifier. Read operations reload authoritative server state after success.

The authenticated layout owns one `@microsoft/signalr` connection to `/hubs/notifications`. It uses automatic reconnect, reads the latest access token from the existing memory-only token store whenever requested, and stops/removes its handler on logout or unmount. It never supplies a refresh token or creates a second refresh flow.

`NotificationCreated` is validated and used only to reload the REST page and unread count. It never fabricates or directly renders a notification, and superseded reloads are aborted. Connection failure leaves manual reload and all REST behavior available. There is no browser push, Notification API, service worker, or persistent event storage. The hub URL derives from `VITE_API_BASE_URL`; production uses HTTPS and a reverse proxy configured for WebSocket upgrades and safe handshake logging.

## Dashboard

`/app/home` remains the authenticated landing route and its navigation label is Dashboard. It loads `GET /api/dashboard` with the shared bearer client, sends no identity or role filters, aborts on unmount, ignores stale responses, and does not persist analytics. Loading is announced; 403 and generic errors are safe and retryable.

The page renders backend-authoritative KPI cards, a Recharts status pie, priority and horizontal category bars, a six-month Created/Closed/Cancelled line chart, and eight safe recent-ticket links. Every visualization has a nearby list or table alternative. UTC month labels are formatted without timezone shifting. Cancelled remains separate from real statuses. Frontend roles affect explanatory copy only: support users see organization-wide wording and other recognized roles see owned-ticket wording. No counts or chart data are fabricated or recomputed from ticket lists.

The existing `client/helpdesk-client` Vite application is the web client. It
uses React 19, strict TypeScript, React Router, native `fetch`, Vitest, jsdom,
and React Testing Library. Authentication is the only implemented feature;
ticket pages and ticket API calls are deferred.

## Structure and routes

`src/api` contains the base client and auth operations, `src/auth` owns the
session and presentation-only role helpers, `src/routes` contains guards,
`src/layouts` contains the authenticated shell, `src/pages` contains login,
registration, home, and not-found pages, and `src/components` contains reusable
feedback UI.

Public routes are `/login` and `/register`. `/app` and `/app/home` require an
authenticated in-memory session. An internal intended destination is restored
after login; protocol-relative and external destinations are rejected. The
authenticated layout currently links only to Home and provides identity, role,
and logout controls.

## Authentication and API lifecycle

Login and registration return the backend's flat `AuthResponse`. Both tokens
are held only in module memory; passwords are never stored. No token is written
to localStorage, sessionStorage, IndexedDB, a URL, logs, or rendered UI. A full
page refresh therefore requires sign-in again because the backend currently
returns refresh tokens in JSON rather than an HttpOnly cookie.

The native-fetch client adds the bearer access token, handles JSON and 204
responses, forwards abort signals, and converts ProblemDetails into a safe
`ApiProblemError`. It performs no navigation or logging. On an eligible 401,
one shared refresh promise rotates both tokens and each failed request retries
at most once. Refresh failure clears the session and never recursively refreshes
the refresh endpoint. Logout attempts server revocation and always clears the
local session.

Frontend role constants exactly match Admin, IT Support Agent, Employee, and
Manager. SupportStaff and Management groups are navigation conveniences only;
backend authorization policies and services remain authoritative.

## Configuration and development

The authenticated `/app/activity` route presents a read-only, paginated activity feed and is shown in navigation only to Admin and IT Support Agent users. Backend policy remains authoritative. Ticket details include a newest-first audit timeline retrieved through the ticket-scoped endpoint. Both views render only plain text from the backend's safe metadata map and never use HTML injection.

Copy `.env.example` to an ignored local `.env` and set the public client
configuration `VITE_API_BASE_URL`. Vite variables are shipped to browsers and
must never contain secrets. The example uses `https://localhost:7233`, matching
the backend HTTPS launch profile. The backend development CORS policy permits
only the configured `http://localhost:5173` origin and standard requested
headers/methods; credentials are not enabled.

```text
npm install
npm run dev
npm run lint
npm run test -- --run
npm run build
```

Normal React text rendering is used throughout; there is no raw HTML injection.
Forms use labels, associated field errors, invalid-state attributes, live error
summaries, semantic navigation/layout elements, keyboard controls, and visible
focus indicators.

## Ticket management

| Page | Backend operations |
|---|---|
| `/app/tickets` | Paged ticket GET and authenticated lookup routes |
| `/app/tickets/new` | Ticket POST |
| `/app/tickets/:ticketId` | Detail GET, status change, and comment creation |
| `/app/tickets/:ticketId/edit` | Detail GET and basic-detail PUT |

`src/types/tickets.ts` mirrors string GUIDs, numeric lookup IDs, nullable
fields, and ISO timestamp strings. `src/api/tickets.ts` is the ticket transport
layer and uses the shared authenticated client. Its URLSearchParams serializer
trims search and omits empty optional values.

The list stores paging, search, lookup filters, UTC date boundaries, sorting,
and direction in the URL. Malformed pagination and sort values fall back to
backend defaults. Filter and sort changes reset the page; pagination uses only
server metadata. AbortController prevents superseded list/detail requests from
updating the screen.

Lookup categories, priorities, and statuses use session-memory caching and a
shared in-flight promise. No IDs or fallback values are fabricated. Create and
edit forms write only title, description, category, and priority. Detail pages
show comments, assignment/status history, and safe attachment metadata. No
attachment action or storage information is exposed.

Admin and IT Support Agent roles reveal status controls, internal comments,
and the assignment section. Transition rules remain backend decisions. There
is no safe support-user directory endpoint, so assignment is disabled and no
free-text user-ID input or fabricated user list exists. An eligible-support-user
directory is required before assignment can become interactive.

Comments use React text rendering and `white-space: pre-wrap`; HTML and Markdown
are not interpreted. Status mutations use returned detail data, while comment
creation reloads detail to preserve server ordering. Dates display locally via
`Intl.DateTimeFormat`, while API and query timestamps remain ISO strings.

Role checks control presentation only. Backend authorization and ProblemDetails
remain authoritative. No fake ticket or user data is used.
# Support-user directory and assignment

Support-staff ticket detail views load `GET /api/support-users` through the authenticated API client. A shared, memory-only cache coalesces concurrent requests and supports retry; it is never written to local or session storage. The assignment form selects only returned user identifiers, shows display names and relevant roles, and has no raw GUID field. Successful responses replace ticket detail immediately without an optimistic update. Stale targets refresh the directory and produce a safe message. Frontend role checks control presentation only: the endpoint policy and `TicketService` remain authoritative.

# Attachments

Ticket details render only safe attachment metadata. Uploads use authenticated multipart requests and client-side size/extension checks as convenience checks; backend content and access validation remains authoritative. Downloads use the authenticated client to obtain a Blob, trigger a temporary object URL, and revoke it immediately—no protected endpoint is rendered as a plain link. Deletes require confirmation and update the UI only after server success. Untrusted documents are not previewed, and storage provider, key, hash, or paths are never exposed.

# Ticket cancellation

Eligible ticket-detail views expose an accessible two-step cancellation section with an optional 500-character reason. No optimistic state is applied: the returned detail replaces local state. Cancelled details show the authoritative timestamp and final-state explanation while retaining the actual workflow status, histories, comments, and attachment downloads. Prohibited edit, assignment, status, upload, and non-support comment controls are hidden. Ticket lists show a separate Cancelled marker alongside the real status. Role checks are presentation-only; backend ownership, terminal-state, and authorization rules remain authoritative.

# Authenticated report exports

The support-only Reports page keeps editable draft filters separate from the filters applied to the visible report. PDF and Excel buttons send only the applied filter object to authenticated backend export endpoints, ensuring the downloaded values match the screen. Responses are handled as Blobs through the shared API client, including refresh behavior. Downloads use a temporary object URL and anchor that are removed immediately after the click; protected export URLs, tokens, and file contents are never persisted or logged in the browser.

# AI ticket analysis

Ticket detail includes a secondary, explicitly triggered AI Analysis card. The browser sends only the ticket identifier through the authenticated API client; ticket content and provider configuration stay on the backend. Results are advisory text with nullable category/priority suggestions, bounded troubleshooting items, and a visible review disclaimer. Failures remain isolated from all normal ticket controls. Output uses normal React text rendering, with no HTML/Markdown interpretation and no automatic application action.
