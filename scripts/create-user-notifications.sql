/*
  STATUS: APPLIED 2026-10-01 — dbo.UserNotifications exists on SahulatAppDB (see db.txt). Idempotent, safe to re-run.

  Idempotent create for dbo.UserNotifications — per-user in-app notification inbox
  (GET /api/notifications, unread-count, read, read-all). UserId is UsersLogin.UID. UserType is the role
  (Client/Provider) the notification was addressed to, so a dual-role account keeps two separate inboxes.
  No FK to UsersLogin (rows are removed by AuthService.DeleteAccountAsync).
*/
IF OBJECT_ID(N'dbo.UserNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserNotifications
    (
        Id          INT            NOT NULL IDENTITY(1,1),
        UserId      INT            NOT NULL,
        UserType    NVARCHAR(20)   NOT NULL,
        [Type]      NVARCHAR(40)   NOT NULL,
        Title       NVARCHAR(200)  NOT NULL,
        Body        NVARCHAR(500)  NOT NULL,
        Screen      NVARCHAR(40)   NOT NULL,
        BookingUid  INT            NULL,
        RequestUid  INT            NULL,
        IsRead      BIT            NOT NULL CONSTRAINT DF_UserNotifications_IsRead DEFAULT (0),
        CreatedAt   DATETIME       NOT NULL CONSTRAINT DF_UserNotifications_CreatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_UserNotifications PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_UserNotifications_UserId_IsRead_CreatedAt
        ON dbo.UserNotifications (UserId, IsRead, CreatedAt DESC);
END
GO
