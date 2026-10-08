/*
  STATUS: APPLIED 2026-10-08 to SahulatAppDB (must exist before the code that reads ProviderZones is deployed).
  Idempotent, safe to re-run. One row per (provider, zone). Zone names come from Configurations key=Zone.
  Providers.Zone stays as a comma-separated display copy. Backfills existing single Providers.Zone values.
*/
IF OBJECT_ID(N'dbo.ProviderZones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProviderZones (
        UID          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProviderZones PRIMARY KEY,
        ProviderUID  int NOT NULL,
        ZoneName     nvarchar(100) NOT NULL,
        CreatedOn    datetime NOT NULL CONSTRAINT DF_ProviderZones_CreatedOn DEFAULT (getdate()),
        CONSTRAINT FK_ProviderZones_Providers FOREIGN KEY (ProviderUID) REFERENCES dbo.Providers (UID) ON DELETE CASCADE,
        CONSTRAINT UQ_ProviderZones_ProviderUID_ZoneName UNIQUE (ProviderUID, ZoneName)
    );
END
GO

INSERT INTO dbo.ProviderZones (ProviderUID, ZoneName)
SELECT p.UID, LTRIM(RTRIM(p.Zone))
FROM dbo.Providers p
WHERE p.Zone IS NOT NULL AND LTRIM(RTRIM(p.Zone)) <> ''
  AND NOT EXISTS (SELECT 1 FROM dbo.ProviderZones z WHERE z.ProviderUID = p.UID);
GO
