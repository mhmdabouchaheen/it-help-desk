# IT Help Desk & Ticketing Management System

## Project Overview

A full-stack help-desk application for creating, assigning, tracking, and auditing IT support tickets. The project demonstrates secure authentication, role-based authorization, operational workflows, persistent notifications, real-time invalidation, analytics, attachments, and an append-only activity trail.

For a complete non-technical walkthrough of every role, page, workflow, feature, security rule, and important limitation, open the standalone [HTML user guide](docs/user-guide.html) in a browser.

## Features

- Registration, login, refresh-token rotation, logout, and JWT authorization
- Admin, IT Support Agent, Employee, and Manager roles
- Ticket creation, editing, cancellation, assignment, status transitions, comments, and internal notes
- Validated attachments with authorization-aware download and soft deletion
- Dashboard analytics, persistent notifications, and SignalR notification updates
- Support-only global activity log and visibility-aware ticket audit timeline
- Optional advisory ticket summaries, categorization, priority recommendations, and troubleshooting through OpenAI or free local Ollama
- Responsive React interface, accessible loading/error states, and top-level render recovery

## Technology Stack

- Backend: ASP.NET Core 10, Entity Framework Core 10, ASP.NET Core Identity, PostgreSQL, SignalR
- Frontend: React 19, TypeScript, Vite, React Router, Recharts, Lucide
- Testing: xUnit, Moq, SQLite in-memory test infrastructure, Vitest, Testing Library

## Architecture

The API separates controllers, application interfaces/contracts, infrastructure services, EF entities/configuration, and centralized exception handling. The frontend separates authenticated API access, providers/hooks, route guards, layouts, pages, reusable components, and typed contracts. Backend authorization is authoritative; frontend role checks only control presentation.

## Screenshots

Add final demonstration screenshots here before internship submission:

- Login and registration
- Employee ticket list and ticket detail
- Support assignment/status workflow
- Dashboard analytics
- Notifications and activity log

## Repository Structure

```text
client/helpdesk-client/       React/Vite frontend
server/HelpDesk.Api/          ASP.NET Core API
server/HelpDesk.Api.Tests/    Isolated backend tests
docs/                         Architecture, setup, database, and UI notes
database/                     Database-oriented repository assets
```

## Prerequisites

- .NET SDK 10
- Node.js 20 or newer and npm
- PostgreSQL 15 or newer for local application use
- `dotnet-ef` matching the EF Core major version

## Local Setup

Clone the repository, then configure and start the backend and frontend separately. Never commit local configuration files.

### PostgreSQL Setup

Create an empty local database and a least-privilege application user. Put the connection string in the ignored `server/HelpDesk.Api/appsettings.Local.json`. Automated tests use SQLite/mocks and do not require PostgreSQL.

### Backend Configuration

```powershell
cd server/HelpDesk.Api
Copy-Item appsettings.Local.example.json appsettings.Local.json
```

Replace the placeholder connection password and JWT secret locally. The JWT secret must be at least 32 UTF-8 bytes. Production should provide configuration through environment variables:

| Environment variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment, normally `Production` |
| `ASPNETCORE_URLS` | API listen URLs inside the host/container |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `Jwt__SecretKey` | Strong secret from a secret manager |
| `Frontend__AllowedOrigins__0` | First explicit frontend origin; increment suffix for more origins |
| `Attachments__StorageRoot` | Non-public persistent attachment directory |
| `Logging__LogLevel__Default` | Production logging level, normally `Information` |

ASP.NET Core environment variables override JSON configuration. Do not place production values in tracked files.

### Frontend Configuration

```powershell
cd client/helpdesk-client
Copy-Item .env.example .env
```

`VITE_API_BASE_URL` must be the externally reachable API origin, for example `http://localhost:5213`. Vite variables are public browser build inputs and must never contain secrets.

## Database Migrations

The current order is:

1. `InitialCreate`
2. `AddRefreshTokens`

Apply migrations explicitly during deployment, before starting the new application version:

```powershell
dotnet ef database update
```

Alternatively, review and execute the repository's idempotent migration scripts through the deployment platform. Application startup does not call `EnsureCreated`, `EnsureDeleted`, `Migrate`, or destructive reset logic.

## Running the Application

Backend:

```powershell
cd server/HelpDesk.Api
dotnet restore
dotnet ef database update
dotnet run
```

Frontend:

```powershell
cd client/helpdesk-client
npm install
npm run dev
```

The launch profiles use `http://localhost:5213`, `https://localhost:7233`, and the frontend development origin `http://localhost:5173`. Development OpenAPI is available at `/openapi/v1.json`. Process health is available anonymously at `/healthz` and exposes only health status.

## Default Roles

- `Admin`: full administrative/support access
- `IT Support Agent`: organization-wide support workflow access
- `Employee`: access to tickets they create
- `Manager`: ownership-scoped ticket access plus management presentation where applicable

