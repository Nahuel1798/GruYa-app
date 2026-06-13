using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProviderProfileAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ProviderProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "ProviderProfiles");
        }
    }
}
