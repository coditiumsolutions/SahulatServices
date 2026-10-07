/*
  STATUS: APPLIED 2026-10-07 to SahulatAppDB (optional; the endpoint works without it).
  Idempotent, safe to re-run. The endpoint works without it; the table is tiny, so this only keeps the lookup cheap
  as the history grows.

  GET /api/v1/app/config?platform=..&device_token=.. (api.txt v3.40) returns last_unblock_at = MAX(ReleasedAtUtc) over the
  releases that apply to the caller. This index covers the "everyone" and platform branches of that query.
*/
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_UpdateBlockReleases_Scope_Platform_ReleasedAtUtc'
                 AND object_id = OBJECT_ID(N'dbo.UpdateBlockReleases'))
BEGIN
    CREATE INDEX IX_UpdateBlockReleases_Scope_Platform_ReleasedAtUtc
        ON dbo.UpdateBlockReleases (Scope, Platform, ReleasedAtUtc DESC);
END
GO
