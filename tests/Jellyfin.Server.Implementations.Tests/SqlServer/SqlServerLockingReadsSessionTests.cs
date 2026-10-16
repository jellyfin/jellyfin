namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// The session tests on databases that read with locks, as SQL Server creates them unless told otherwise.
/// </summary>
public sealed class SqlServerLockingReadsSessionTests : SqlServerSessionTestsBase
{
    public SqlServerLockingReadsSessionTests()
        : base(SqlServerVariants.LockingReadsOrSelected, readsFromRowVersions: false)
    {
    }
}
