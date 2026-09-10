using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS "SmartCollections" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SmartCollections" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "UserId" TEXT NOT NULL,
    "FiltersJson" TEXT NOT NULL,
    "SortBy" TEXT NULL,
    "SortOrder" INTEGER NULL,
    "Limit" INTEGER NOT NULL DEFAULT 50,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);
""");

            migrationBuilder.Sql("""
CREATE INDEX IF NOT EXISTS "IX_SmartCollections_UserId"
ON "SmartCollections" ("UserId");
""");

            migrationBuilder.Sql("""
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SmartCollections_UserId_Name"
ON "SmartCollections" ("UserId", "Name");
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Safe rollback if the table exists, otherwise ignore
            migrationBuilder.DropTable(name: "SmartCollections");
        }
    }
}
