using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: Zone may already exist from a manual ALTER before this migration lands.
            // Snapshot also caught up unrelated live tables (UpdateBlockReleases / UserDeviceTokens /
            // UserNotifications) that already exist in SahulatAppDB — do not recreate them here.
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Providers', 'Zone') IS NULL
BEGIN
    ALTER TABLE dbo.Providers ADD Zone nvarchar(100) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Providers', 'Zone') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Providers DROP COLUMN Zone;
END
");
        }
    }
}
