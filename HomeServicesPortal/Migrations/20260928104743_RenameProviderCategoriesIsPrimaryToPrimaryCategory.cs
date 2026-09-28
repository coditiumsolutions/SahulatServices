using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class RenameProviderCategoriesIsPrimaryToPrimaryCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: production/dev may already have been altered manually before this migration lands.
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.ProviderCategories', 'IsPrimary') IS NOT NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ProviderCategories')
      AND c.name = N'IsPrimary';

    IF @df IS NOT NULL
        EXEC(N'ALTER TABLE dbo.ProviderCategories DROP CONSTRAINT [' + @df + N']');

    EXEC sp_rename N'dbo.ProviderCategories.IsPrimary', N'PrimaryCategory', 'COLUMN';
    ALTER TABLE dbo.ProviderCategories ALTER COLUMN PrimaryCategory int NOT NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints
        WHERE name = N'DF_ProviderCategories_PrimaryCategory'
          AND parent_object_id = OBJECT_ID(N'dbo.ProviderCategories'))
    BEGIN
        ALTER TABLE dbo.ProviderCategories
            ADD CONSTRAINT DF_ProviderCategories_PrimaryCategory DEFAULT ((0)) FOR PrimaryCategory;
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.ProviderCategories', 'PrimaryCategory') IS NOT NULL
   AND COL_LENGTH('dbo.ProviderCategories', 'IsPrimary') IS NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ProviderCategories')
      AND c.name = N'PrimaryCategory';

    IF @df IS NOT NULL
        EXEC(N'ALTER TABLE dbo.ProviderCategories DROP CONSTRAINT [' + @df + N']');

    EXEC sp_rename N'dbo.ProviderCategories.PrimaryCategory', N'IsPrimary', 'COLUMN';
    ALTER TABLE dbo.ProviderCategories ALTER COLUMN IsPrimary bit NOT NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.ProviderCategories')
          AND COL_NAME(parent_object_id, parent_column_id) = N'IsPrimary')
    BEGIN
        ALTER TABLE dbo.ProviderCategories
            ADD CONSTRAINT DF_ProviderCategories_IsPrimary DEFAULT ((0)) FOR IsPrimary;
    END
END
");
        }
    }
}
