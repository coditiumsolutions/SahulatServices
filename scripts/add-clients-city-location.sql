/*
  Add Clients.City and Clients.Location (idempotent).
*/
IF COL_LENGTH(N'dbo.Clients', N'City') IS NULL
BEGIN
    ALTER TABLE dbo.Clients ADD City nvarchar(100) NULL;
END
GO

IF COL_LENGTH(N'dbo.Clients', N'Location') IS NULL
BEGIN
    ALTER TABLE dbo.Clients ADD Location nvarchar(250) NULL;
END
GO
