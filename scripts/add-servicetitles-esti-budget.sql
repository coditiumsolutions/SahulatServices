/*
  Add ServiceTitles.esti_budget (idempotent).
*/
IF COL_LENGTH(N'dbo.ServiceTitles', N'esti_budget') IS NULL
BEGIN
    ALTER TABLE dbo.ServiceTitles ADD esti_budget decimal(12,2) NULL;
END
GO
