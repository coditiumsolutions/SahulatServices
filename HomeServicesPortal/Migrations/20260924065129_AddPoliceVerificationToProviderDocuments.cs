using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPoliceVerificationToProviderDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PoliceVerificationPath",
                table: "ProviderDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoliceVerificationPath",
                table: "ProviderDocuments");
        }
    }
}
