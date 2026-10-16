using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.MySql.Migrations
{
    /// <inheritdoc />
    public partial class IndexItemPathByHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only PostgreSQL changes the index on the path of an item, to a hash index.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only PostgreSQL changes the index on the path of an item, to a hash index.
        }
    }
}
