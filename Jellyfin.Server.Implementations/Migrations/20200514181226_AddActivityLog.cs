#pragma warning disable CS1591
#pragma warning disable SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class AddActivityLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jellyfin");

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 512, nullable: false),
                    Overview = table.Column<string>(maxLength: 512, nullable: true),
                    ShortOverview = table.Column<string>(maxLength: 512, nullable: true),
                    Type = table.Column<string>(maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<string>(maxLength: 256, nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    LogSeverity = table.Column<int>(nullable: false),
                    RowVersion = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs",
                schema: "jellyfin");
        }
    }
}
