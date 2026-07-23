/*
================================================================================
  Script   : create-provider-documents.sql
  Purpose  : Create dbo.ProviderDocuments for provider profile / CNIC image paths
  Database : SQL Server 2022
  Notes    :
    - One document row per provider (UNIQUE on ProviderUID)
    - Stores relative paths only (e.g. uploads/providers/125/profile.jpg)
    - Safe to re-run (idempotent object creation)
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*------------------------------------------------------------------------------
  1. Create table (if missing)
------------------------------------------------------------------------------*/
IF OBJECT_ID(N'dbo.ProviderDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProviderDocuments
    (
        UID                  INT            NOT NULL IDENTITY(1, 1),
        ProviderUID          INT            NOT NULL,
        ProfilePhotoPath     NVARCHAR(500)  NULL,
        CNICFrontImagePath   NVARCHAR(500)  NULL,
        CNICBackImagePath    NVARCHAR(500)  NULL,
        CreatedOn            DATETIME       NOT NULL
            CONSTRAINT DF_ProviderDocuments_CreatedOn DEFAULT (GETDATE()),
        UpdatedOn            DATETIME       NULL,

        CONSTRAINT PK_ProviderDocuments
            PRIMARY KEY CLUSTERED (UID ASC)
    );
END
GO

/*------------------------------------------------------------------------------
  2. Unique constraint: one document record per provider
------------------------------------------------------------------------------*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE [name] = N'UQ_ProviderDocuments_ProviderUID'
      AND [parent_object_id] = OBJECT_ID(N'dbo.ProviderDocuments')
)
BEGIN
    ALTER TABLE dbo.ProviderDocuments
    ADD CONSTRAINT UQ_ProviderDocuments_ProviderUID
        UNIQUE NONCLUSTERED (ProviderUID ASC);
END
GO

/*------------------------------------------------------------------------------
  3. Foreign key to Providers(UID)
------------------------------------------------------------------------------*/
IF OBJECT_ID(N'dbo.Providers', N'U') IS NULL
BEGIN
    THROW 50001, N'dbo.Providers must exist before creating FK_ProviderDocuments_Providers.', 1;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ProviderDocuments_Providers'
      AND [parent_object_id] = OBJECT_ID(N'dbo.ProviderDocuments')
)
BEGIN
    ALTER TABLE dbo.ProviderDocuments
    ADD CONSTRAINT FK_ProviderDocuments_Providers
        FOREIGN KEY (ProviderUID)
        REFERENCES dbo.Providers (UID)
        ON DELETE CASCADE
        ON UPDATE NO ACTION;
END
GO

/*------------------------------------------------------------------------------
  4. Useful nonclustered indexes
      - UQ_ProviderDocuments_ProviderUID already covers ProviderUID lookups.
      - Filtered indexes support common "missing / complete document" checks.
------------------------------------------------------------------------------*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ProviderDocuments_CreatedOn'
      AND [object_id] = OBJECT_ID(N'dbo.ProviderDocuments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProviderDocuments_CreatedOn
        ON dbo.ProviderDocuments (CreatedOn DESC)
        INCLUDE (ProviderUID, ProfilePhotoPath, CNICFrontImagePath, CNICBackImagePath);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ProviderDocuments_MissingProfilePhoto'
      AND [object_id] = OBJECT_ID(N'dbo.ProviderDocuments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProviderDocuments_MissingProfilePhoto
        ON dbo.ProviderDocuments (ProviderUID ASC)
        WHERE ProfilePhotoPath IS NULL;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ProviderDocuments_CNICImagesPresent'
      AND [object_id] = OBJECT_ID(N'dbo.ProviderDocuments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProviderDocuments_CNICImagesPresent
        ON dbo.ProviderDocuments (ProviderUID ASC)
        INCLUDE (CNICFrontImagePath, CNICBackImagePath)
        WHERE CNICFrontImagePath IS NOT NULL
          AND CNICBackImagePath IS NOT NULL;
END
GO

/*------------------------------------------------------------------------------
  5. Extended property comments (MS_Description)
------------------------------------------------------------------------------*/
DECLARE @Schema SYSNAME = N'dbo';
DECLARE @Table  SYSNAME = N'ProviderDocuments';

