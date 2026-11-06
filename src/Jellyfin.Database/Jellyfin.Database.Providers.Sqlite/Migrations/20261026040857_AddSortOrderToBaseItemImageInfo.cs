using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToBaseItemImageInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItemImageInfos_ItemId_ImageType",
                table: "BaseItemImageInfos");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "BaseItemImageInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"WITH ImageInfos AS
(
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY ItemId, ImageType ORDER BY DateModified, Path) - 1 AS OrderId
    FROM BaseItemImageInfos
)
UPDATE BaseItemImageInfos
SET SortOrder = (SELECT OrderId FROM ImageInfos WHERE BaseItemImageInfos.Id = ImageInfos.Id);");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId_ImageType_SortOrder",
                table: "BaseItemImageInfos",
                columns: new[] { "ItemId", "ImageType", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItemImageInfos_ItemId_ImageType_SortOrder",
                table: "BaseItemImageInfos");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "BaseItemImageInfos");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId_ImageType",
                table: "BaseItemImageInfos",
                columns: new[] { "ItemId", "ImageType" });
        }
    }
}
