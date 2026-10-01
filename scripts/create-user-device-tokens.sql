/*
  STATUS: APPLIED 2026-10-01 — dbo.UserDeviceTokens exists on SahulatAppDB (see db.txt). Idempotent, safe to re-run.

  Idempotent create for dbo.UserDeviceTokens — FCM registration tokens (POST /api/notifications/register-token).
  UserId is UsersLogin.UID. DeviceToken is unique: one device token belongs to one user at a time.
  No FK to UsersLogin (rows are removed by AuthService.DeleteAccountAsync and by FCM "Unregistered" cleanup).
*/
IF OBJECT_ID(N'dbo.UserDeviceTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDeviceTokens
    (
        Id           INT            NOT NULL IDENTITY(1,1),
        UserId       INT            NOT NULL,
        UserType     NVARCHAR(20)   NOT NULL,
        DeviceToken  NVARCHAR(512)  NOT NULL,
        Platform     NVARCHAR(10)   NOT NULL,
        UpdatedAt    DATETIME       NOT NULL CONSTRAINT DF_UserDeviceTokens_UpdatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_UserDeviceTokens PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX UX_UserDeviceTokens_DeviceToken ON dbo.UserDeviceTokens (DeviceToken);
    CREATE INDEX IX_UserDeviceTokens_UserId ON dbo.UserDeviceTokens (UserId);
END
GO
