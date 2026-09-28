using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderCategoriesJunctionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderCategories",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderUID = table.Column<int>(type: "int", nullable: false),
                    CategoryUID = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCategories", x => x.UID);
                    table.ForeignKey(
                        name: "FK_ProviderCategories_Providers",
                        column: x => x.ProviderUID,
                        principalTable: "Providers",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderCategories_ServiceCategories",
                        column: x => x.CategoryUID,
                        principalTable: "ServiceCategories",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCategories_CategoryUID",
                table: "ProviderCategories",
                column: "CategoryUID");

            migrationBuilder.CreateIndex(
                name: "UQ_ProviderCategories_ProviderUID_CategoryUID",
                table: "ProviderCategories",
                columns: new[] { "ProviderUID", "CategoryUID" },
                unique: true);

            // Backfill: every existing provider gets one ProviderCategories row for their
            // current (single) CategoryUID, flagged IsPrimary=1, so no provider is ever left
            // without a junction row once this migration completes.
            migrationBuilder.Sql(@"
                INSERT INTO ProviderCategories (ProviderUID, CategoryUID, IsPrimary, CreatedOn)
                SELECT p.UID, p.CategoryUID, 1, GETDATE()
                FROM Providers p;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderCategories");
        }
    }
}
