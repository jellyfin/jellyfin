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

            // Populate SortOrder using a window function to order within each (ItemId, ImageType) group.
            // NOTE: This uses DateModified/Path ordering as a reasonable initial approximation.
            // The PopulateImageSortOrder startup migration (which runs after this) improves the ordering
            // by applying filename-based priority rules (e.g., {mediaName}-fanart before backdrop).
            migrationBuilder.Sql(
                @"WITH ImageInfos AS
(
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY ItemId, ImageType ORDER BY DateModified, Path) - 1 AS OrderId
    FROM BaseItemImageInfos
)
UPDATE BaseItemImageInfos
SET SortOrder = (SELECT OrderId FROM ImageInfos WHERE BaseItemImageInfos.Id = ImageInfos.Id)");

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
