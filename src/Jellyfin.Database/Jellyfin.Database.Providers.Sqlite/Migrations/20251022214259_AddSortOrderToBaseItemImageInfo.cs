using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// Adds SortOrder column, populates it based on DateModified, and creates 3-column index.
    /// </summary>
    public partial class AddSortOrderToBaseItemImageInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old single-column index on ItemId
            migrationBuilder.DropIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos");

            // Add SortOrder column with default value 0
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "BaseItemImageInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Populate SortOrder based on DateModified order
            // Uses ROW_NUMBER() to assign sequential SortOrder per (ItemId, ImageType) group
            migrationBuilder.Sql(
                @"WITH ImageInfos AS
                (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY ItemId, ImageType ORDER BY DateModified) - 1 AS OrderId
                    FROM BaseItemImageInfos
                )
                UPDATE BaseItemImageInfos
                SET SortOrder = (SELECT OrderId FROM ImageInfos WHERE BaseItemImageInfos.Id = ImageInfos.Id)");

            // Create the final 3-column index
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

            // Restore the original single-column index
            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos",
                column: "ItemId");
        }
    }
}
