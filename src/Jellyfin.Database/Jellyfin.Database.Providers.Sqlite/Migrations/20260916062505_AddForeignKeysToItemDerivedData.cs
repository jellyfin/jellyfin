using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeysToItemDerivedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adding a constraint rebuilds the table, and EFCore turns foreign keys off only for the DROP and RENAME
            // that follow the copy, so we need to cleanup before adding the constraints.
            migrationBuilder.Sql("DELETE FROM TrickplayInfos WHERE NOT EXISTS (SELECT 1 FROM BaseItems WHERE BaseItems.Id = TrickplayInfos.ItemId);");
            migrationBuilder.Sql("DELETE FROM MediaSegments WHERE NOT EXISTS (SELECT 1 FROM BaseItems WHERE BaseItems.Id = MediaSegments.ItemId);");

            migrationBuilder.CreateIndex(
                name: "IX_MediaSegments_ItemId",
                table: "MediaSegments",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaSegments_BaseItems_ItemId",
                table: "MediaSegments",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrickplayInfos_BaseItems_ItemId",
                table: "TrickplayInfos",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaSegments_BaseItems_ItemId",
                table: "MediaSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_TrickplayInfos_BaseItems_ItemId",
                table: "TrickplayInfos");

            migrationBuilder.DropIndex(
                name: "IX_MediaSegments_ItemId",
                table: "MediaSegments");
        }
    }
}
