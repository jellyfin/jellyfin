using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class CoverAncestorIdsParentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AncestorIds_ParentItemId",
                table: "AncestorIds");

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_ParentItemId_ItemId",
                table: "AncestorIds",
                columns: new[] { "ParentItemId", "ItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AncestorIds_ParentItemId_ItemId",
                table: "AncestorIds");

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_ParentItemId",
                table: "AncestorIds",
                column: "ParentItemId");
        }
    }
}
