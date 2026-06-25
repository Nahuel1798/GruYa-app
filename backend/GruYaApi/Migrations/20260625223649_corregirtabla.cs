using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class corregirtabla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "ProviderProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "ProviderProfiles");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLocation_Latitude",
                table: "ProviderProfiles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLocation_Longitude",
                table: "ProviderProfiles",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLocation_Latitude",
                table: "ProviderProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentLocation_Longitude",
                table: "ProviderProfiles");

            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "ProviderProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "ProviderProfiles",
                type: "double precision",
                nullable: true);
        }
    }
}
