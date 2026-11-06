using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioSortTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SortAlbum",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SortAlbumArtist",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SortArtist",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortAlbum",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "SortAlbumArtist",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "SortArtist",
                table: "BaseItems");
        }
    }
}
