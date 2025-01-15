using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class FixAncestorIdConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AncestorIds_BaseItems_BaseItemEntityId",
                table: "AncestorIds");

            migrationBuilder.DropIndex(
                name: "IX_AncestorIds_BaseItemEntityId",
                table: "AncestorIds");

            migrationBuilder.DropColumn(
                name: "BaseItemEntityId",
                table: "AncestorIds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BaseItemEntityId",
                table: "AncestorIds",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_BaseItemEntityId",
                table: "AncestorIds",
                column: "BaseItemEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AncestorIds_BaseItems_BaseItemEntityId",
                table: "AncestorIds",
                column: "BaseItemEntityId",
                principalTable: "BaseItems",
                principalColumn: "Id");
        }
    }
}
