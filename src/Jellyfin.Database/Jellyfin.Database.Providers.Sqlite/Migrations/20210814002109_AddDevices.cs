#pragma warning disable CS1591, SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class AddDevices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceOptions",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    CustomName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    AppName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "jellyfin",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_AccessToken",
                schema: "jellyfin",
                table: "ApiKeys",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceOptions_DeviceId",
                schema: "jellyfin",
                table: "DeviceOptions",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_AccessToken_DateLastActivity",
                schema: "jellyfin",
                table: "Devices",
                columns: new[] { "AccessToken", "DateLastActivity" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                schema: "jellyfin",
                table: "Devices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId_DateLastActivity",
                schema: "jellyfin",
                table: "Devices",
                columns: new[] { "DeviceId", "DateLastActivity" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId_DeviceId",
                schema: "jellyfin",
                table: "Devices",
                columns: new[] { "UserId", "DeviceId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "DeviceOptions",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "Devices",
                schema: "jellyfin");
        }
    }
}
