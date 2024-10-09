using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class FixedAncestorIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AncestorIds_BaseItems_ItemId",
                table: "AncestorIds");

            migrationBuilder.DropIndex(
                name: "IX_AncestorIds_ItemId_AncestorIdText",
                table: "AncestorIds");

            migrationBuilder.RenameColumn(
                name: "AncestorIdText",
                table: "AncestorIds",
                newName: "BaseItemEntityId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AncestorIds",
                newName: "ParentItemId");

            migrationBuilder.RenameIndex(
                name: "IX_AncestorIds_Id",
                table: "AncestorIds",
                newName: "IX_AncestorIds_ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_BaseItemEntityId",
                table: "AncestorIds",
                column: "BaseItemEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AncestorIds_BaseItems_BaseItemEntityId",
                table: "AncestorIds",
                column: "BaseItemEntityId",
                principalTable: "BaseItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AncestorIds_BaseItems_BaseItemEntityId",
                table: "AncestorIds");

            migrationBuilder.DropIndex(
                name: "IX_AncestorIds_BaseItemEntityId",
                table: "AncestorIds");

            migrationBuilder.RenameColumn(
                name: "BaseItemEntityId",
                table: "AncestorIds",
                newName: "AncestorIdText");

            migrationBuilder.RenameColumn(
                name: "ParentItemId",
                table: "AncestorIds",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_AncestorIds_ParentItemId",
                table: "AncestorIds",
                newName: "IX_AncestorIds_Id");

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_ItemId_AncestorIdText",
                table: "AncestorIds",
                columns: new[] { "ItemId", "AncestorIdText" });

            migrationBuilder.AddForeignKey(
                name: "FK_AncestorIds_BaseItems_ItemId",
                table: "AncestorIds",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
