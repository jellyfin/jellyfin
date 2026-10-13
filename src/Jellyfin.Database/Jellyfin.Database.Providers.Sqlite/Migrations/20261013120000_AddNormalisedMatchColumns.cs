using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalisedMatchColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameLower",
                table: "Peoples",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "CleanOriginalTitle",
                table: "BaseItems",
                type: "TEXT",
                nullable: true);

            // The index on lower("Name") is replaced by a plain index on the column that holds the lower case name.
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Peoples_NameLower\";");

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_NameLower",
                table: "Peoples",
                column: "NameLower");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dropped in SQL, as dropping a column through the builder rebuilds the whole table.
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Peoples_NameLower\";");
            migrationBuilder.Sql("ALTER TABLE \"Peoples\" DROP COLUMN \"NameLower\";");
            migrationBuilder.Sql("ALTER TABLE \"BaseItems\" DROP COLUMN \"CleanOriginalTitle\";");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Peoples_NameLower\" ON \"Peoples\" (lower(\"Name\"));");
        }
    }
}
