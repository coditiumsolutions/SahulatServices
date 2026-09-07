/*
  Idempotent create for dbo.AdminNotifications — admin portal notification bell.
*/
IF OBJECT_ID(N'dbo.AdminNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminNotifications
    (
        UID               INT            NOT NULL IDENTITY(1,1),
        Type              NVARCHAR(50)   NOT NULL,
        Title             NVARCHAR(200)  NOT NULL,
        Message           NVARCHAR(500)  NOT NULL,
        LinkUrl           NVARCHAR(300)  NULL,
        RelatedEntityUID  INT            NULL,
        IsRead            BIT            NOT NULL CONSTRAINT DF_AdminNotifications_IsRead DEFAULT (0),
        CreatedOn         DATETIME       NOT NULL CONSTRAINT DF_AdminNotifications_CreatedOn DEFAULT (GETDATE()),
        CONSTRAINT PK_AdminNotifications PRIMARY KEY CLUSTERED (UID)
    );

    CREATE INDEX IX_AdminNotifications_IsRead_CreatedOn
        ON dbo.AdminNotifications (IsRead, CreatedOn DESC);

    EXEC sys.sp_addextendedproperty
        @name = N'MS_Description', @value = N'Admin portal in-app notifications (e.g. new service requests).',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = N'AdminNotifications';
END
GO
