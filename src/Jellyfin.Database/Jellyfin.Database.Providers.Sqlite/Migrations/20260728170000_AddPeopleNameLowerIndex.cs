using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleNameLowerIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Expression index, so it cannot be declared on the entity type. /Persons collapses the
            // one-row-per-(Name, PersonType) table to one row per lowercased name; without this index
            // that dedup scans and groups the whole table on every request.
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Peoples_NameLower\" ON \"Peoples\" (lower(\"Name\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Peoples_NameLower\";");
        }
    }
}
