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
                    Client = table.Column<string>(maxLength: 32, nullable: false),
                    ShowSidebar = table.Column<bool>(nullable: false),
                    ShowBackdrop = table.Column<bool>(nullable: false),
                    ScrollDirection = table.Column<int>(nullable: false),
                    IndexBy = table.Column<int>(nullable: true),
                    SkipForwardLength = table.Column<int>(nullable: false),
                    SkipBackwardLength = table.Column<int>(nullable: false),
                    ChromecastVersion = table.Column<int>(nullable: false),
                    EnableNextVideoInfoOverlay = table.Column<bool>(nullable: false),
                    DashboardTheme = table.Column<string>(maxLength: 32, nullable: true),
                    TvHome = table.Column<string>(maxLength: 32, nullable: true)
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
                name: "ItemDisplayPreferences",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<Guid>(nullable: false),
                    Client = table.Column<string>(maxLength: 32, nullable: false),
                    ViewType = table.Column<int>(nullable: false),
                    RememberIndexing = table.Column<bool>(nullable: false),
                    IndexBy = table.Column<int>(nullable: true),
                    RememberSorting = table.Column<bool>(nullable: false),
                    SortBy = table.Column<string>(maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDisplayPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemDisplayPreferences_Users_UserId",
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
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomeSection_DisplayPreferencesId",
                schema: "jellyfin",
                table: "HomeSection",
                column: "DisplayPreferencesId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemDisplayPreferences_UserId",
                schema: "jellyfin",
                table: "ItemDisplayPreferences",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeSection",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "ItemDisplayPreferences",
                schema: "jellyfin");

            migrationBuilder.DropTable(
                name: "DisplayPreferences",
                schema: "jellyfin");
        }
    }
}
