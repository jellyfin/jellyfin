using System;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class LibraryPeopleMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Peoples_BaseItems_ItemId",
                table: "Peoples");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Peoples",
                table: "Peoples");

            migrationBuilder.DropIndex(
                name: "IX_Peoples_ItemId_ListOrder",
                table: "Peoples");

            migrationBuilder.DropColumn(
                name: "ListOrder",
                table: "Peoples");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Peoples");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "Peoples",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Peoples",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Peoples",
                table: "Peoples",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "PeopleBaseItemMap",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeopleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    ListOrder = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeopleBaseItemMap", x => new { x.ItemId, x.PeopleId });
                    table.ForeignKey(
                        name: "FK_PeopleBaseItemMap_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeopleBaseItemMap_Peoples_PeopleId",
                        column: x => x.PeopleId,
                        principalTable: "Peoples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_ListOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "ListOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_SortOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_PeopleId",
                table: "PeopleBaseItemMap",
                column: "PeopleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeopleBaseItemMap");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Peoples",
                table: "Peoples");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Peoples",
                newName: "ItemId");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Peoples",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ListOrder",
                table: "Peoples",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Peoples",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Peoples",
                table: "Peoples",
                columns: new[] { "ItemId", "Role", "ListOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_ItemId_ListOrder",
                table: "Peoples",
                columns: new[] { "ItemId", "ListOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Peoples_BaseItems_ItemId",
                table: "Peoples",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
