using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Creates the databases of a MySQL or MariaDB server through a <see cref="LeasedDatabasePool"/>, because neither server
/// can copy a database. All of them live on one server per test process, either a container or the server named by the
/// connection string setting of the provider.
/// </summary>
public abstract class MySqlFamilyTestBackend : ITestDatabaseBackend, IDisposable
{
    private readonly LeasedDatabasePool _pool;
    private readonly Func<Version> _readServerVersion;
    private readonly Func<string> _readReportedServerVersion;

    private protected MySqlFamilyTestBackend(TestDbProvider provider)
    {
        var server = new MySqlFamilyServer(provider);
        Provider = provider;
        _readServerVersion = () => server.ServerVersion;
        _readReportedServerVersion = () => server.ReportedServerVersion;
        _pool = new LeasedDatabasePool(server);
    }

    public TestDbProvider Provider { get; }

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

    /// <summary>
    /// Drops every database and stops the container, if there is one. The pool does the same when the process exits.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pool.Dispose();
        }
    }
}
