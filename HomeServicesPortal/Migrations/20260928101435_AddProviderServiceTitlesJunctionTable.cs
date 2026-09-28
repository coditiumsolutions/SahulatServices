using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderServiceTitlesJunctionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderServiceTitles",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderUID = table.Column<int>(type: "int", nullable: false),
                    ServiceTitleUID = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderServiceTitles", x => x.UID);
                    table.ForeignKey(
                        name: "FK_ProviderServiceTitles_Providers",
                        column: x => x.ProviderUID,
                        principalTable: "Providers",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderServiceTitles_ServiceTitles",
                        column: x => x.ServiceTitleUID,
                        principalTable: "ServiceTitles",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderServiceTitles_ServiceTitleUID",
                table: "ProviderServiceTitles",
                column: "ServiceTitleUID");

            migrationBuilder.CreateIndex(
                name: "UQ_ProviderServiceTitles_ProviderUID_ServiceTitleUID",
                table: "ProviderServiceTitles",
                columns: new[] { "ProviderUID", "ServiceTitleUID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderServiceTitles");
        }
    }
}
