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
DELETE FROM BaseItems
        WHERE
        ParentId IS NOT NULL
        AND
        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
DELETE FROM BaseItems
        WHERE
        ParentId IS NOT NULL
        AND
        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
DELETE FROM BaseItems
        WHERE
        ParentId IS NOT NULL
        AND
        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
DELETE FROM BaseItems
        WHERE
        ParentId IS NOT NULL
        AND
        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
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
