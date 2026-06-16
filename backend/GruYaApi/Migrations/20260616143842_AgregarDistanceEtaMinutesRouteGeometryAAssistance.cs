using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDistanceEtaMinutesRouteGeometryAAssistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanceKm",
                table: "Assistances",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EtaMinutes",
                table: "Assistances",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteGeometry",
                table: "Assistances",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Assistances");

            migrationBuilder.DropColumn(
                name: "EtaMinutes",
                table: "Assistances");

            migrationBuilder.DropColumn(
                name: "RouteGeometry",
                table: "Assistances");
        }
    }
}
