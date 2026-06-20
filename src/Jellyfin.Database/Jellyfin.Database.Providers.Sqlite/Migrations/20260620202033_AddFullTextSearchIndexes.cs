using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS BaseItems_fts USING fts5(
                    Id UNINDEXED,
                    CleanName,
                    Name,
                    OriginalTitle,
                    content='BaseItems',
                    content_rowid='rowid',
                    tokenize='porter unicode61'
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO BaseItems_fts(rowid, Id, CleanName, Name, OriginalTitle)
                SELECT rowid, Id, CleanName, Name, OriginalTitle
                FROM BaseItems;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS BaseItems_fts_insert AFTER INSERT ON BaseItems
                BEGIN
                    INSERT INTO BaseItems_fts(rowid, Id, CleanName, Name, OriginalTitle)
                    VALUES (new.rowid, new.Id, new.CleanName, new.Name, new.OriginalTitle);
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS BaseItems_fts_update AFTER UPDATE ON BaseItems
                WHEN old.CleanName IS NOT new.CleanName
                   OR old.Name IS NOT new.Name
                   OR old.OriginalTitle IS NOT new.OriginalTitle
                BEGIN
                    UPDATE BaseItems_fts
                    SET CleanName = new.CleanName,
                        Name = new.Name,
                        OriginalTitle = new.OriginalTitle
                    WHERE rowid = new.rowid;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS BaseItems_fts_delete AFTER DELETE ON BaseItems
                BEGIN
                    DELETE FROM BaseItems_fts WHERE rowid = old.rowid;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS BaseItems_fts_delete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS BaseItems_fts_update;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS BaseItems_fts_insert;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS BaseItems_fts;");
        }
    }
}
