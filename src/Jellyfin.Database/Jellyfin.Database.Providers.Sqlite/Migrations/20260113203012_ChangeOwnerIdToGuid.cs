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
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "OrphanedBaseItemIds";
                CREATE TEMPORARY TABLE "OrphanedBaseItemIds" ("Id" TEXT NOT NULL PRIMARY KEY);

                INSERT INTO "OrphanedBaseItemIds" ("Id")
                WITH RECURSIVE Orphan ("Id") AS (
                    SELECT Child."Id"
                    FROM "BaseItems" AS Child
                    WHERE Child."ParentId" IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM "BaseItems" AS Parent WHERE Parent."Id" = Child."ParentId")
                    UNION
                    SELECT Descendant."Id"
                    FROM "BaseItems" AS Descendant
                    INNER JOIN Orphan ON Descendant."ParentId" = Orphan."Id"
                )
                SELECT "Id" FROM Orphan;

                -- Keep the play state of the doomed items the way ItemPersistenceService does when it
                -- deletes an item: reattach it to the placeholder item instead of letting the
                -- FK_UserData_BaseItems_ItemId cascade wipe it. The placeholder can only hold one row
                -- per (UserId, CustomDataKey), so resolve collisions before repointing anything.
                DELETE FROM "UserData"
                WHERE "ItemId" = '00000000-0000-0000-0000-000000000001'
                  AND EXISTS (
                      SELECT 1
                      FROM "UserData" AS Doomed
                      INNER JOIN "OrphanedBaseItemIds" AS Orphan ON Orphan."Id" = Doomed."ItemId"
                      WHERE Doomed."UserId" = "UserData"."UserId"
                        AND Doomed."CustomDataKey" = "UserData"."CustomDataKey");

                DELETE FROM "UserData"
                WHERE "ItemId" IN (SELECT "Id" FROM "OrphanedBaseItemIds")
                  AND "rowid" NOT IN (
                      SELECT MIN("rowid")
                      FROM "UserData"
                      WHERE "ItemId" IN (SELECT "Id" FROM "OrphanedBaseItemIds")
                      GROUP BY "UserId", "CustomDataKey");

                UPDATE "UserData"
                SET "ItemId" = '00000000-0000-0000-0000-000000000001',
                    "RetentionDate" = datetime('now')
                WHERE "ItemId" IN (SELECT "Id" FROM "OrphanedBaseItemIds");

                -- FK_LinkedChildren_BaseItems_{ParentId,ChildId} are NO ACTION, so these rows have to
                -- go by hand or the delete below fails on them.
                DELETE FROM "LinkedChildren"
                WHERE "ParentId" IN (SELECT "Id" FROM "OrphanedBaseItemIds")
                   OR "ChildId" IN (SELECT "Id" FROM "OrphanedBaseItemIds");

                DELETE FROM "BaseItems" WHERE "Id" IN (SELECT "Id" FROM "OrphanedBaseItemIds");

                DROP TABLE "OrphanedBaseItemIds";
                """);

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
