using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddLabourAndMaterialItemsToServiceBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LabourAmount",
                table: "ServiceBookings",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingMaterialItems",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingUID = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 1m),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingMaterialItems", x => x.UID);
                    table.ForeignKey(
                        name: "FK_BookingMaterialItems_ServiceBookings",
                        column: x => x.BookingUID,
                        principalTable: "ServiceBookings",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingMaterialItems_BookingUID",
                table: "BookingMaterialItems",
                column: "BookingUID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingMaterialItems");

            migrationBuilder.DropColumn(
                name: "LabourAmount",
                table: "ServiceBookings");
        }
    }
}
