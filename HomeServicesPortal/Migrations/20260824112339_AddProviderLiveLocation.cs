using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderLiveLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Providers",
                type: "decimal(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationUpdatedOn",
                table: "Providers",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Providers",
                type: "decimal(10,7)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "LocationUpdatedOn",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Providers");
        }
    }
}
