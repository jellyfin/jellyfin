using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddProperParentChildRelationBaseItemWithCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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
                DELETE FROM "BaseItems" WHERE "Id" IN (SELECT "Id" FROM Orphan);
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseItems_BaseItems_ParentId",
                table: "BaseItems",
                column: "ParentId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseItems_BaseItems_ParentId",
                table: "BaseItems");
        }
    }
}
