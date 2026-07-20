START TRANSACTION;
CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TokenHash" character varying(64) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "UsedAtUtc" timestamp with time zone,
    "RevokedAtUtc" timestamp with time zone,
    "ReplacedByTokenId" uuid,
    "CreatedByIpAddress" character varying(45),
    "RevokedByIpAddress" character varying(45),
    "RevocationReason" character varying(500),
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_RefreshTokens_ExpiresAfterCreated" CHECK ("ExpiresAtUtc" > "CreatedAtUtc"),
    CONSTRAINT "CK_RefreshTokens_ReplacementDiffers" CHECK ("ReplacedByTokenId" IS NULL OR "ReplacedByTokenId" <> "Id"),
    CONSTRAINT "CK_RefreshTokens_RevokedAfterCreated" CHECK ("RevokedAtUtc" IS NULL OR "RevokedAtUtc" >= "CreatedAtUtc"),
    CONSTRAINT "CK_RefreshTokens_TokenHash_Format" CHECK ("TokenHash" ~ '^[0-9a-f]{64}$'),
    CONSTRAINT "CK_RefreshTokens_UsedAfterCreated" CHECK ("UsedAtUtc" IS NULL OR "UsedAtUtc" >= "CreatedAtUtc"),
    CONSTRAINT "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId" FOREIGN KEY ("ReplacedByTokenId") REFERENCES "RefreshTokens" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_RefreshTokens_ReplacedByTokenId" ON "RefreshTokens" ("ReplacedByTokenId") WHERE "ReplacedByTokenId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");

CREATE INDEX "IX_RefreshTokens_UserId_CreatedAtUtc" ON "RefreshTokens" ("UserId", "CreatedAtUtc");

CREATE INDEX "IX_RefreshTokens_UserId_ExpiresAtUtc" ON "RefreshTokens" ("UserId", "ExpiresAtUtc");

CREATE INDEX "IX_RefreshTokens_UserId_ExpiresAtUtc_Active" ON "RefreshTokens" ("UserId", "ExpiresAtUtc") WHERE "UsedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260720100526_AddRefreshTokens', '10.0.5');

COMMIT;

