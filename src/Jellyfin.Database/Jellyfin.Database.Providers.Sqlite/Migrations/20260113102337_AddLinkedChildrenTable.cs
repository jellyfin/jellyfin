using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedChildrenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinkedChildren",
                columns: table => new
                {
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChildId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChildType = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedChildren", x => new { x.ParentId, x.ChildId });
                    table.ForeignKey(
                        name: "FK_LinkedChildren_BaseItems_ChildId",
                        column: x => x.ChildId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LinkedChildren_BaseItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ChildId",
                table: "LinkedChildren",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ChildId_ChildType",
                table: "LinkedChildren",
                columns: new[] { "ChildId", "ChildType" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ParentId",
                table: "LinkedChildren",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ParentId_ChildType",
                table: "LinkedChildren",
                columns: new[] { "ParentId", "ChildType" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ParentId_SortOrder",
                table: "LinkedChildren",
                columns: new[] { "ParentId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-populate LinkedChildren data back into the JSON Data column before dropping the table
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET Data = CASE
                      WHEN Data IS NULL OR Data = '' THEN
                          json_object('LinkedChildren', (
                              SELECT json_group_array(
                                  json_object(
                                      'Path', Child.Path,
                                      'Type', CASE LC.ChildType
                                          WHEN 0 THEN 'Manual'
                                          WHEN 1 THEN 'Shortcut'
                                          ELSE 'Manual'
                                      END,
                                      'ItemId', LOWER(REPLACE(LC.ChildId, '-', ''))
                                  )
                              )
                              FROM LinkedChildren LC
                              INNER JOIN BaseItems Child ON LC.ChildId = Child.Id
                              WHERE LC.ParentId = BaseItems.Id
                              ORDER BY LC.SortOrder
                          ))
                      ELSE
                          json_set(
                              Data,
                              '$.LinkedChildren',
                              (
                                  SELECT json_group_array(
                                      json_object(
                                          'Path', Child.Path,
                                          'Type', CASE LC.ChildType
                                              WHEN 0 THEN 'Manual'
                                              WHEN 1 THEN 'Shortcut'
                                              ELSE 'Manual'
                                          END,
                                          'ItemId', LOWER(REPLACE(LC.ChildId, '-', ''))
                                      )
                                  )
                                  FROM LinkedChildren LC
                                  INNER JOIN BaseItems Child ON LC.ChildId = Child.Id
                                  WHERE LC.ParentId = BaseItems.Id
                                  ORDER BY LC.SortOrder
                              )
                          )
                  END
                  WHERE EXISTS (
                      SELECT 1
                      FROM LinkedChildren LC
                      WHERE LC.ParentId = BaseItems.Id
                  )");

            migrationBuilder.DropTable(
                name: "LinkedChildren");

            migrationBuilder.Sql(
                @"DELETE FROM __EFMigrationsHistory
                  WHERE MigrationId = '20260113120000_MigrateLinkedChildren'");
        }
    }
}
