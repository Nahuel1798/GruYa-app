using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregoTablaPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssistanceId",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AssistanceId",
                table: "Payments",
                column: "AssistanceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Assistances_AssistanceId",
                table: "Payments",
                column: "AssistanceId",
                principalTable: "Assistances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Assistances_AssistanceId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_AssistanceId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AssistanceId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Payments");
        }
    }
}
