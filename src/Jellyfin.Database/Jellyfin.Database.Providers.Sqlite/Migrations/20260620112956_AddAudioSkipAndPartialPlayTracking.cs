using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioSkipAndPartialPlayTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSkippedDate",
                table: "UserData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartiallyPlayed",
                table: "UserData",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SkipCount",
                table: "UserData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOriginal",
                table: "MediaStreamInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_SkipCount",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "SkipCount" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_SkipCount_ItemId",
                table: "UserData",
                columns: new[] { "UserId", "SkipCount", "ItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserData_ItemId_UserId_SkipCount",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_UserData_UserId_SkipCount_ItemId",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "LastSkippedDate",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "PartiallyPlayed",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "SkipCount",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "IsOriginal",
                table: "MediaStreamInfos");
        }
    }
}
