using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleIdentityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CleanName",
                table: "Peoples",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "Peoples",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_CleanName_PersonType",
                table: "Peoples",
                columns: new[] { "CleanName", "PersonType" });

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_ItemId",
                table: "Peoples",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemProviders_ProviderId_ProviderValue",
                table: "BaseItemProviders",
                columns: new[] { "ProviderId", "ProviderValue" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Peoples_CleanName_PersonType",
                table: "Peoples");

            migrationBuilder.DropIndex(
                name: "IX_Peoples_ItemId",
                table: "Peoples");

            migrationBuilder.DropIndex(
                name: "IX_BaseItemProviders_ProviderId_ProviderValue",
                table: "BaseItemProviders");

            migrationBuilder.DropColumn(
                name: "CleanName",
                table: "Peoples");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Peoples");
        }
    }
}
