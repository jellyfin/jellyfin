using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddDateLastModifiedFilesystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateLastModifiedFilesystem",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE BaseItems SET DateLastModifiedFilesystem = DateCreated WHERE PATH IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateLastModifiedFilesystem",
                table: "BaseItems");
        }
    }
}
