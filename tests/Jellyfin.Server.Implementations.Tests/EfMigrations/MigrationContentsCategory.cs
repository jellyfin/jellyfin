namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

/// <summary>
/// Why the migrations of a change may differ between providers (ADR-0001 W2).
/// A fix for a bug in current code is deliberately not a category: it has to reach every provider.
/// </summary>
internal enum MigrationContentsCategory
{
    /// <summary>
    /// SQL on one provider that does what the operations of the others do, written by hand because the operation
    /// does not suit that engine, for example because it would rebuild a SQLite table.
    /// </summary>
    EquivalentSql,

    /// <summary>
    /// A step or an object that only one engine needs or supports.
    /// </summary>
    EngineSpecific,

    /// <summary>
    /// A fix for data that only databases created by old versions on SQLite can hold.
    /// </summary>
    LegacySqliteData,

    /// <summary>
    /// The baselines of two providers share an id, but cover a different number of SQLite migrations,
    /// so one baseline contains what the other provider adds in a later migration.
    /// </summary>
    BaselineCoverage
}
