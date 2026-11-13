using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseItemGenreAndStudioLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseItemGenres",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenreItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemGenres", x => new { x.ItemId, x.GenreItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemGenres_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseItemStudios",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudioItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemStudios", x => new { x.ItemId, x.StudioItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemStudios_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemGenres_GenreItemId_ItemId",
                table: "BaseItemGenres",
                columns: new[] { "GenreItemId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemStudios_StudioItemId_ItemId",
                table: "BaseItemStudios",
                columns: new[] { "StudioItemId", "ItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseItemGenres");

            migrationBuilder.DropTable(
                name: "BaseItemStudios");
        }
    }
}
