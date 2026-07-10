using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameCroppingToMediaStreamInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CropBottom",
                table: "MediaStreamInfos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CropLeft",
                table: "MediaStreamInfos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CropRight",
                table: "MediaStreamInfos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CropTop",
                table: "MediaStreamInfos",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CropBottom",
                table: "MediaStreamInfos");

            migrationBuilder.DropColumn(
                name: "CropLeft",
                table: "MediaStreamInfos");

            migrationBuilder.DropColumn(
                name: "CropRight",
                table: "MediaStreamInfos");

            migrationBuilder.DropColumn(
                name: "CropTop",
                table: "MediaStreamInfos");
        }
    }
}
