using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_UserId",
                table: "Vehicles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_UserId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Vehicles");
        }
    }
}
