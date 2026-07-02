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
            // Drop the old 2-column index
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS IX_BaseItemImageInfos_ItemId_ImageType;");

            // Add the SortOrder column with default value 0
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "BaseItemImageInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Populate SortOrder using window function to order within each (ItemId, ImageType) group.
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

            // Create the new 3-column composite index
            migrationBuilder.Sql(
                @"CREATE INDEX IX_BaseItemImageInfos_ItemId_ImageType_SortOrder
ON BaseItemImageInfos (ItemId, ImageType, SortOrder);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS IX_BaseItemImageInfos_ItemId_ImageType_SortOrder;");

            migrationBuilder.Sql(
                @"CREATE INDEX IX_BaseItemImageInfos_ItemId_ImageType
ON BaseItemImageInfos (ItemId, ImageType);");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "BaseItemImageInfos");
        }
    }
}
