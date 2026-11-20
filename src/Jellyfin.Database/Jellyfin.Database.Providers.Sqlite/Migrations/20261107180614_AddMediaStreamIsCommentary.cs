using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaStreamIsCommentary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommentary",
                table: "MediaStreamInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommentary",
                table: "MediaStreamInfos");
        }
    }
}
