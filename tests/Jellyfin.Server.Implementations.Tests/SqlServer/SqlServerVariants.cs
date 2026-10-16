using Jellyfin.Database.Testing;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// The backends of the variants of SQL Server databases that some tests run on as well.
/// </summary>
internal static class SqlServerVariants
{
    /// <summary>
    /// Gets the backend of databases that read with locks on a SQL Server run, and the backend of the run otherwise.
    /// </summary>
    public static ITestDatabaseBackend LockingReadsOrSelected
        => TestDatabaseEnvironment.Provider == TestDbProvider.SqlServer ? SqlServerTestBackend.LockingReads : TestDatabaseEnvironment.Backend;
}
