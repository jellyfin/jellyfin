using Jellyfin.Database.Testing.Conformance;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// The batch save rows of the kit on SQL Server databases that read with locks, as SQL Server creates a database unless
/// told to read from row versions. The variant exists only on SQL Server; on another engine the rows run on its own
/// databases again.
/// </summary>
public sealed class SqlServerLockingReadsBatchSaveTests : BatchSaveConformance
{
    public SqlServerLockingReadsBatchSaveTests()
        : base(SqlServerVariants.LockingReadsOrSelected)
    {
    }
}
