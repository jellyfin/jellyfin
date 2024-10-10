using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class FixedItemValueReferenceStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemValues_BaseItems_ItemId",
                table: "ItemValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemValues",
                table: "ItemValues");

            migrationBuilder.DropIndex(
                name: "IX_ItemValues_ItemId_Type_CleanValue",
                table: "ItemValues");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "ItemValues",
                newName: "ItemValueId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemValues",
                table: "ItemValues",
                column: "ItemValueId");

            migrationBuilder.CreateTable(
                name: "ItemValuesMap",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemValueId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValuesMap", x => new { x.ItemValueId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_ItemValuesMap_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemValuesMap_ItemValues_ItemValueId",
                        column: x => x.ItemValueId,
                        principalTable: "ItemValues",
                        principalColumn: "ItemValueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "Type", "CleanValue" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemValuesMap_ItemId",
                table: "ItemValuesMap",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_AncestorIds_BaseItems_ItemId",
                table: "AncestorIds",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AncestorIds_BaseItems_ParentItemId",
                table: "AncestorIds",
                column: "ParentItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AncestorIds_BaseItems_ItemId",
                table: "AncestorIds");

            migrationBuilder.DropForeignKey(
                name: "FK_AncestorIds_BaseItems_ParentItemId",
                table: "AncestorIds");

            migrationBuilder.DropTable(
                name: "ItemValuesMap");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemValues",
                table: "ItemValues");

            migrationBuilder.DropIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues");

            migrationBuilder.RenameColumn(
                name: "ItemValueId",
                table: "ItemValues",
                newName: "ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemValues",
                table: "ItemValues",
                columns: new[] { "ItemId", "Type", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_ItemId_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "ItemId", "Type", "CleanValue" });

            migrationBuilder.AddForeignKey(
                name: "FK_ItemValues_BaseItems_ItemId",
                table: "ItemValues",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
