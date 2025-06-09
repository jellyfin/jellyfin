using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class DetachUserDataInsteadOfDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserData_BaseItems_ItemId",
                table: "UserData");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetentionDate",
                table: "UserData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserData_BaseItems_ItemId",
                table: "UserData",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserData_BaseItems_ItemId",
                table: "UserData");

            migrationBuilder.DropColumn(
                name: "RetentionDate",
                table: "UserData");

            migrationBuilder.AddForeignKey(
                name: "FK_UserData_BaseItems_ItemId",
                table: "UserData",
                column: "ItemId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
