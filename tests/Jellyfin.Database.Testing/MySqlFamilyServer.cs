using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Testcontainers.MariaDb;
using Testcontainers.MySql;

namespace Jellyfin.Database.Testing;

/// <summary>
/// One MySQL or MariaDB server, either a container or the server named by the connection string setting of the
/// provider, on which the databases of a <see cref="LeasedDatabasePool"/> live.
/// </summary>
internal sealed class MySqlFamilyServer : IPooledDatabaseServer
{
    private const string MySqlImage = "mysql:8.4.11";
    private const string MariaDbImage = "mariadb:11.4.13";
    private const string RootPassword = "jellyfin-test";
    private const string Collation = "utf8mb4_0900_bin";

    // The tests send long lists of ids, which are sent as one parameter for each value.
    private const string MaxAllowedPacket = "--max-allowed-packet=67108864";

    private static readonly TimeSpan _containerStopTimeout = TimeSpan.FromSeconds(30);

    private readonly TestDbProvider _provider;
    private readonly Lazy<string> _adminConnectionString;
    private IAsyncDisposable? _container;

    public MySqlFamilyServer(TestDbProvider provider)
    {
        if (provider is not (TestDbProvider.MySql or TestDbProvider.MariaDb))
        {
            throw new ArgumentOutOfRangeException(nameof(provider), provider, "The server is a MySQL or MariaDB server.");
        }

        _provider = provider;
        _adminConnectionString = new(StartServer);
    }

    public Version ServerVersion => ReadServerVersion(_adminConnectionString.Value);

    /// <summary>
    /// Gets the version as the server reports it to <c>VERSION()</c>, which names MariaDB on a MariaDB server.
    /// </summary>
    public string ReportedServerVersion => ReadReportedServerVersion(_adminConnectionString.Value);

    public string CreateDatabase()
    {
        var name = $"jf_{Guid.NewGuid():N}";
        Execute(_adminConnectionString.Value, $"CREATE DATABASE `{name}` CHARACTER SET utf8mb4 COLLATE {Collation}");
        return name;
    }

    public DbContextOptions<JellyfinDbContext> CreateOptions(string database, IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        CreateProvider().Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-MySQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = WithDatabase(database),
                    Options = [new CustomDatabaseOption { Key = "server-version", Value = _provider == TestDbProvider.MariaDb ? "11.4.7-mariadb" : "8.4-mysql" }]
                }
            });
        return builder.AddInterceptors(interceptors).Options;
    }

    public IJellyfinDatabaseProvider CreateProvider() => new MySqlDatabaseProvider(NullLogger<MySqlDatabaseProvider>.Instance);

    public string GetSchemaFingerprint(string database)
    {
        // The statistics of a table, such as its row count and its next auto increment value, change with its contents,
        // so only what describes the structure is read.
        string[] queries =
        [
            "SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = DATABASE()",
            "SELECT TABLE_NAME, TABLE_TYPE, ENGINE, TABLE_COLLATION, ROW_FORMAT FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME",
            "SELECT TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, COLUMN_TYPE, IS_NULLABLE, COLLATION_NAME, COLUMN_DEFAULT, EXTRA, GENERATION_EXPRESSION FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME, ORDINAL_POSITION",
            "SELECT TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX, COLUMN_NAME, NON_UNIQUE, SUB_PART, INDEX_TYPE, COLLATION FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX",
            "SELECT TABLE_NAME, CONSTRAINT_NAME, CONSTRAINT_TYPE FROM information_schema.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME, CONSTRAINT_NAME",
            "SELECT TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION",
            "SELECT TABLE_NAME, CONSTRAINT_NAME, UPDATE_RULE, DELETE_RULE FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() ORDER BY TABLE_NAME, CONSTRAINT_NAME",
            "SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_TIMING, EVENT_MANIPULATION, ACTION_STATEMENT FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE() ORDER BY TRIGGER_NAME",
            "SELECT ROUTINE_NAME, ROUTINE_TYPE, ROUTINE_DEFINITION FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = DATABASE() ORDER BY ROUTINE_NAME",
            "SELECT TABLE_NAME, VIEW_DEFINITION FROM information_schema.VIEWS WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME"
        ];

        using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(WithDatabase(database)) { Pooling = false }.ConnectionString);
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
                    description.Append(reader.IsDBNull(column) ? "\u0000" : Text(reader.GetValue(column))).Append('\u001f');
                }

                description.Append('\u001e');
            }

            description.Append('\u001d');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(description.ToString())));
    }

    public void DropDatabase(string database)
    {
        // The provider adds settings to the connection string, and connections are pooled per connection string.
        var options = CreateOptions(database, []);
        using (var connection = new MySqlConnection(RelationalOptionsExtension.Extract(options).ConnectionString))
        {
            MySqlConnection.ClearPool(connection);
        }

        Execute(_adminConnectionString.Value, $"DROP DATABASE IF EXISTS `{database}`");
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

    private static string Text(object value)
    {
        return value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The statements are built from generated database names, which cannot be passed as parameters.")]
    private static void Execute(string connectionString, string commandText)
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();
        using var command = new MySqlCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    private static string ReadReportedServerVersion(string connectionString)
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();
        using var command = new MySqlCommand("SELECT VERSION()", connection);
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture)!;
    }

    private static Version ReadServerVersion(string connectionString)
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();
        return ParseVersion(connection.ServerVersion);
    }

    private string WithDatabase(string name)
    {
        return new MySqlConnectionStringBuilder(_adminConnectionString.Value) { Database = name }.ConnectionString;
    }

    private string StartServer()
    {
        var connectionString = TestDatabaseEnvironment.GetProviderSetting(_provider, "CONNECTIONSTRING");
        if (connectionString is not null)
        {
            return connectionString;
        }

        var image = TestDatabaseEnvironment.GetProviderSetting(_provider, "IMAGE");
        if (_provider == TestDbProvider.MariaDb)
        {
            var container = new MariaDbBuilder(image ?? MariaDbImage)
                .WithUsername("root")
                .WithPassword(RootPassword)
                .WithTmpfsMount("/var/lib/mysql")
                .WithCommand(MaxAllowedPacket, "--max-connections=300", "--innodb-flush-log-at-trx-commit=0")
                .Build();
            container.StartAsync().GetAwaiter().GetResult();
            _container = container;
            return container.GetConnectionString();
        }
        else
        {
            var container = new MySqlBuilder(image ?? MySqlImage)
                .WithUsername("root")
                .WithPassword(RootPassword)
                .WithTmpfsMount("/var/lib/mysql")
                .WithCommand(MaxAllowedPacket, "--max-connections=300", "--innodb-flush-log-at-trx-commit=0", "--skip-log-bin")
                .Build();
            container.StartAsync().GetAwaiter().GetResult();
            _container = container;
            return container.GetConnectionString();
        }
    }

    private static Version ParseVersion(string reported)
    {
        // MariaDB prefixes the version with 5.5.5- when it talks to a client that does not know about it.
        var text = reported.StartsWith("5.5.5-", StringComparison.Ordinal) ? reported[6..] : reported;
        var end = 0;
        while (end < text.Length && (char.IsAsciiDigit(text[end]) || text[end] == '.'))
        {
            end++;
        }

        return Version.Parse(text[..end]);
    }
}
