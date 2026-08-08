using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ListType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoRemoveWatched = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemLists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemListBaseItemMap",
                columns: table => new
                {
                    ItemListId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomDataKey = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RetentionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SortIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemListBaseItemMap", x => new { x.ItemListId, x.CustomDataKey });
                    table.ForeignKey(
                        name: "FK_ItemListBaseItemMap_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ItemListBaseItemMap_ItemLists_ItemListId",
                        column: x => x.ItemListId,
                        principalTable: "ItemLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemListBaseItemMap_ItemId",
                table: "ItemListBaseItemMap",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemListBaseItemMap_ItemListId_SortIndex",
                table: "ItemListBaseItemMap",
                columns: new[] { "ItemListId", "SortIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLists_UserId_Name",
                table: "ItemLists",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemLists_UserId_SortIndex",
                table: "ItemLists",
                columns: new[] { "UserId", "SortIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemListBaseItemMap");

            migrationBuilder.DropTable(
                name: "ItemLists");
        }
    }
}
