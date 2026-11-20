using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Base fixture for the tests that only make sense against SQLite, e.g. because they look at the SQL it is sent
/// or at SQLite's own error codes. Tests that hold for every provider derive from <see cref="DbTestFixture"/>.
/// </summary>
public abstract class SqliteDbTestFixture : DbTestFixture
{
    protected SqliteDbTestFixture(params IInterceptor[] interceptors)
        : base(new SqliteTestDatabase(), interceptors)
    {
    }
}
