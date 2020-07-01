#pragma warning disable CS1591
#pragma warning disable SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class AddDisplayPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisplayPreferences",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<Guid>(nullable: true),
                    Client = table.Column<string>(maxLength: 64, nullable: false),
                    RememberIndexing = table.Column<bool>(nullable: false),
                    RememberSorting = table.Column<bool>(nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    ShowSidebar = table.Column<bool>(nullable: false),
                    ShowBackdrop = table.Column<bool>(nullable: false),
                    SortBy = table.Column<string>(nullable: true),
                    ViewType = table.Column<int>(nullable: true),
                    ScrollDirection = table.Column<int>(nullable: false),
                    IndexBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisplayPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "jellyfin",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomeSection",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayPreferencesId = table.Column<int>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeSection_DisplayPreferences_DisplayPreferencesId",
                        column: x => x.DisplayPreferencesId,
                        principalSchema: "jellyfin",
                        principalTable: "DisplayPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisplayPreferences_UserId",
                schema: "jellyfin",
                table: "DisplayPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeSection_DisplayPreferencesId",
                schema: "jellyfin",
                table: "HomeSection",
                column: "DisplayPreferencesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeSection",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "DisplayPreferences",
                schema: "jellyfin");
        }
    }
}
