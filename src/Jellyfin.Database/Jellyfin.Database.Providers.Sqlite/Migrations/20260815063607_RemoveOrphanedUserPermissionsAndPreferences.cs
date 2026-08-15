using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanedUserPermissionsAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every user update used to detach the old rows instead of deleting them, because the
            // foreign key is nullable. Nothing else writes a permission or preference without a user,
            // so a null UserId identifies exactly the rows that leaked. Delete before dropping the
            // columns: SQLite rewrites the table for a DROP COLUMN and these tables can hold millions
            // of dead rows.
            migrationBuilder.Sql("DELETE FROM Permissions WHERE UserId IS NULL;");
            migrationBuilder.Sql("DELETE FROM Preferences WHERE UserId IS NULL;");

            // Dead columns. These held the foreign key before it was renamed to UserId and the values
            // copied across; a ForeignKey attribute on the User navigations kept them in the model as
            // shadow properties that nothing has written since. Rows old enough to predate the rename
            // still carry a value, but it only ever duplicates UserId, so nothing is lost by dropping
            // them. Deliberately after the deletes, which is where the rest of those rows go.
            migrationBuilder.DropColumn(
                name: "Preference_Preferences_Guid",
                table: "Preferences");

            migrationBuilder.DropColumn(
                name: "Permission_Permissions_Guid",
                table: "Permissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Preference_Preferences_Guid",
                table: "Preferences",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Permission_Permissions_Guid",
                table: "Permissions",
                type: "TEXT",
                nullable: true);
        }
    }
}
