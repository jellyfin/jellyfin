using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Creates SQL Server databases through a <see cref="LeasedDatabasePool"/>, because the server cannot copy a database
/// cheaply. All of them live on one server per test process, either a container or the server named by
/// <c>JELLYFIN_TEST_SQLSERVER_CONNECTIONSTRING</c>.
/// </summary>
public sealed class SqlServerTestBackend : ITestDatabaseBackend, IDisposable
{
    private readonly LeasedDatabasePool _pool;
    private readonly Func<Version> _readServerVersion;
    private readonly Func<string> _readReportedServerVersion;

    private SqlServerTestBackend()
    {
        // The pool owns the server and disposes it.
        var server = new SqlServerServer();
        _readServerVersion = () => server.ServerVersion;
        _readReportedServerVersion = () => server.ReportedServerVersion;
        _pool = new LeasedDatabasePool(server);
    }

    public static SqlServerTestBackend Instance { get; } = new();

    public TestDbProvider Provider => TestDbProvider.SqlServer;

    public Version ServerVersion => _readServerVersion();

    public string ReportedServerVersion => _readReportedServerVersion();

    public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors, bool fresh = false)
    {
        return _pool.Lease(source, interceptors, fresh);
    }

    public TestDatabase CreateEmpty()
    {
        return _pool.CreateEmpty();
    }

    public void Dispose()
    {
        _pool.Dispose();
    }
}
