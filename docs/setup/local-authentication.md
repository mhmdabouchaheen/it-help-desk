# Local authentication configuration

The tracked `appsettings.json` intentionally contains an empty `Jwt:SecretKey`. The API refuses to start until a valid secret is supplied from a non-tracked source.

## Environment variable

Set `Jwt__SecretKey` to a development-only secret containing at least 32 UTF-8 bytes before starting the API. For example, in PowerShell:

```powershell
$env:Jwt__SecretKey = "<at-least-32-byte-development-secret>"
dotnet run --project server/HelpDesk.Api/HelpDesk.Api.csproj
```

Remove the environment variable when it is no longer needed:

```powershell
Remove-Item Env:Jwt__SecretKey
```

## Ignored local settings

Alternatively, copy `server/HelpDesk.Api/appsettings.Local.example.json` to `appsettings.Local.json` and replace the empty JWT value locally:

```json
{
  "Jwt": {
    "SecretKey": "<at-least-32-byte-development-secret>"
  }
}
```

`appsettings.Local.json` is ignored by Git and is loaded optionally in the Development environment. Never commit it or place its secret in tracked settings, documentation, logs, or migration scripts.

Production secrets must come from a secure secret manager or protected environment variables. Development secrets must not be reused in production.

## Development-only local administrator

To create one local administrator for exercising support-only features, add the following section to the ignored `server/HelpDesk.Api/appsettings.Local.json`:

```json
{
  "DevelopmentAdmin": {
    "Enabled": true,
    "Email": "YOUR_LOCAL_ADMIN_EMAIL",
    "Password": "YOUR_LOCAL_ADMIN_PASSWORD",
    "DisplayName": "Local Admin"
  }
}
```

Use a password that satisfies the application's current ASP.NET Core Identity password policy. Then start PostgreSQL and run the backend with `ASPNETCORE_ENVIRONMENT=Development`. The bootstrap runs once at startup, creates the user through ASP.NET Core Identity if it is missing, and idempotently ensures the exact `Admin` role is assigned. Existing users are never duplicated and their passwords are never reset.

Log in through the normal frontend `/login` page and confirm that the sidebar identifies the account as Admin. You may set `DevelopmentAdmin:Enabled` back to `false` after the account has been created.

This mechanism does not run in Production, Staging, or the default Testing environment. Never commit `appsettings.Local.json`, reuse its password, or place the password in logs or tracked files. The equivalent environment-variable keys are `DevelopmentAdmin__Enabled`, `DevelopmentAdmin__Email`, `DevelopmentAdmin__Password`, and `DevelopmentAdmin__DisplayName`.

## Token handling

Access tokens are short-lived signed credentials. Refresh tokens support longer sessions, but clients must protect them as sensitive credentials and must never write them to logs.

The server stores only lowercase SHA-256 hashes of refresh tokens. Plaintext values are returned only when a token is created or rotated. Each rotated token is single-use and links to its replacement; detected reuse revokes the user's active refresh tokens.

Revocation and rotation will be exposed through future authentication endpoints. No refresh-token endpoint exists yet.
