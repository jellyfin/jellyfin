using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSortNameInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SortNameInitial",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "SortNameInitial",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SortNameInitial",
                table: "BaseItems",
                column: "SortNameInitial");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItems_SortNameInitial",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "SortNameInitial",
                table: "BaseItems");
        }
    }
}
