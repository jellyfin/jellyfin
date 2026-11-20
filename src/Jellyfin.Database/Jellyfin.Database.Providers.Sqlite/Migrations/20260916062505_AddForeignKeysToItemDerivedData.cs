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
            // that follow the copy, so rows without an item have to leave first. On a 10.10 upgrade BaseItems is still
            // empty here, so they are set aside for MigrateLibraryDb to restore rather than deleted.
            migrationBuilder.Sql("CREATE TABLE OrphanedTrickplayInfos AS SELECT * FROM TrickplayInfos WHERE NOT EXISTS (SELECT 1 FROM BaseItems WHERE BaseItems.Id = TrickplayInfos.ItemId);");
            migrationBuilder.Sql("DELETE FROM TrickplayInfos WHERE NOT EXISTS (SELECT 1 FROM BaseItems WHERE BaseItems.Id = TrickplayInfos.ItemId);");
            migrationBuilder.Sql("CREATE TABLE OrphanedMediaSegments AS SELECT * FROM MediaSegments WHERE NOT EXISTS (SELECT 1 FROM BaseItems WHERE BaseItems.Id = MediaSegments.ItemId);");
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
                onDelete: ReferentialAction.NoAction);

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

            migrationBuilder.Sql("DROP TABLE IF EXISTS OrphanedTrickplayInfos;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS OrphanedMediaSegments;");
        }
    }
}
