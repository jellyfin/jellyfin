namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Describes how the MySQL and MariaDB migrations relate to the migrations of the SQLite provider.
/// </summary>
/// <remarks>
/// MySQL and MariaDB start from one migration that creates the whole schema. Every migration after it carries the
/// identifier of the SQLite migration it mirrors, so all providers apply schema changes in the same order.
/// </remarks>
internal static class MySqlMigrationBaseline
{
    /// <summary>
    /// The identifier of the migration that creates the schema. It sorts before every other migration, so it always runs
    /// first.
    /// </summary>
    public const string MigrationId = "20200101000000_InitialCreate";

    /// <summary>
    /// The last SQLite migration whose schema change is already part of the baseline. SQLite migrations after this one
    /// need a MySQL and MariaDB migration of their own.
    /// </summary>
    public const string CoversThroughSqliteMigration = "20261013120000_AddNormalisedMatchColumns";
}
