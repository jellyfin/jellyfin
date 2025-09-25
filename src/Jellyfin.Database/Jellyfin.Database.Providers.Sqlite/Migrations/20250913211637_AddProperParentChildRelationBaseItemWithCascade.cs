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
WITH RECURSIVE detachedParents AS (
    SELECT Id FROM BaseItems child
        WHERE
        child.ParentId IS NOT NULL
        AND
        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE child.ParentId = parent.Id)
)

SELECT * FROM detachedParents
DELETE FROM BaseItems d WHERE
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