/* Table comment */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.extended_properties ep
    INNER JOIN sys.tables t ON ep.major_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE ep.name = N'MS_Description'
      AND ep.minor_id = 0
      AND s.name = @Schema
      AND t.name = @Table
)
BEGIN
    EXEC sys.sp_addextendedproperty
        @name = N'MS_Description',
        @value = N'Stores relative file paths for a provider profile photo and CNIC images. One row per provider. Absolute URLs must not be stored.',
        @level0type = N'SCHEMA', @level0name = @Schema,
        @level1type = N'TABLE',  @level1name = @Table;
END
ELSE
BEGIN
    EXEC sys.sp_updateextendedproperty
        @name = N'MS_Description',
        @value = N'Stores relative file paths for a provider profile photo and CNIC images. One row per provider. Absolute URLs must not be stored.',
        @level0type = N'SCHEMA', @level0name = @Schema,
        @level1type = N'TABLE',  @level1name = @Table;
END

/* Helper: add or update column description */
DECLARE @Columns TABLE
(
    ColumnName SYSNAME NOT NULL PRIMARY KEY,
    Description NVARCHAR(500) NOT NULL
);

INSERT INTO @Columns (ColumnName, Description)
VALUES
    (N'UID', N'Identity primary key for the provider document record.'),
    (N'ProviderUID', N'Foreign key to Providers.UID. Unique — each provider may have only one document record.'),
    (N'ProfilePhotoPath', N'Relative path to the provider profile photo under wwwroot (example: uploads/providers/125/profile.jpg). NULL if not uploaded.'),
    (N'CNICFrontImagePath', N'Relative path to the CNIC front image under wwwroot (example: uploads/providers/125/cnic_front.jpg). NULL if not uploaded.'),
    (N'CNICBackImagePath', N'Relative path to the CNIC back image under wwwroot (example: uploads/providers/125/cnic_back.jpg). NULL if not uploaded.'),
    (N'CreatedOn', N'UTC/local server datetime when the document row was first created. Defaults to GETDATE().'),
    (N'UpdatedOn', N'Datetime when any document path was last updated. NULL until the first update.');

DECLARE @ColumnName SYSNAME;
DECLARE @Description NVARCHAR(500);

DECLARE col_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT ColumnName, Description FROM @Columns;

OPEN col_cursor;
FETCH NEXT FROM col_cursor INTO @ColumnName, @Description;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.extended_properties ep
        INNER JOIN sys.tables t ON ep.major_id = t.object_id
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        INNER JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ep.minor_id
        WHERE ep.name = N'MS_Description'
          AND s.name = @Schema
          AND t.name = @Table
          AND c.name = @ColumnName
    )
    BEGIN
        EXEC sys.sp_addextendedproperty
            @name = N'MS_Description',
            @value = @Description,
            @level0type = N'SCHEMA', @level0name = @Schema,
            @level1type = N'TABLE',  @level1name = @Table,
            @level2type = N'COLUMN', @level2name = @ColumnName;
    END
    ELSE
    BEGIN
        EXEC sys.sp_updateextendedproperty
            @name = N'MS_Description',
            @value = @Description,
            @level0type = N'SCHEMA', @level0name = @Schema,
            @level1type = N'TABLE',  @level1name = @Table,
            @level2type = N'COLUMN', @level2name = @ColumnName;
    END

    FETCH NEXT FROM col_cursor INTO @ColumnName, @Description;
END

CLOSE col_cursor;
DEALLOCATE col_cursor;
GO

/*------------------------------------------------------------------------------
  6. Sample INSERT statements (two providers)
     Adjust ProviderUID values to match real Providers.UID rows in your DB.
     Script is re-run safe: skips when a document row already exists.
------------------------------------------------------------------------------*/
DECLARE @SampleProvider1 INT;
DECLARE @SampleProvider2 INT;

SELECT TOP (1) @SampleProvider1 = p.UID
FROM dbo.Providers AS p
ORDER BY p.UID ASC;

SELECT TOP (1) @SampleProvider2 = p.UID
FROM dbo.Providers AS p
WHERE p.UID <> @SampleProvider1
ORDER BY p.UID ASC;

IF @SampleProvider1 IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.ProviderDocuments AS pd
       WHERE pd.ProviderUID = @SampleProvider1
   )
BEGIN
    INSERT INTO dbo.ProviderDocuments
    (
        ProviderUID,
        ProfilePhotoPath,
        CNICFrontImagePath,
        CNICBackImagePath,
        CreatedOn,
        UpdatedOn
    )
    VALUES
    (
        @SampleProvider1,
        N'uploads/providers/' + CAST(@SampleProvider1 AS NVARCHAR(20)) + N'/profile.jpg',
        N'uploads/providers/' + CAST(@SampleProvider1 AS NVARCHAR(20)) + N'/cnic_front.jpg',
        N'uploads/providers/' + CAST(@SampleProvider1 AS NVARCHAR(20)) + N'/cnic_back.jpg',
        GETDATE(),
        NULL
    );
