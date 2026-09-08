/*
  Create dbo.Configurations and seed Cities (idempotent).
*/
IF OBJECT_ID(N'dbo.Configurations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Configurations
    (
        UID         int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Configurations PRIMARY KEY,
        ConfigKey   nvarchar(100) NOT NULL,
        ConfigValue nvarchar(MAX) NOT NULL,
        CreatedOn   datetime NOT NULL
            CONSTRAINT DF_Configurations_CreatedOn DEFAULT (getdate())
    );

    CREATE UNIQUE INDEX UQ_Configurations_ConfigKey
        ON dbo.Configurations (ConfigKey);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'Cities')
BEGIN
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValue)
    VALUES (
        N'Cities',
        N'Karachi, Islamabad, Faisalabad, Lahore, Multan, Sialkot, Sukkur, Hyderabad'
    );
END
GO
