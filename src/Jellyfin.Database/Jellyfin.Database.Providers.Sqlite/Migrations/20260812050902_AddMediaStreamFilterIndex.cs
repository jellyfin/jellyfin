using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaStreamFilterIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamType_ItemId_Language_IsExternal",
                table: "MediaStreamInfos",
                columns: new[] { "StreamType", "ItemId", "Language", "IsExternal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaStreamInfos_StreamType_ItemId_Language_IsExternal",
                table: "MediaStreamInfos");
        }
    }
}
