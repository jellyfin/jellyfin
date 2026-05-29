using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioSkipTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSkippedDate",
                table: "UserData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkipCount",
                table: "UserData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_LastSkippedDate",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "LastSkippedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_SkipCount",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "SkipCount" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_ItemId_LastSkippedDate",
                table: "UserData",
                columns: new[] { "UserId", "ItemId", "LastSkippedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_SkipCount_ItemId",
                table: "UserData",
                columns: new[] { "UserId", "SkipCount", "ItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserData_ItemId_UserId_LastSkippedDate",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_UserData_ItemId_UserId_SkipCount",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_UserData_UserId_ItemId_LastSkippedDate",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_UserData_UserId_SkipCount_ItemId",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "LastSkippedDate",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "SkipCount",
                table: "UserData");
        }
    }
}
