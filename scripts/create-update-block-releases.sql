/*
  STATUS: APPLIED 2026-10-06 - dbo.UpdateBlockReleases exists on SahulatAppDB (see db.txt). Needed by the "Release blocked devices" card (Admin > Push Broadcast).
  Idempotent, safe to re-run.

  dbo.UpdateBlockReleases - append-only history of admin "release blocked devices" sends (silent app_unblock push,
  see api.txt "Admin release of update blocks"). ReleasedAtUtc is the same instant sent to devices as sent_at.
  Scope: everyone | android | ios | user | device. No FKs (UserId = UsersLogin.UID, DeviceTokenId = UserDeviceTokens.Id;
  tokens can be deleted later and the history must survive).
*/
IF OBJECT_ID(N'dbo.UpdateBlockReleases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UpdateBlockReleases
    (
        Id            INT            NOT NULL IDENTITY(1,1),
        Scope         NVARCHAR(20)   NOT NULL,
        Platform      NVARCHAR(10)   NULL,
        UserId        INT            NULL,
        DeviceTokenId INT            NULL,
        ReleasedAtUtc DATETIME       NOT NULL,
        ReleasedBy    NVARCHAR(100)  NOT NULL,
        Reason        NVARCHAR(200)  NOT NULL,
        Recipients    INT            NOT NULL CONSTRAINT DF_UpdateBlockReleases_Recipients DEFAULT (0),
        Sent          INT            NOT NULL CONSTRAINT DF_UpdateBlockReleases_Sent DEFAULT (0),
        Failed        INT            NOT NULL CONSTRAINT DF_UpdateBlockReleases_Failed DEFAULT (0),
        CONSTRAINT PK_UpdateBlockReleases PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_UpdateBlockReleases_ReleasedAtUtc ON dbo.UpdateBlockReleases (ReleasedAtUtc DESC);
END
GO
