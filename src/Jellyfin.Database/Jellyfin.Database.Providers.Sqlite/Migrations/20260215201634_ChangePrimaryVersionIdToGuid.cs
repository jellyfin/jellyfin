using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ChangePrimaryVersionIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert "N" format (32 chars, no hyphens) to standard GUID format (36 chars with hyphens)
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET PrimaryVersionId = UPPER(
                    SUBSTR(PrimaryVersionId,1,8)||'-'||
                    SUBSTR(PrimaryVersionId,9,4)||'-'||
                    SUBSTR(PrimaryVersionId,13,4)||'-'||
                    SUBSTR(PrimaryVersionId,17,4)||'-'||
                    SUBSTR(PrimaryVersionId,21,12))
                  WHERE PrimaryVersionId IS NOT NULL AND LENGTH(PrimaryVersionId) = 32");

            // Normalize existing standard-format values to uppercase
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET PrimaryVersionId = UPPER(PrimaryVersionId)
                  WHERE PrimaryVersionId IS NOT NULL");

            // Clear invalid values (not 36 characters = not a valid GUID)
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET PrimaryVersionId = NULL
                  WHERE PrimaryVersionId IS NOT NULL AND LENGTH(PrimaryVersionId) != 36");

            // Clear placeholder/empty GUIDs
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET PrimaryVersionId = NULL
                  WHERE PrimaryVersionId = '00000000-0000-0000-0000-000000000000'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert standard GUID format back to "N" format (remove hyphens, lowercase)
            migrationBuilder.Sql(
                @"UPDATE BaseItems
                  SET PrimaryVersionId = LOWER(REPLACE(PrimaryVersionId, '-', ''))
                  WHERE PrimaryVersionId IS NOT NULL");
        }
    }
}
