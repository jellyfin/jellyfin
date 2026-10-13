using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Postgres.Migrations
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
                type: "text",
                nullable: false,
                defaultValue: string.Empty,
                collation: "C");

            // The default only fills the rows that exist, the model has none.
            migrationBuilder.Sql("""ALTER TABLE "Peoples" ALTER COLUMN "NameLower" DROP DEFAULT;""");

            migrationBuilder.AddColumn<string>(
                name: "CleanOriginalTitle",
                table: "BaseItems",
                type: "text",
                nullable: true,
                collation: "C");

            // The index on lower("Name") is replaced by a plain index on the column that holds the lower case name.
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Peoples_NameLower";""");

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_NameLower",
                table: "Peoples",
                column: "NameLower");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Peoples_NameLower",
                table: "Peoples");

            migrationBuilder.DropColumn(
                name: "NameLower",
                table: "Peoples");

            migrationBuilder.DropColumn(
                name: "CleanOriginalTitle",
                table: "BaseItems");

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Peoples_NameLower" ON "Peoples" (lower("Name"));""");
        }
    }
}
