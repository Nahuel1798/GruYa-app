using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class AssistanceOriginDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Location_Longitude",
                table: "Assistances",
                newName: "Origin_Longitude");

            migrationBuilder.RenameColumn(
                name: "Location_Latitude",
                table: "Assistances",
                newName: "Origin_Latitude");

            migrationBuilder.AddColumn<decimal>(
                name: "Destination_Latitude",
                table: "Assistances",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Destination_Longitude",
                table: "Assistances",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Destination_Latitude",
                table: "Assistances");

            migrationBuilder.DropColumn(
                name: "Destination_Longitude",
                table: "Assistances");

            migrationBuilder.RenameColumn(
                name: "Origin_Longitude",
                table: "Assistances",
                newName: "Location_Longitude");

            migrationBuilder.RenameColumn(
                name: "Origin_Latitude",
                table: "Assistances",
                newName: "Location_Latitude");
        }
    }
}
