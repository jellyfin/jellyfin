using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyToOwnerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseItems_BaseItems_BaseItemEntityId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_BaseItemEntityId",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "BaseItemEntityId",
                table: "BaseItems");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseItems_BaseItems_OwnerId",
                table: "BaseItems",
                column: "OwnerId",
                principalTable: "BaseItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseItems_BaseItems_OwnerId",
                table: "BaseItems");

            migrationBuilder.AddColumn<Guid>(
                name: "BaseItemEntityId",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "BaseItemEntityId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_BaseItemEntityId",
                table: "BaseItems",
                column: "BaseItemEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseItems_BaseItems_BaseItemEntityId",
                table: "BaseItems",
                column: "BaseItemEntityId",
                principalTable: "BaseItems",
                principalColumn: "Id");
        }
    }
}
