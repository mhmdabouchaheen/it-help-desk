# Local database setup

Keep local PostgreSQL credentials in `server/HelpDesk.Api/appsettings.Local.json`. This file is ignored by Git and must never be committed. Use `appsettings.Local.example.json` as the placeholder-only template.

From `server/HelpDesk.Api`, apply the refresh-token migration explicitly:

```powershell
dotnet ef database update 20260720100526_AddRefreshTokens --context ApplicationDbContext
```

Verify the migration history without exposing credentials:

```powershell
dotnet ef migrations list --context ApplicationDbContext
```

The expected applied migrations are:

- `20260718133039_InitialCreate`
- `20260720100526_AddRefreshTokens`

Always confirm the effective connection targets the intended local development database before running a database update.
