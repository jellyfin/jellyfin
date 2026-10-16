using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;

namespace Jellyfin.Database.Testing;

/// <summary>
/// One SQL Server, either a container or the server named by <c>JELLYFIN_TEST_SQLSERVER_CONNECTIONSTRING</c>, on which
/// the databases of a <see cref="LeasedDatabasePool"/> live.
/// </summary>
/// <remarks>
/// Each database is created with the collation of the text columns as its default, and reads at read committed from
/// row versions, as the provider recommends an operator to set it up.
/// </remarks>
internal sealed class SqlServerServer : IPooledDatabaseServer
{
    // SQL Server 2022, the oldest supported version, with its latest security update, pinned by digest.
    internal const string DefaultImage = "mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718";

    private static readonly TimeSpan _containerStopTimeout = TimeSpan.FromSeconds(30);

    private readonly Lazy<string> _adminConnectionString;
    private IAsyncDisposable? _container;

    public SqlServerServer()
    {
        _adminConnectionString = new(StartServer);
    }

    public Version ServerVersion
    {
        get
        {
            using var connection = new SqlConnection(_adminConnectionString.Value);
            connection.Open();
            return Version.Parse(connection.ServerVersion);
        }
    }

    public string ReportedServerVersion => Convert.ToString(Scalar(_adminConnectionString.Value, "SELECT @@VERSION"), CultureInfo.InvariantCulture)!;

    public string CreateDatabase()
    {
        var name = $"jf_{Guid.NewGuid():N}";
        Execute(_adminConnectionString.Value, $"CREATE DATABASE [{name}] COLLATE {SqlServerDatabaseProvider.Collation}");
        Execute(_adminConnectionString.Value, $"ALTER DATABASE [{name}] SET READ_COMMITTED_SNAPSHOT ON");
        return name;
    }

    public DbContextOptions<JellyfinDbContext> CreateOptions(string database, IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        CreateProvider().Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-SqlServer",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = WithDatabase(database)
                }
            });
        return builder.AddInterceptors(interceptors).Options;
    }

    public IJellyfinDatabaseProvider CreateProvider() => new SqlServerDatabaseProvider(NullLogger<SqlServerDatabaseProvider>.Instance);

    public string GetSchemaFingerprint(string database)
    {
        string[] queries =
        [
            "SELECT collation_name, is_read_committed_snapshot_on, compatibility_level FROM sys.databases WHERE database_id = DB_ID()",
            "SELECT t.name, c.column_id, c.name, ty.name, c.max_length, c.precision, c.scale, c.is_nullable, c.collation_name, c.is_identity, dc.definition "
                + "FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id "
                + "LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id ORDER BY t.name, c.column_id",
            "SELECT t.name, i.name, i.type_desc, i.is_unique, i.is_primary_key, i.has_filter, i.filter_definition, ic.key_ordinal, ic.is_included_column, c.name, ic.is_descending_key "
                + "FROM sys.tables t JOIN sys.indexes i ON i.object_id = t.object_id LEFT JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id "
                + "LEFT JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id ORDER BY t.name, i.name, ic.key_ordinal, c.name",
            // Whether a key is switched off or trusts its rows is left out: the pool self-tests switch the keys off for a
            // lease, as the server has no switch for a session, and the reset turns them on again. They count it instead.
            "SELECT OBJECT_NAME(fk.parent_object_id), fk.name, OBJECT_NAME(fk.referenced_object_id), fk.delete_referential_action_desc "
                + "FROM sys.foreign_keys fk ORDER BY 1, 2",
            "SELECT name, type_desc, OBJECT_DEFINITION(object_id) FROM sys.objects WHERE is_ms_shipped = 0 AND type NOT IN ('U', 'PK', 'F', 'D', 'UQ', 'IT', 'S', 'SQ') ORDER BY name"
        ];

        using var connection = new SqlConnection(new SqlConnectionStringBuilder(WithDatabase(database)) { Pooling = false }.ConnectionString);
        connection.Open();
        var description = new StringBuilder();
        foreach (var query in queries)
        {
            using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The queries are constants.
            command.CommandText = query;
#pragma warning restore CA2100
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    description.Append(reader.IsDBNull(column) ? "\u0000" : Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture)).Append('\u001f');
                }

                description.Append('\u001e');
            }

            description.Append('\u001d');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(description.ToString())));
    }

    public void DropDatabase(string database)
    {
        using (var connection = new SqlConnection(WithDatabase(database)))
        {
            SqlConnection.ClearPool(connection);
        }

        Execute(
            _adminConnectionString.Value,
            $"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END");
    }

    public void Dispose()
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

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The statements are built from generated database names.")]
    private static void Execute(string connectionString, string commandText)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The statements are constants.")]
    private static object? Scalar(string connectionString, string commandText)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand(commandText, connection);
        return command.ExecuteScalar();
    }

    private string WithDatabase(string name)
    {
        return new SqlConnectionStringBuilder(_adminConnectionString.Value) { InitialCatalog = name }.ConnectionString;
    }

    private string StartServer()
    {
        var connectionString = TestDatabaseEnvironment.GetProviderSetting(TestDbProvider.SqlServer, "CONNECTIONSTRING");
        if (connectionString is not null)
        {
            return connectionString;
        }

        var image = TestDatabaseEnvironment.GetProviderSetting(TestDbProvider.SqlServer, "IMAGE") ?? DefaultImage;
        var container = new MsSqlBuilder(image)
            .WithEnvironment("MSSQL_PID", "Developer")
            .Build();
        container.StartAsync().GetAwaiter().GetResult();
        _container = container;
        return container.GetConnectionString();
    }
}