New self-registered users receive the Employee role. No production user or password is seeded by application code.

For a local-only Admin account, configure the disabled-by-default `DevelopmentAdmin` section in the ignored `server/HelpDesk.Api/appsettings.Local.json`, then start the backend in Development. See [local authentication setup](docs/setup/local-authentication.md). The bootstrap uses ASP.NET Core Identity, is idempotent, never resets an existing password, and never executes outside Development.

## Testing

```powershell
dotnet test server/HelpDesk.Api.Tests/HelpDesk.Api.Tests.csproj -c Release
cd client/helpdesk-client
npm run lint
npm run test -- --run
npm run build
```

## Security Design

The API validates JWT issuer, audience, lifetime, signature, subject, and roles. Route policies and service-level ownership checks remain authoritative. Refresh tokens are hashed at rest and rotation/reuse rules are enforced. CORS accepts only configured origins. Problem responses avoid internal exception details. Responses include basic anti-sniffing, anti-framing, referrer, and permissions headers; a deployment-specific Content Security Policy should be tested at the edge before enforcement.

Logs and audit metadata exclude passwords, tokens, token hashes, comment/internal-note bodies, attachment contents, and storage keys. Production secrets belong in a platform secret manager.

## Optional local AI

The ticket-detail AI analysis can run through OpenAI or free local Ollama. For a no-credit-card development setup, install Ollama, run `ollama pull llama3.2:3b`, and select `Ai:Provider` as `Ollama` in the ignored local settings. See [AI integration](docs/architecture/ai-integration.md) for the complete setup and security boundaries.

## Real-Time Notifications

Persistent notifications are authoritative; SignalR prompts clients to refresh them. Enable WebSockets and proxy upgrade headers. The current setup is suitable for one API instance. Multiple instances require sticky sessions where applicable and a supported SignalR backplane/service.

## File Attachments

Local storage is appropriate for development and a single-instance deployment only. `Attachments__StorageRoot` must point to a non-public persistent volume that survives restarts and redeployments. Back up this volume with the database. Horizontal scaling requires shared/object storage such as S3-compatible storage or Azure Blob Storage. Antivirus/content scanning is not implemented.

## Audit Trail

The existing `ActivityLogs` table records allowlisted, non-sensitive metadata. Global access is restricted to support roles and ticket history reuses ticket visibility rules. Writes are best effort rather than compliance-grade transactional auditing; a regulated design would require a transactional outbox or equivalent architecture.

## Deployment Notes

For the zero-cost internship/demo deployment using Render Static Sites, a Render Free Web Service, and Neon PostgreSQL, follow the complete [Render + Neon deployment guide](docs/deployment/free-render-neon.md). The repository includes a root `render.yaml`, a hardened multi-stage API Dockerfile, and SPA rewrite configuration.

- Terminate TLS at a trusted reverse proxy or platform and redirect HTTP to HTTPS.
- Trust forwarded headers only from explicitly configured proxy networks/addresses; broad forwarded-header trust is intentionally not enabled in application code.
- Configure the proxy for WebSocket connection upgrades and appropriate idle timeouts.
- Serve the Vite `dist` directory from a static host and rewrite unknown non-file routes to `index.html`, so `/login`, `/app/home`, and ticket routes survive hard refreshes.
- Apply database migrations as a separate reviewed deployment step.
- Use a persistent, non-public attachment volume and a backup/restore plan.
- Run a single API instance unless SignalR and attachment scale-out requirements are addressed.
- Keep production logging at `Information` or stricter and avoid request-body logging.

Recommended container architecture, if containerization is added later: a static frontend host, ASP.NET Core API, managed PostgreSQL, and persistent/object attachment storage. Docker files are intentionally omitted until that deployment topology can be validated.

## Known Limitations

- Frontend tokens are memory-only; a page refresh loses the session.
- A future authentication redesign should prefer secure HttpOnly cookies where appropriate.
- Local attachments require a persistent volume and do not include antivirus scanning.
- Multi-instance SignalR needs scale-out/backplane planning.
- Activity logging is best effort, not compliance-grade transactional audit.
- No email or SMS notification channel is implemented.
- No cloud attachment provider is implemented; AI analysis remains optional and provider-dependent.
- Dependency advisories may remain where no verified compatible fix is available; review validation reports before deployment.

## Future Improvements

- Transactional audit outbox
- Object storage and malware scanning
- SignalR managed service/backplane
- Secure cookie-based browser authentication redesign
- Deployment-specific CSP and centralized observability
- Email/SMS notification channels

## Internship Learning Outcomes

This project demonstrates layered full-stack architecture, secure identity and authorization, relational modeling, workflow design, asynchronous notifications, safe file handling, audit design, automated testing, production configuration, and deployment-readiness review.

## License / Academic Use

This repository is an internship/academic portfolio project. Add an explicit license before permitting redistribution or production reuse.
