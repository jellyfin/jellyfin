using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelImageCacheColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ETag",
                table: "BaseItemImageInfos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "BaseItemImageInfos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SourceLastModified",
                table: "BaseItemImageInfos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ETag",
                table: "BaseItemImageInfos");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "BaseItemImageInfos");

            migrationBuilder.DropColumn(
                name: "SourceLastModified",
                table: "BaseItemImageInfos");
        }
    }
}
