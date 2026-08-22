using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanedUserPermissionsAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Permissions WHERE UserId IS NULL;");
            migrationBuilder.Sql("DELETE FROM Preferences WHERE UserId IS NULL;");

            migrationBuilder.DropIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Preference_Preferences_Guid",
                table: "Preferences");

            migrationBuilder.DropColumn(
                name: "Permission_Permissions_Guid",
                table: "Permissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Preferences",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Permissions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences",
                columns: ["UserId", "Kind"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions",
                columns: ["UserId", "Kind"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Preferences",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "Preference_Preferences_Guid",
                table: "Preferences",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Permissions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "Permission_Permissions_Guid",
                table: "Permissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences",
                columns: ["UserId", "Kind"],
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions",
                columns: ["UserId", "Kind"],
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }
    }
}
