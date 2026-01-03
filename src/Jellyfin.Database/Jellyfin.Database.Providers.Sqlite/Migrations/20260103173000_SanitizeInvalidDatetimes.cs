using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class SanitizeInvalidDatetimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sanitize invalid DateCreated values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET DateCreated = NULL
                WHERE DateCreated IS NOT NULL AND (
                    CAST(SUBSTR(DateCreated, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateCreated, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(DateCreated, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateCreated, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(DateCreated, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(DateCreated, 1, 4) AS INTEGER) > 2100
                );
            ");

            // Sanitize invalid DateModified values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET DateModified = NULL
                WHERE DateModified IS NOT NULL AND (
                    CAST(SUBSTR(DateModified, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateModified, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(DateModified, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateModified, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(DateModified, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(DateModified, 1, 4) AS INTEGER) > 2100
                );
            ");

            // Sanitize invalid PremiereDate values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET PremiereDate = NULL
                WHERE PremiereDate IS NOT NULL AND (
                    CAST(SUBSTR(PremiereDate, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(PremiereDate, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(PremiereDate, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(PremiereDate, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(PremiereDate, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(PremiereDate, 1, 4) AS INTEGER) > 2100
                );
            ");

            // Sanitize invalid StartDate values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET StartDate = NULL
                WHERE StartDate IS NOT NULL AND (
                    CAST(SUBSTR(StartDate, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(StartDate, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(StartDate, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(StartDate, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(StartDate, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(StartDate, 1, 4) AS INTEGER) > 2100
                );
            ");

            // Sanitize invalid EndDate values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET EndDate = NULL
                WHERE EndDate IS NOT NULL AND (
                    CAST(SUBSTR(EndDate, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(EndDate, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(EndDate, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(EndDate, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(EndDate, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(EndDate, 1, 4) AS INTEGER) > 2100
                );
            ");

            // Sanitize invalid DateLastMediaAdded values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET DateLastMediaAdded = NULL
                WHERE DateLastMediaAdded IS NOT NULL AND (
                    CAST(SUBSTR(DateLastMediaAdded, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateLastMediaAdded, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(DateLastMediaAdded, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateLastMediaAdded, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(DateLastMediaAdded, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(DateLastMediaAdded, 1, 4) AS INTEGER) > 2100
                );
            ");

            // Sanitize invalid DateLastRefreshed values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET DateLastRefreshed = NULL
                WHERE DateLastRefreshed IS NOT NULL AND (
                    CAST(SUBSTR(DateLastRefreshed, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateLastRefreshed, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(DateLastRefreshed, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateLastRefreshed, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(DateLastRefreshed, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(DateLastRefreshed, 1, 4) AS INTEGER) > 2100
                );
            ");

            // Sanitize invalid DateLastSaved values
            migrationBuilder.Sql(@"
                UPDATE BaseItems SET DateLastSaved = NULL
                WHERE DateLastSaved IS NOT NULL AND (
                    CAST(SUBSTR(DateLastSaved, 6, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateLastSaved, 6, 2) AS INTEGER) > 12 OR
                    CAST(SUBSTR(DateLastSaved, 9, 2) AS INTEGER) < 1 OR
                    CAST(SUBSTR(DateLastSaved, 9, 2) AS INTEGER) > 31 OR
                    CAST(SUBSTR(DateLastSaved, 1, 4) AS INTEGER) < 1900 OR
                    CAST(SUBSTR(DateLastSaved, 1, 4) AS INTEGER) > 2100
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration only sanitizes data - it cannot be reversed
            // Invalid datetime values have already been nullified
        }
    }
}
