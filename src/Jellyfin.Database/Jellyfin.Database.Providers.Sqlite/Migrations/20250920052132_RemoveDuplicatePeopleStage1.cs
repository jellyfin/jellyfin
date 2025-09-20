using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// Stage 1: Drop old index and create temporary index for duplicate People cleanup.
    /// </summary>
    public partial class RemoveDuplicatePeopleStage1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the old IX_Peoples_Name index since we'll replace it with a better one
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name",
                table: "Peoples");

            // Step 2: Update any NULL PersonType values to "Unknown" (precautionary - should be none)
            migrationBuilder.Sql(@"
                UPDATE Peoples
                SET PersonType = 'Unknown'
                WHERE PersonType IS NULL;
            ");

            // Step 3: Create index on (Name, PersonType) for performance during cleanup operations
            // This will be made unique later, but we need it now for the GROUP BY operations
            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name_PersonType_Temp",
                table: "Peoples",
                columns: new[] { "Name", "PersonType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the temporary index
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType_Temp",
                table: "Peoples");

            // Recreate the old IX_Peoples_Name index
            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name",
                table: "Peoples",
                column: "Name");
        }
    }
}
