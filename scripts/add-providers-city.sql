/*
  Add Providers.City (idempotent).
*/
IF COL_LENGTH(N'dbo.Providers', N'City') IS NULL
BEGIN
    ALTER TABLE dbo.Providers ADD City nvarchar(100) NULL;
END
GO
