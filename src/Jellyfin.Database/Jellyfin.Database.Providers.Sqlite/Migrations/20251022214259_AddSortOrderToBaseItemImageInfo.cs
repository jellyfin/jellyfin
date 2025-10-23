using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToBaseItemImageInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "BaseItemImageInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId_SortOrder",
                table: "BaseItemImageInfos",
                columns: new[] { "ItemId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItemImageInfos_ItemId_SortOrder",
                table: "BaseItemImageInfos");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "BaseItemImageInfos");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos",
                column: "ItemId");
        }
    }
}
