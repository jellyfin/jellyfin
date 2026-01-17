using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOwnerIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize OwnerId to uppercase GUID format
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET OwnerId = UPPER(OwnerId)
                  WHERE OwnerId IS NOT NULL");

            // Clear invalid OwnerId values (not 36 characters = not a valid GUID)
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET OwnerId = null
                  WHERE OwnerId IS NOT NULL AND length(OwnerId) != 36");

            // Clear placeholder/empty GUIDs
            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "OwnerId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000000"),
                column: "OwnerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "OwnerId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "OwnerId",
                value: null);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseItemEntityId",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "BaseItemEntityId", "Name", "OwnerId" },
                values: new object[] { null, "This is a placeholder item for UserData that has been detached from its original item", null });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_BaseItemEntityId",
                table: "BaseItems",
                column: "BaseItemEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ExtraType",
                table: "BaseItems",
                column: "ExtraType");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ExtraType_OwnerId",
                table: "BaseItems",
                columns: new[] { "ExtraType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_OwnerId",
                table: "BaseItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_IsFolder_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "IsFolder", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_MediaType_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "MediaType", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Type", "IsVirtualItem", "DateCreated" });

            migrationBuilder.AddForeignKey(
                name: "FK_BaseItems_BaseItems_BaseItemEntityId",
                table: "BaseItems",
                column: "BaseItemEntityId",
                principalTable: "BaseItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "OwnerId",
                value: null);

            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET OwnerId = LOWER(OwnerId)
                  WHERE OwnerId IS NOT NULL");

            migrationBuilder.DropForeignKey(
                name: "FK_BaseItems_BaseItems_BaseItemEntityId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_BaseItemEntityId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_ExtraType",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_ExtraType_OwnerId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_OwnerId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_TopParentId_IsFolder_IsVirtualItem_DateCreated",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_TopParentId_MediaType_IsVirtualItem_DateCreated",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem_DateCreated",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "BaseItemEntityId",
                table: "BaseItems");

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "Name", "OwnerId" },
                values: new object[] { "This is a placeholder item for UserData that has been detacted from its original item", null });
        }
    }
}
