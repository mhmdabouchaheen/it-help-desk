using Microsoft.Data.Sqlite;

namespace HelpDesk.Api.Tests;

internal static class TicketSqliteDatabase
{
    public static async Task InitializeAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE "Users" (
                "Id" TEXT PRIMARY KEY, "UserName" TEXT, "NormalizedUserName" TEXT,
                "Email" TEXT, "NormalizedEmail" TEXT, "EmailConfirmed" INTEGER NOT NULL DEFAULT 0,
                "PasswordHash" TEXT, "SecurityStamp" TEXT, "ConcurrencyStamp" TEXT,
                "PhoneNumber" TEXT, "PhoneNumberConfirmed" INTEGER NOT NULL DEFAULT 0,
                "TwoFactorEnabled" INTEGER NOT NULL DEFAULT 0, "LockoutEnd" TEXT,
                "LockoutEnabled" INTEGER NOT NULL DEFAULT 0, "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
                "DisplayName" TEXT NOT NULL, "IsActive" INTEGER NOT NULL DEFAULT 1,
                "CreatedAtUtc" TEXT NOT NULL, "UpdatedAtUtc" TEXT NOT NULL, "DeactivatedAtUtc" TEXT
            );
            CREATE TABLE "Roles" (
                "Id" TEXT PRIMARY KEY, "Name" TEXT, "NormalizedName" TEXT,
                "ConcurrencyStamp" TEXT, "Description" TEXT, "IsActive" INTEGER NOT NULL DEFAULT 1,
                "CreatedAtUtc" TEXT NOT NULL, "UpdatedAtUtc" TEXT NOT NULL
            );
            CREATE TABLE "UserRoles" (
                "UserId" TEXT NOT NULL, "RoleId" TEXT NOT NULL,
                "AssignedAtUtc" TEXT NOT NULL, "AssignedByUserId" TEXT,
                PRIMARY KEY ("UserId", "RoleId")
            );
            CREATE TABLE "Categories" (
                "Id" INTEGER PRIMARY KEY, "Name" TEXT NOT NULL, "Description" TEXT,
                "SortOrder" INTEGER NOT NULL, "IsActive" INTEGER NOT NULL DEFAULT 1,
                "CreatedAtUtc" TEXT NOT NULL, "UpdatedAtUtc" TEXT NOT NULL
            );
            CREATE TABLE "Priorities" (
                "Id" INTEGER PRIMARY KEY, "Name" TEXT NOT NULL, "Rank" INTEGER NOT NULL,
                "Description" TEXT, "IsActive" INTEGER NOT NULL DEFAULT 1,
                "CreatedAtUtc" TEXT NOT NULL, "UpdatedAtUtc" TEXT NOT NULL
            );
            CREATE TABLE "Statuses" (
                "Id" INTEGER PRIMARY KEY, "Name" TEXT NOT NULL, "Description" TEXT,
                "SortOrder" INTEGER NOT NULL, "IsTerminal" INTEGER NOT NULL DEFAULT 0, "IsActive" INTEGER NOT NULL DEFAULT 1,
                "CreatedAtUtc" TEXT NOT NULL, "UpdatedAtUtc" TEXT NOT NULL
            );
            CREATE TABLE "Tickets" (
                "Id" TEXT PRIMARY KEY, "ReferenceNumber" TEXT NOT NULL, "Title" TEXT NOT NULL,
                "Description" TEXT NOT NULL, "CategoryId" INTEGER NOT NULL, "PriorityId" INTEGER NOT NULL,
                "StatusId" INTEGER NOT NULL, "CreatedByUserId" TEXT NOT NULL, "AssignedToUserId" TEXT,
                "CreatedAtUtc" TEXT NOT NULL, "UpdatedAtUtc" TEXT NOT NULL,
                "ResolvedAtUtc" TEXT, "ClosedAtUtc" TEXT, "CancelledAtUtc" TEXT
            );
            CREATE UNIQUE INDEX "IX_Tickets_ReferenceNumber" ON "Tickets" ("ReferenceNumber");
            CREATE TABLE "TicketComments" (
                "Id" TEXT PRIMARY KEY, "TicketId" TEXT NOT NULL, "AuthorUserId" TEXT NOT NULL,
                "Body" TEXT NOT NULL, "Visibility" TEXT NOT NULL, "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT, "DeletedAtUtc" TEXT
            );
            CREATE TABLE "TicketAttachments" (
                "Id" TEXT PRIMARY KEY, "TicketId" TEXT NOT NULL, "CommentId" TEXT,
                "UploadedByUserId" TEXT NOT NULL, "OriginalFileName" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL, "SizeBytes" INTEGER NOT NULL,
                "StorageProvider" TEXT NOT NULL, "StorageKey" TEXT NOT NULL,
                "ContentHash" TEXT, "CreatedAtUtc" TEXT NOT NULL, "DeletedAtUtc" TEXT
            );
            CREATE TABLE "TicketAssignments" (
                "Id" TEXT PRIMARY KEY, "TicketId" TEXT NOT NULL, "AssignedToUserId" TEXT NOT NULL,
                "AssignedByUserId" TEXT, "AssignedAtUtc" TEXT NOT NULL, "EndedAtUtc" TEXT,
                "EndedByUserId" TEXT, "Reason" TEXT
            );
            CREATE UNIQUE INDEX "IX_TicketAssignments_TicketId"
                ON "TicketAssignments" ("TicketId") WHERE "EndedAtUtc" IS NULL;
            CREATE TABLE "TicketStatusHistory" (
                "Id" TEXT PRIMARY KEY, "TicketId" TEXT NOT NULL, "FromStatusId" INTEGER,
                "ToStatusId" INTEGER NOT NULL, "ChangedByUserId" TEXT,
                "ChangedAtUtc" TEXT NOT NULL, "Reason" TEXT
            );
            """;
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();

        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("O");
        await InsertAsync(connection,
            """INSERT INTO "Categories" VALUES (1,'Hardware',NULL,1,1,$now,$now),(2,'Software',NULL,2,1,$now,$now);""", now);
        await InsertAsync(connection,
            """INSERT INTO "Priorities" VALUES (1,'Low',1,NULL,1,$now,$now),(2,'Medium',2,NULL,1,$now,$now);""", now);
        await InsertAsync(connection,
            """INSERT INTO "Statuses" VALUES (1,'Open',NULL,1,0,1,$now,$now),(2,'In Progress',NULL,2,0,1,$now,$now),(3,'Pending',NULL,3,0,1,$now,$now),(4,'Resolved',NULL,4,0,1,$now,$now),(5,'Closed',NULL,5,1,1,$now,$now);""", now);
        await InsertAsync(connection,
            """INSERT INTO "Roles" VALUES ('11111111-1111-1111-1111-111111111111','Admin','ADMIN','a',NULL,1,$now,$now),('22222222-2222-2222-2222-222222222222','IT Support Agent','IT SUPPORT AGENT','b',NULL,1,$now,$now),('33333333-3333-3333-3333-333333333333','Employee','EMPLOYEE','c',NULL,1,$now,$now),('44444444-4444-4444-4444-444444444444','Manager','MANAGER','d',NULL,1,$now,$now);""", now);
    }

    private static async Task InsertAsync(SqliteConnection connection, string sql, string now)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync();
    }
}
