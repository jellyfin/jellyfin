using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddHiddenByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHiddenByUser",
                table: "UserData",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_IsHiddenByUser",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "IsHiddenByUser" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserData_ItemId_UserId_IsHiddenByUser",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "IsHiddenByUser",
                table: "UserData");
        }
    }
}
