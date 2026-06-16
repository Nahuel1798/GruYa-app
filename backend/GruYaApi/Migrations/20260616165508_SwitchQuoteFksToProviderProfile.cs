using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GruYaApi.Migrations
{
    /// <inheritdoc />
    public partial class SwitchQuoteFksToProviderProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assistances_Users_RequestedProviderId",
                table: "Assistances");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Users_ProviderId",
                table: "Quotes");

            migrationBuilder.RenameColumn(
                name: "ProviderId",
                table: "Quotes",
                newName: "ProviderProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Quotes_ProviderId",
                table: "Quotes",
                newName: "IX_Quotes_ProviderProfileId");

            migrationBuilder.RenameColumn(
                name: "RequestedProviderId",
                table: "Assistances",
                newName: "RequestedProviderProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Assistances_RequestedProviderId",
                table: "Assistances",
                newName: "IX_Assistances_RequestedProviderProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assistances_ProviderProfiles_RequestedProviderProfileId",
                table: "Assistances",
                column: "RequestedProviderProfileId",
                principalTable: "ProviderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_ProviderProfiles_ProviderProfileId",
                table: "Quotes",
                column: "ProviderProfileId",
                principalTable: "ProviderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assistances_ProviderProfiles_RequestedProviderProfileId",
                table: "Assistances");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_ProviderProfiles_ProviderProfileId",
                table: "Quotes");

            migrationBuilder.RenameColumn(
                name: "ProviderProfileId",
                table: "Quotes",
                newName: "ProviderId");

            migrationBuilder.RenameIndex(
                name: "IX_Quotes_ProviderProfileId",
                table: "Quotes",
                newName: "IX_Quotes_ProviderId");

            migrationBuilder.RenameColumn(
                name: "RequestedProviderProfileId",
                table: "Assistances",
                newName: "RequestedProviderId");

            migrationBuilder.RenameIndex(
                name: "IX_Assistances_RequestedProviderProfileId",
                table: "Assistances",
                newName: "IX_Assistances_RequestedProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assistances_Users_RequestedProviderId",
                table: "Assistances",
                column: "RequestedProviderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Users_ProviderId",
                table: "Quotes",
                column: "ProviderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
