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
            // Create FTS5 virtual table for BaseItems full-text search
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS BaseItems_fts USING fts5(
                    Id UNINDEXED,
                    Name,
                    CleanName,
                    OriginalTitle,
                    content='BaseItems',
                    content_rowid='rowid',
                    tokenize='porter unicode61'
                );
                """);

            // Populate the FTS table with existing data
            migrationBuilder.Sql(
                """
                INSERT INTO BaseItems_fts(rowid, Id, Name, CleanName, OriginalTitle)
                SELECT rowid, Id, Name, CleanName, OriginalTitle
                FROM BaseItems;
                """);

            // Create trigger to keep FTS table in sync on INSERT
            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS BaseItems_fts_insert AFTER INSERT ON BaseItems
                BEGIN
                    INSERT INTO BaseItems_fts(rowid, Id, Name, CleanName, OriginalTitle)
                    VALUES (new.rowid, new.Id, new.Name, new.CleanName, new.OriginalTitle);
                END;
                """);

            // Create trigger to keep FTS table in sync on UPDATE
            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS BaseItems_fts_update AFTER UPDATE ON BaseItems
                BEGIN
                    UPDATE BaseItems_fts
                    SET Name = new.Name,
                        CleanName = new.CleanName,
                        OriginalTitle = new.OriginalTitle
                    WHERE rowid = new.rowid;
                END;
                """);

            // Create trigger to keep FTS table in sync on DELETE
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
