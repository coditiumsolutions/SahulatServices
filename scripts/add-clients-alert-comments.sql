/*
  Add Clients.CustomerAlert and Clients.Comments (idempotent).
*/
IF COL_LENGTH(N'dbo.Clients', N'CustomerAlert') IS NULL
BEGIN
    ALTER TABLE dbo.Clients ADD CustomerAlert nvarchar(500) NULL;
END
GO

IF COL_LENGTH(N'dbo.Clients', N'Comments') IS NULL
BEGIN
    ALTER TABLE dbo.Clients ADD Comments nvarchar(1000) NULL;
END
GO
