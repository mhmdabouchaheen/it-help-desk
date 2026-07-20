# Migration review scripts

These files are generated Entity Framework Core review and deployment artifacts. They have not been executed against a database.

- `InitialCreate.sql` targets an empty database and applies the initial schema once.
- `InitialCreate.Idempotent.sql` checks EF migration history before applying the initial schema.
- `AddRefreshTokens.sql` reviews the schema change from `InitialCreate` to `AddRefreshTokens`.
- `AllMigrations.Idempotent.sql` conditionally applies all migrations through `AddRefreshTokens` based on EF migration history.

The `AddRefreshTokens` migration and both newly generated review scripts are intentionally unexecuted. Review them before using a normal database deployment process.

Database credentials and connection strings must never be stored in these scripts.
