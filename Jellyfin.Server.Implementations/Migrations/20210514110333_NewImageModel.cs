#pragma warning disable CS1591
#pragma warning disable SA1601

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Migrations
{
    public partial class NewImageModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageInfos",
                schema: "jellyfin");

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                schema: "jellyfin",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Images",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 65535, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Blurhash = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileCreationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FileModificationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ImageId",
                schema: "jellyfin",
                table: "Users",
                column: "ImageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Images_ImageId",
                schema: "jellyfin",
                table: "Users",
                column: "ImageId",
                principalSchema: "jellyfin",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Images_ImageId",
                schema: "jellyfin",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Images",
                schema: "jellyfin");

            migrationBuilder.DropIndex(
                name: "IX_Users_ImageId",
                schema: "jellyfin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ImageId",
                schema: "jellyfin",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "ImageInfos",
                schema: "jellyfin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageInfos_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "jellyfin",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageInfos_UserId",
                schema: "jellyfin",
                table: "ImageInfos",
                column: "UserId",
                unique: true);
        }
    }
}
