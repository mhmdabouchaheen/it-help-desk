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

## Token handling

Access tokens are short-lived signed credentials. Refresh tokens support longer sessions, but clients must protect them as sensitive credentials and must never write them to logs.

The server stores only lowercase SHA-256 hashes of refresh tokens. Plaintext values are returned only when a token is created or rotated. Each rotated token is single-use and links to its replacement; detected reuse revokes the user's active refresh tokens.

Revocation and rotation will be exposed through future authentication endpoints. No refresh-token endpoint exists yet.
