using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Database.Providers.Postgres.Migrations;

/// <summary>
/// The part of the baseline that the EF tools cannot generate from the model. When the baseline is generated again,
/// the two calls into this file have to be added to <c>Up</c> and <c>Down</c> again.
/// </summary>
public partial class InitialCreate
{
    private static void CreateHandWrittenObjects(MigrationBuilder migrationBuilder)
    {
        // Looks up people by their lowercased name. The model has no index on an expression.
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Peoples_NameLower" ON "Peoples" (lower("Name"));""");
        migrationBuilder.Sql(PostgresUuidAggregate.CreateSql);
    }

    private static void DropHandWrittenObjects(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PostgresUuidAggregate.DropSql);
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Peoples_NameLower";""");
    }
}
