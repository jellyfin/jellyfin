using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// Stage 3: Create unique index after removing duplicate People records.
    /// </summary>
    public partial class RemoveDuplicatePeopleStage3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the temporary index
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType_Temp",
                table: "Peoples");

            // Step 2: Make PersonType column NOT NULL (SQLite doesn't support ALTER COLUMN directly)
            // We need to recreate the table
            migrationBuilder.Sql(@"
                -- Create temporary table with NOT NULL constraint
                CREATE TABLE Peoples_temp (
                    Id TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    PersonType TEXT NOT NULL DEFAULT 'Unknown',
                    CONSTRAINT PK_Peoples PRIMARY KEY (Id)
                );
            ");

            migrationBuilder.Sql(@"
                -- Copy data from old table
                INSERT INTO Peoples_temp (Id, Name, PersonType)
                SELECT Id, Name, COALESCE(PersonType, 'Unknown')
                FROM Peoples;
            ");

            migrationBuilder.Sql(@"
                -- Drop old table and recreate foreign key constraints
                PRAGMA foreign_keys=off;
                DROP TABLE Peoples;
                ALTER TABLE Peoples_temp RENAME TO Peoples;
                PRAGMA foreign_keys=on;
            ");

            // Step 3: Create unique index on Name, PersonType (replaces the old IX_Peoples_Name)
            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples",
                columns: new[] { "Name", "PersonType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples");
        }
    }
}
