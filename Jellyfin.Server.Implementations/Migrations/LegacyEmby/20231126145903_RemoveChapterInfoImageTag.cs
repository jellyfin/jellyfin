using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations.LegacyEmby
{
    /// <inheritdoc />
    public partial class RemoveChapterInfoImageTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageTag",
                table: "ChapterInfos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageTag",
                table: "ChapterInfos",
                type: "TEXT",
                nullable: true);
        }
    }
}
