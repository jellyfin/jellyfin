using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class ExpandedBaseItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Images",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "LockedFields",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "TrailerTypes",
                table: "BaseItems");

            migrationBuilder.AlterColumn<int>(
                name: "ExtraType",
                table: "BaseItems",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Audio",
                table: "BaseItems",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BaseItemImageInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImageType = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Blurhash = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemImageInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseItemImageInfos_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseItemMetadataFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemMetadataFields", x => new { x.Id, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemMetadataFields_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseItemTrailerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemTrailerTypes", x => new { x.Id, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemTrailerTypes_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemMetadataFields_ItemId",
                table: "BaseItemMetadataFields",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemTrailerTypes_ItemId",
                table: "BaseItemTrailerTypes",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseItemImageInfos");

            migrationBuilder.DropTable(
                name: "BaseItemMetadataFields");

            migrationBuilder.DropTable(
                name: "BaseItemTrailerTypes");

            migrationBuilder.AlterColumn<string>(
                name: "ExtraType",
                table: "BaseItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Audio",
                table: "BaseItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedFields",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrailerTypes",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);
        }
    }
}
