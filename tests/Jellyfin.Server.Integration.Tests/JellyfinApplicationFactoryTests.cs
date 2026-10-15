using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Testing;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

/// <summary>
/// Covers how the factory runs the server on the database provider selected for the test run.
/// </summary>
public sealed class JellyfinApplicationFactoryTests
{
    [Fact]
    public async Task CreateClient_SelectedProvider_RunsTheServerOnThatProvider()
    {
        using var factory = new JellyfinApplicationFactory();
        using var client = factory.CreateClient();

        Assert.IsType(ProviderTypeOfTheRun(), factory.Services.GetRequiredService<IJellyfinDatabaseProvider>());
        BackendLiveness.Verify(Environment.GetEnvironmentVariable(BackendLiveness.ExpectedProviderVariable), TestDatabaseEnvironment.Backend);

        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        await using var context = await dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var applied = (await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToHashSet(StringComparer.Ordinal);
        Assert.All(context.GetService<IMigrationsAssembly>().Migrations.Keys, id => Assert.Contains(id, applied));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void CreateClient_Sqlite_LeavesTheDatabaseConfigurationToTheServer()
    {
        using var factory = new JellyfinApplicationFactory();
        using var client = factory.CreateClient();

        var configuration = ReadDatabaseConfiguration(factory);

        Assert.Equal("Jellyfin-SQLite", configuration.DatabaseType);
        Assert.Null(configuration.CustomProviderOptions);
    }

    [ProviderFact(TestDbProvider.Postgres, TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void CreateClient_DatabaseServer_StartsEveryFactoryOnADatabaseOfItsOwn()
    {
        using var first = new JellyfinApplicationFactory();
        using var second = new JellyfinApplicationFactory();
        using var firstClient = first.CreateClient();
        using var secondClient = second.CreateClient();

        var firstConfiguration = ReadDatabaseConfiguration(first);
        var secondConfiguration = ReadDatabaseConfiguration(second);

        Assert.Equal(DatabaseTypeOfTheRun(), firstConfiguration.DatabaseType);
        Assert.Equal(DatabaseTypeOfTheRun(), secondConfiguration.DatabaseType);
        Assert.NotEqual(DatabaseName(firstConfiguration), DatabaseName(secondConfiguration));
    }

    [ProviderFact(TestDbProvider.Postgres, TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Dispose_DatabaseServer_DropsTheDatabase()
    {
        string connectionString;
        using (var factory = new JellyfinApplicationFactory())
        {
            using var client = factory.CreateClient();
            connectionString = ReadDatabaseConfiguration(factory).CustomProviderOptions!.ConnectionString;
            Assert.True(CanConnect(connectionString));
        }

        Assert.False(CanConnect(connectionString));
    }

    [ProviderFact(TestDbProvider.Postgres, TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task Services_DatabaseServer_AreBuiltByTheMigrationsOfTheServer()
    {
        using var factory = new JellyfinApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Startup/Configuration", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        await using var context = await dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var applied = (await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToHashSet(StringComparer.Ordinal);
        var migrations = context.GetService<IMigrationsAssembly>();

        Assert.Same(ProviderTypeOfTheRun().Assembly, migrations.Assembly);
        Assert.NotEmpty(migrations.Migrations);
        Assert.All(migrations.Migrations.Keys, id => Assert.Contains(id, applied));
    }

    [ProviderFact(TestDbProvider.Postgres, TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Dispose_DatabaseServer_RemovesTheDirectoryItCreated()
    {
        string root;
        string configurationPath;
        using (var factory = new JellyfinApplicationFactory())
        {
            using var client = factory.CreateClient();
            var paths = factory.Services.GetRequiredService<IApplicationPaths>();
            root = paths.ProgramDataPath;
            configurationPath = Path.Combine(paths.ConfigurationDirectoryPath, "database.xml");
            Assert.True(File.Exists(configurationPath));
        }

        Assert.False(File.Exists(configurationPath));
        Assert.False(Directory.Exists(root));
    }

    [ProviderFact(TestDbProvider.Postgres, TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Dispose_DatabaseServer_KeepsTheDirectoryItDidNotCreate()
    {
        var root = CreateRoot();
        var sentinel = Path.Combine(root, "keep.txt");
        File.WriteAllText(sentinel, "keep");
        try
        {
            string configurationPath;
            using (var factory = new RootApplicationFactory(root))
            {
                using var client = factory.CreateClient();
                configurationPath = Path.Combine(factory.Services.GetRequiredService<IApplicationPaths>().ConfigurationDirectoryPath, "database.xml");
                Assert.True(File.Exists(configurationPath));
            }

            Assert.False(File.Exists(configurationPath));
            Assert.True(File.Exists(sentinel));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [ProviderFact(TestDbProvider.Postgres, TestDbProvider.MySql, TestDbProvider.MariaDb)]
    [UnsupportedOSPlatform("windows")]
    public void CreateClient_DatabaseServer_KeepsTheDatabaseConfigurationPrivate()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix-only test");

        using var factory = new JellyfinApplicationFactory();
        using var client = factory.CreateClient();
        var path = Path.Combine(factory.Services.GetRequiredService<IApplicationPaths>().ConfigurationDirectoryPath, "database.xml");

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
    }

    [Fact]
    public void CreateClient_ConfigurationOfAnotherProvider_RefusesToStart()
    {
        var other = TestDatabaseEnvironment.Provider == TestDbProvider.Sqlite ? typeof(PostgresDatabaseProvider) : typeof(SqliteDatabaseProvider);
        var key = other.GetCustomAttribute<JellyfinDatabaseProviderKeyAttribute>()!.DatabaseProviderKey;
        var root = CreateRoot();
        try
        {
            new MyXmlSerializer().SerializeToFile(new DatabaseConfigurationOptions { DatabaseType = key }, Path.Combine(root, "config", "database.xml"));
            using var factory = new RootApplicationFactory(root);

            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            var refusal = exception as InvalidOperationException ?? exception.InnerException as InvalidOperationException;
            Assert.NotNull(refusal);
            Assert.Contains(key, refusal.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "jellyfin-test-data", "factory-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
        Directory.CreateDirectory(Path.Combine(root, "config"));
        return root;
    }

    private static void DeleteRoot(string root)
    {
        try
        {
            Directory.Delete(root, true);
        }
        catch (IOException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private static DatabaseConfigurationOptions ReadDatabaseConfiguration(JellyfinApplicationFactory factory)
    {
        var paths = factory.Services.GetRequiredService<IApplicationPaths>();
        var path = Path.Combine(paths.ConfigurationDirectoryPath, "database.xml");
        return (DatabaseConfigurationOptions)new MyXmlSerializer().DeserializeFromFile(typeof(DatabaseConfigurationOptions), path)!;
    }

    private static Type ProviderTypeOfTheRun()
    {
        return TestDatabaseEnvironment.Provider switch
        {
            TestDbProvider.Sqlite => typeof(SqliteDatabaseProvider),
            TestDbProvider.Postgres => typeof(PostgresDatabaseProvider),
            TestDbProvider.MySql or TestDbProvider.MariaDb => typeof(MySqlDatabaseProvider),
            var other => throw new NotSupportedException($"{other} is not covered.")
        };
    }

    private static string DatabaseTypeOfTheRun()
    {
        return TestDatabaseEnvironment.Provider switch
        {
            TestDbProvider.Postgres => "Jellyfin-PostgreSQL",
            TestDbProvider.MySql or TestDbProvider.MariaDb => "Jellyfin-MySQL",
            var other => throw new NotSupportedException($"{other} is not covered.")
        };
    }

    private static string? DatabaseName(DatabaseConfigurationOptions configuration)
    {
        var connectionString = configuration.CustomProviderOptions!.ConnectionString;
        return TestDatabaseEnvironment.Provider == TestDbProvider.Postgres
            ? new NpgsqlConnectionStringBuilder(connectionString).Database
            : new MySqlConnectionStringBuilder(connectionString).Database;
    }

    private static bool CanConnect(string connectionString)
    {
        return TestDatabaseEnvironment.Provider == TestDbProvider.Postgres
            ? CanConnectToPostgres(connectionString)
            : CanConnectToMySql(connectionString);
    }

    private static bool CanConnectToMySql(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString) { Pooling = false };
        try
        {
            using var connection = new MySqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.UnknownDatabase)
        {
            return false;
        }
    }

    private static bool CanConnectToPostgres(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false };
        try
        {
            using var connection = new NpgsqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            return false;
        }
    }

    private sealed class RootApplicationFactory(string webHostPathRoot) : JellyfinApplicationFactory(webHostPathRoot);
}
