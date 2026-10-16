using Jellyfin.Database.Testing;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// The session tests on databases that read from row versions, as the provider recommends.
/// </summary>
public sealed class SqlServerSessionTests : SqlServerSessionTestsBase
{
    public SqlServerSessionTests()
        : base(TestDatabaseEnvironment.Backend, readsFromRowVersions: true)
    {
    }
}
