/*
================================================================================
  Script   : alter-provider-documents-verification.sql
  Purpose  : Add verification columns to dbo.ProviderDocuments
  Safe     : Idempotent (checks COL_LENGTH before ALTER)
================================================================================
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.ProviderDocuments', N'U') IS NULL
BEGIN
    THROW 50001, N'dbo.ProviderDocuments does not exist.', 1;
END
GO

IF COL_LENGTH(N'dbo.ProviderDocuments', N'IsVerified') IS NULL
BEGIN
    ALTER TABLE dbo.ProviderDocuments
    ADD IsVerified BIT NOT NULL
        CONSTRAINT DF_ProviderDocuments_IsVerified DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.ProviderDocuments', N'VerifiedOn') IS NULL
BEGIN
    ALTER TABLE dbo.ProviderDocuments ADD VerifiedOn DATETIME NULL;
END
GO

IF COL_LENGTH(N'dbo.ProviderDocuments', N'VerifiedBy') IS NULL
BEGIN
    ALTER TABLE dbo.ProviderDocuments ADD VerifiedBy INT NULL;
END
GO

IF COL_LENGTH(N'dbo.ProviderDocuments', N'VerificationRemarks') IS NULL
BEGIN
    ALTER TABLE dbo.ProviderDocuments ADD VerificationRemarks NVARCHAR(500) NULL;
END
GO
