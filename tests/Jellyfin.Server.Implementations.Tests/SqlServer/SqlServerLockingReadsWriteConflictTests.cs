using Jellyfin.Database.Testing.Conformance;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// The write conflict rows of the kit on SQL Server databases that read with locks, as SQL Server creates a database unless
/// told to read from row versions: concurrent writers then meet blocking, deadlocks and lock time-outs instead of row
/// versions. The variant exists only on SQL Server; on another engine the rows run on its own databases again.
/// </summary>
public sealed class SqlServerLockingReadsWriteConflictTests : WriteConflictConformance
{
    public SqlServerLockingReadsWriteConflictTests()
        : base(SqlServerVariants.LockingReadsOrSelected)
    {
    }
}
