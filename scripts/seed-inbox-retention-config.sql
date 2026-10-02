/*
  Seed the in-app inbox limits into dbo.Configurations so they show up (and are editable) under
  Admin > Configurations (idempotent). The app falls back to the same defaults if a row is missing.

    Inbox.RetentionDays  90   notifications older than this many days are deleted (allowed 7-365)
    Inbox.MaxPerRole     200  newest notifications kept per user and role (allowed 20-1000)
*/
IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'Inbox.RetentionDays')
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValue) VALUES (N'Inbox.RetentionDays', N'90');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'Inbox.MaxPerRole')
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValue) VALUES (N'Inbox.MaxPerRole', N'200');
GO