END

IF @SampleProvider2 IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.ProviderDocuments AS pd
       WHERE pd.ProviderUID = @SampleProvider2
   )
BEGIN
    INSERT INTO dbo.ProviderDocuments
    (
        ProviderUID,
        ProfilePhotoPath,
        CNICFrontImagePath,
        CNICBackImagePath,
        CreatedOn,
        UpdatedOn
    )
    VALUES
    (
        @SampleProvider2,
        N'uploads/providers/' + CAST(@SampleProvider2 AS NVARCHAR(20)) + N'/profile.jpg',
        N'uploads/providers/' + CAST(@SampleProvider2 AS NVARCHAR(20)) + N'/cnic_front.jpg',
        NULL, -- intentionally incomplete for demo / missing-back scenarios
        GETDATE(),
        NULL
    );
END
GO

/*
-- Explicit sample inserts (uncomment and replace UIDs if preferred):

INSERT INTO dbo.ProviderDocuments
(
    ProviderUID,
    ProfilePhotoPath,
    CNICFrontImagePath,
    CNICBackImagePath,
    CreatedOn,
    UpdatedOn
)
VALUES
(
    1,
    N'uploads/providers/1/profile.jpg',
    N'uploads/providers/1/cnic_front.jpg',
    N'uploads/providers/1/cnic_back.jpg',
    GETDATE(),
    NULL
),
(
    2,
    N'uploads/providers/2/profile.jpg',
    N'uploads/providers/2/cnic_front.jpg',
    N'uploads/providers/2/cnic_back.jpg',
    GETDATE(),
    NULL
);
*/

/*------------------------------------------------------------------------------
  7. Verification / reporting queries
------------------------------------------------------------------------------*/

-- 7a. Providers with document information
SELECT
    p.UID              AS ProviderUID,
    p.FullName,
    p.CNIC,
    p.IsVerified,
    pd.UID             AS DocumentUID,
    pd.ProfilePhotoPath,
    pd.CNICFrontImagePath,
    pd.CNICBackImagePath,
    pd.CreatedOn       AS DocumentCreatedOn,
    pd.UpdatedOn       AS DocumentUpdatedOn
FROM dbo.Providers AS p
INNER JOIN dbo.ProviderDocuments AS pd
    ON pd.ProviderUID = p.UID
ORDER BY p.UID;

-- 7b. Providers whose documents are missing
--     (no document row, or any required path is NULL)
SELECT
    p.UID              AS ProviderUID,
    p.FullName,
    p.CNIC,
    p.IsVerified,
    CASE
        WHEN pd.UID IS NULL THEN N'No document record'
        WHEN pd.ProfilePhotoPath IS NULL THEN N'Missing profile photo'
        WHEN pd.CNICFrontImagePath IS NULL THEN N'Missing CNIC front'
        WHEN pd.CNICBackImagePath IS NULL THEN N'Missing CNIC back'
        ELSE N'Incomplete'
    END                AS MissingReason,
    pd.ProfilePhotoPath,
    pd.CNICFrontImagePath,
    pd.CNICBackImagePath
FROM dbo.Providers AS p
LEFT JOIN dbo.ProviderDocuments AS pd
    ON pd.ProviderUID = p.UID
WHERE pd.UID IS NULL
   OR pd.ProfilePhotoPath IS NULL
   OR pd.CNICFrontImagePath IS NULL
   OR pd.CNICBackImagePath IS NULL
ORDER BY p.UID;

-- 7c. Providers whose CNIC images exist (front and back)
SELECT
    p.UID              AS ProviderUID,
    p.FullName,
    p.CNIC,
    p.IsVerified,
    pd.UID             AS DocumentUID,
    pd.CNICFrontImagePath,
    pd.CNICBackImagePath,
    pd.ProfilePhotoPath,
    pd.UpdatedOn
FROM dbo.Providers AS p
INNER JOIN dbo.ProviderDocuments AS pd
    ON pd.ProviderUID = p.UID
WHERE pd.CNICFrontImagePath IS NOT NULL
  AND pd.CNICBackImagePath IS NOT NULL
ORDER BY p.UID;
GO
