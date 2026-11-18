using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicatePlaylistChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows that predate the composite (ParentId, SortOrder) primary key stored a null SortOrder
            // (e.g. BoxSet and Collection children). Assign each such row a stable 0-based position within
            // its parent so the rows stay unique once SortOrder becomes part of the primary key; otherwise
            // they would all collapse to the column default (0) and collide during the table rebuild.
            migrationBuilder.Sql(
                @"UPDATE ""LinkedChildren""
                  SET ""SortOrder"" = (
                      SELECT COUNT(*)
                      FROM ""LinkedChildren"" AS lc2
                      WHERE lc2.""ParentId"" = ""LinkedChildren"".""ParentId""
                        AND lc2.""rowid"" < ""LinkedChildren"".""rowid""
                  )
                  WHERE ""SortOrder"" IS NULL;");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LinkedChildren",
                table: "LinkedChildren");

            migrationBuilder.DropIndex(
                name: "IX_LinkedChildren_ParentId_SortOrder",
                table: "LinkedChildren");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "LinkedChildren",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LinkedChildren",
                table: "LinkedChildren",
                columns: new[] { "ParentId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The (ParentId, ChildId) primary key cannot represent the same child more than once per
            // parent. Drop any duplicate entries (keeping the first by SortOrder) that may have been
            // created while duplicates were allowed, so the old key can be restored. This is lossy by
            // nature — duplicate playlist entries cannot survive a downgrade.
            migrationBuilder.Sql(
                @"DELETE FROM ""LinkedChildren""
                  WHERE ""rowid"" NOT IN (
                      SELECT MIN(""rowid"")
                      FROM ""LinkedChildren""
                      GROUP BY ""ParentId"", ""ChildId""
                  );");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LinkedChildren",
                table: "LinkedChildren");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "LinkedChildren",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LinkedChildren",
                table: "LinkedChildren",
                columns: new[] { "ParentId", "ChildId" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ParentId_SortOrder",
                table: "LinkedChildren",
                columns: new[] { "ParentId", "SortOrder" });
        }
    }
}
