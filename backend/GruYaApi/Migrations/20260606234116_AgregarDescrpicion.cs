using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDescrpicion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Locations");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ProviderProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ProviderProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Locations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
