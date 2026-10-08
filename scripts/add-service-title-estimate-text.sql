/*
  STATUS: APPLIED 2026-10-08 to SahulatAppDB (must exist before the code that reads ServiceTitles.EstimateText is deployed).
  Idempotent, safe to re-run. Free-text estimate (e.g. "2000-3000") shown to customers; BasePrice stays numeric.
*/
IF COL_LENGTH('dbo.ServiceTitles', 'EstimateText') IS NULL
BEGIN
    ALTER TABLE dbo.ServiceTitles ADD EstimateText nvarchar(100) NULL;
END
GO
