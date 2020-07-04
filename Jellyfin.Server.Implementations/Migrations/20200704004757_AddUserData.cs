#pragma warning disable CS1591
#pragma warning disable SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class AddUserData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserItemData",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<Guid>(nullable: false),
                    IsPlayed = table.Column<bool>(nullable: false),
                    PlayCount = table.Column<int>(nullable: false),
                    PlaybackPositionTicks = table.Column<long>(nullable: false),
                    LastPlayedDate = table.Column<DateTime>(nullable: true),
                    VideoStreamIndex = table.Column<int>(nullable: true),
                    AudioStreamIndex = table.Column<int>(nullable: true),
                    SubtitleStreamIndex = table.Column<int>(nullable: true),
                    IsFavorite = table.Column<bool>(nullable: false),
                    Rating = table.Column<float>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserItemData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserItemData_UserId_ItemId",
                schema: "jellyfin",
                table: "UserItemData",
                columns: new[] { "UserId", "ItemId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserItemData",
                schema: "jellyfin");
        }
    }
}
