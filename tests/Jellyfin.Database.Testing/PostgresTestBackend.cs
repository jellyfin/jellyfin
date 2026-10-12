using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Creates a PostgreSQL database for every test instance. All of them live on one server per test process, either a
/// container or the server named by <c>JELLYFIN_TEST_POSTGRES_CONNECTIONSTRING</c>. The schema is built once per
/// <see cref="TestSchemaSource"/> into a template database that every instance is copied from.
/// </summary>
public sealed class PostgresTestBackend : ITestDatabaseBackend
{
    private const string DefaultImage = "postgres:16.15-trixie";

    private static readonly TimeSpan _containerStopTimeout = TimeSpan.FromSeconds(30);

    private readonly string _runId = Guid.NewGuid().ToString("N");
    private readonly Lazy<string> _adminConnectionString;
    private readonly ConcurrentDictionary<TestSchemaSource, Lazy<string>> _templates = new();
    private readonly ConcurrentDictionary<string, byte> _databases = new();

    // Copying a template needs exclusive access to it, so only one database can be created at a time.
    private readonly Lock _createLock = new();

    private PostgreSqlContainer? _container;

    private PostgresTestBackend()
    {
        _adminConnectionString = new(StartServer);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DropRemainingDatabases();
    }

    public static PostgresTestBackend Instance { get; } = new();

    public TestDbProvider Provider => TestDbProvider.Postgres;

    public Version ServerVersion => ReadServerVersion(_adminConnectionString.Value);

    public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors)
    {
        var template = _templates.GetOrAdd(source, key => new Lazy<string>(() => CreateTemplate(key))).Value;
        var name = $"jf_{Guid.NewGuid():N}";

        _databases[name] = 0;
        lock (_createLock)
        {
            Execute(_adminConnectionString.Value, $"CREATE DATABASE \"{name}\" TEMPLATE \"{template}\" STRATEGY FILE_COPY");
        }

        var options = CreateOptions(WithDatabase(name), interceptors);

        return new TestDatabase(options, CreateProvider, () => DropDatabase(name));
    }

    private static IJellyfinDatabaseProvider CreateProvider() => new PostgresDatabaseProvider(NullLogger<PostgresDatabaseProvider>.Instance);

    private string StartServer()
    {
        var connectionString = TestDatabaseEnvironment.GetProviderSetting(TestDbProvider.Postgres, "CONNECTIONSTRING");
        if (connectionString is not null)
        {
            return connectionString;
        }

        _container = new PostgreSqlBuilder(TestDatabaseEnvironment.GetProviderSetting(TestDbProvider.Postgres, "IMAGE") ?? DefaultImage)
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithCommand("-c", "fsync=off", "-c", "synchronous_commit=off", "-c", "full_page_writes=off", "-c", "max_connections=300")
            .Build();
        _container.StartAsync().GetAwaiter().GetResult();
        return _container.GetConnectionString();
    }

    private static DbContextOptions<JellyfinDbContext> CreateOptions(string connectionString, IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        CreateProvider().Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-PostgreSQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = connectionString
                }
            });
        return builder.AddInterceptors(interceptors).Options;
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The statements are built from generated database names, which cannot be passed as parameters.")]
    private static void Execute(string connectionString, string commandText)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    private static Version ReadServerVersion(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = new NpgsqlCommand("SHOW server_version_num", connection);
        var number = int.Parse(Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
        return new Version(number / 10000, number % 10000);
    }

    private string WithDatabase(string name)
    {
        return new NpgsqlConnectionStringBuilder(_adminConnectionString.Value) { Database = name }.ConnectionString;
    }

    private void ClearPool(string database)
    {
        // The provider adds settings to the connection string, and connections are pooled per connection string.
        var options = CreateOptions(WithDatabase(database), []);
        using var connection = new NpgsqlConnection(RelationalOptionsExtension.Extract(options).ConnectionString);
        NpgsqlConnection.ClearPool(connection);
    }

    private string CreateTemplate(TestSchemaSource source)
    {
        var name = $"jf_template_{source.ToString().ToLowerInvariant()}_{_runId}";
        _databases[name] = 0;
        Execute(_adminConnectionString.Value, $"CREATE DATABASE \"{name}\"");

        var connectionString = WithDatabase(name);
        source.Apply(CreateOptions(connectionString, []), CreateProvider());

        // A pooled connection would keep the template in use, and a database cannot be copied while it is.
        ClearPool(name);
        return name;
    }

    private void DropDatabase(string name)
    {
        ClearPool(name);
        Execute(_adminConnectionString.Value, $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
        _databases.TryRemove(name, out _);
    }

    private void DropRemainingDatabases()
    {
        if (!_adminConnectionString.IsValueCreated)
        {
            return;
        }

        var failures = new List<string>();
        foreach (var name in _databases.Keys)
        {
            try
            {
                DropDatabase(name);
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Console.Error.WriteLine($"Could not drop {failures.Count} test database(s):{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }

        StopContainer();
    }

    private void StopContainer()
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            if (!_container.DisposeAsync().AsTask().Wait(_containerStopTimeout))
            {
                Console.Error.WriteLine($"The test database container did not stop within {_containerStopTimeout.TotalSeconds:0} seconds.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not stop the test database container: {ex.Message}");
        }
    }
}
