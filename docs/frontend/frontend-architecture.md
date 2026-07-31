# Frontend architecture

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
