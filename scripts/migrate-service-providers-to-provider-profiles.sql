-- Migrates provider FKs from ServiceProviders to ProviderProfiles, then drops ServiceProviders.
-- Run against SahulatAppDB before deploying the updated application.
-- BACK UP THE DATABASE BEFORE RUNNING.

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    -- 1) Migrate ServiceProviders rows into Users + ProviderProfiles (skip when mobile already registered same mobile).
    IF OBJECT_ID(N'dbo.ServiceProviders', N'U') IS NOT NULL
    BEGIN
        DECLARE @Map TABLE (OldProviderUid INT NOT NULL PRIMARY KEY, NewProfileUid INT NOT NULL);

        -- Use cursor-less migration via temp mapping table
        CREATE TABLE #ProviderMigration (
            OldUid INT NOT NULL PRIMARY KEY,
            FullName VARCHAR(150) NOT NULL,
            MobileNo VARCHAR(20) NULL,
            CNIC VARCHAR(20) NULL,
            CategoryUID INT NOT NULL,
            ExperienceYears INT NULL,
            Rating DECIMAL(3,2) NULL,
            IsVerified BIT NULL,
            IsActive BIT NULL,
            CreatedOn DATETIME NULL
        );

        INSERT INTO #ProviderMigration (OldUid, FullName, MobileNo, CNIC, CategoryUID, ExperienceYears, Rating, IsVerified, IsActive, CreatedOn)
        SELECT UID, FullName, MobileNo, CNIC, CategoryUID, ExperienceYears, Rating, IsVerified, IsActive, CreatedOn
        FROM dbo.ServiceProviders;

        DECLARE @OldUid INT, @FullName VARCHAR(150), @Mobile VARCHAR(20), @Cnic VARCHAR(20);
        DECLARE @CategoryUID INT, @Exp INT, @Rating DECIMAL(3,2), @Verified BIT, @Active BIT, @Created DATETIME;
        DECLARE @UserUid INT, @ProfileUid INT, @Email VARCHAR(150);

        DECLARE mig CURSOR LOCAL FAST_FORWARD FOR
            SELECT OldUid, FullName, MobileNo, CNIC, CategoryUID, ExperienceYears, Rating, IsVerified, IsActive, CreatedOn
            FROM #ProviderMigration;

        OPEN mig;
        FETCH NEXT FROM mig INTO @OldUid, @FullName, @Mobile, @Cnic, @CategoryUID, @Exp, @Rating, @Verified, @Active, @Created;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @ProfileUid = NULL;
            SET @UserUid = NULL;

            -- Reuse existing provider profile when mobile matches.
            IF @Mobile IS NOT NULL
            BEGIN
                SELECT TOP 1 @ProfileUid = pp.UID, @UserUid = pp.UserUID
                FROM dbo.ProviderProfiles pp
                INNER JOIN dbo.Users u ON u.UID = pp.UserUID
                WHERE u.MobileNo = @Mobile;
            END

            IF @ProfileUid IS NULL
            BEGIN
                SET @Email = CONCAT('migrated_provider_', @OldUid, '@homeservices.local');

                INSERT INTO dbo.Users (FullName, MobileNo, Email, PasswordHash, UserType, IsActive, CreatedOn)
                VALUES (@FullName, @Mobile, @Email, CONVERT(VARCHAR(36), NEWID()), N'Provider', ISNULL(@Active, 1), ISNULL(@Created, GETDATE()));

                SET @UserUid = SCOPE_IDENTITY();

                INSERT INTO dbo.ProviderProfiles (UserUID, CategoryUID, CNIC, ExperienceYears, Rating, IsVerified)
                VALUES (@UserUid, @CategoryUID, @Cnic, ISNULL(@Exp, 0), ISNULL(@Rating, 0), ISNULL(@Verified, 0));

                SET @ProfileUid = SCOPE_IDENTITY();
            END

            INSERT INTO @Map (OldProviderUid, NewProfileUid) VALUES (@OldUid, @ProfileUid);

            FETCH NEXT FROM mig INTO @OldUid, @FullName, @Mobile, @Cnic, @CategoryUID, @Exp, @Rating, @Verified, @Active, @Created;
        END

        CLOSE mig;
        DEALLOCATE mig;

        -- 2) Remap child tables to ProviderProfiles.UID
        UPDATE pl SET ProviderUID = m.NewProfileUid
        FROM dbo.ProviderLocations pl INNER JOIN @Map m ON pl.ProviderUID = m.OldProviderUid;

        UPDATE pa SET ProviderUID = m.NewProfileUid
        FROM dbo.ProviderAvailability pa INNER JOIN @Map m ON pa.ProviderUID = m.OldProviderUid;

        UPDATE pd SET ProviderUID = m.NewProfileUid
        FROM dbo.ProviderDocuments pd INNER JOIN @Map m ON pd.ProviderUID = m.OldProviderUid;

        UPDATE pq SET ProviderUID = m.NewProfileUid
        FROM dbo.ProviderQuotes pq INNER JOIN @Map m ON pq.ProviderUID = m.OldProviderUid;

        UPDATE b SET ProviderUID = m.NewProfileUid
        FROM dbo.Bookings b INNER JOIN @Map m ON b.ProviderUID = m.OldProviderUid;

        UPDATE r SET ProviderUID = m.NewProfileUid
        FROM dbo.Reviews r INNER JOIN @Map m ON r.ProviderUID = m.OldProviderUid;

        DROP TABLE #ProviderMigration;
    END

    -- 3) Drop old FK constraints pointing at ServiceProviders (names from existing DB)
    DECLARE @sql NVARCHAR(MAX) = N'';

    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(10)
    FROM sys.foreign_keys
    WHERE referenced_object_id = OBJECT_ID(N'dbo.ServiceProviders');

    EXEC sp_executesql @sql;

    -- 4) Add FK constraints to ProviderProfiles (if not already present)
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProviderLocations_ProviderProfiles')
        ALTER TABLE dbo.ProviderLocations WITH CHECK
        ADD CONSTRAINT FK_ProviderLocations_ProviderProfiles FOREIGN KEY (ProviderUID) REFERENCES dbo.ProviderProfiles(UID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProviderAvailability_ProviderProfiles')
        ALTER TABLE dbo.ProviderAvailability WITH CHECK
        ADD CONSTRAINT FK_ProviderAvailability_ProviderProfiles FOREIGN KEY (ProviderUID) REFERENCES dbo.ProviderProfiles(UID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProviderDocuments_ProviderProfiles')
        ALTER TABLE dbo.ProviderDocuments WITH CHECK
        ADD CONSTRAINT FK_ProviderDocuments_ProviderProfiles FOREIGN KEY (ProviderUID) REFERENCES dbo.ProviderProfiles(UID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProviderQuotes_ProviderProfiles')
        ALTER TABLE dbo.ProviderQuotes WITH CHECK
        ADD CONSTRAINT FK_ProviderQuotes_ProviderProfiles FOREIGN KEY (ProviderUID) REFERENCES dbo.ProviderProfiles(UID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Bookings_ProviderProfiles')
        ALTER TABLE dbo.Bookings WITH CHECK
        ADD CONSTRAINT FK_Bookings_ProviderProfiles FOREIGN KEY (ProviderUID) REFERENCES dbo.ProviderProfiles(UID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reviews_ProviderProfiles')
        ALTER TABLE dbo.Reviews WITH CHECK
        ADD CONSTRAINT FK_Reviews_ProviderProfiles FOREIGN KEY (ProviderUID) REFERENCES dbo.ProviderProfiles(UID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProviderProfiles_ServiceCategories')
        ALTER TABLE dbo.ProviderProfiles WITH CHECK
        ADD CONSTRAINT FK_ProviderProfiles_ServiceCategories FOREIGN KEY (CategoryUID) REFERENCES dbo.ServiceCategories(UID);

    -- 5) Drop legacy tables
    IF OBJECT_ID(N'dbo.ProviderUsers', N'U') IS NOT NULL
        DROP TABLE dbo.ProviderUsers;

    IF OBJECT_ID(N'dbo.ServiceProviders', N'U') IS NOT NULL
        DROP TABLE dbo.ServiceProviders;

    COMMIT TRANSACTION;
    PRINT 'Migration completed: ServiceProviders removed; ProviderProfiles is now the provider master.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
