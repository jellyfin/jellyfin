using System;
using System.IO;
using System.Linq;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Implementations.Tests.Postgres;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public sealed class MySqlRegistrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-mysql-registration-tests", Guid.NewGuid().ToString("N"));

    // The warning is given once for each database in the process, so each test uses a database of its own.
    private readonly string _database = "jellyfin_registration_" + Guid.NewGuid().ToString("N");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public void Initialise_LogsOneWarningThatTheProviderIsExperimental()
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();
        var provider = new MySqlDatabaseProvider(logger);
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();

        provider.Initialise(builder, CreateConfiguration());

        var entry = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("MySQL and MariaDB", entry.Message, StringComparison.Ordinal);
        Assert.Contains("experimental", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_LogsNothing()
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();

        _ = new MySqlDatabaseProvider(logger);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Initialise_AgainForTheSameDatabase_LogsNoSecondWarning()
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();
        var configuration = CreateConfiguration();

        new MySqlDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), configuration);
        new MySqlDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), configuration);

        Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void Initialise_ForAnotherDatabase_WarnsAgain()
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();

        new MySqlDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration());
        new MySqlDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration(Guid.NewGuid().ToString("N")));

        Assert.Equal(2, logger.Entries.Count(entry => entry.Level == LogLevel.Warning));
    }

    [Fact]
    public void AddJellyfinDbContext_MySqlConfigured_CreatesTheMySqlProviderAndWarnsOnce()
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();
        using var serviceProvider = BuildServiceProvider(logger);

        var first = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
        var second = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
        using var scope = serviceProvider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>().CreateDbContext();

        Assert.IsType<MySqlDatabaseProvider>(first);
        Assert.Same(first, second);
        Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void AddJellyfinDbContext_RegisteredInTheMigrationContainerAndTheHostContainer_WarnsOnce()
    {
        // The server registers the database context twice: for the migrations that run before it starts, and for the host.
        var logger = new CollectingLogger<MySqlDatabaseProvider>();
        using var migrationContainer = BuildServiceProvider(logger);
        using var hostContainer = BuildServiceProvider(logger);

        _ = migrationContainer.GetRequiredService<IDbContextFactory<JellyfinDbContext>>().CreateDbContext();
        _ = hostContainer.GetRequiredService<IDbContextFactory<JellyfinDbContext>>().CreateDbContext();

        Assert.NotSame(
            migrationContainer.GetRequiredService<IJellyfinDatabaseProvider>(),
            hostContainer.GetRequiredService<IJellyfinDatabaseProvider>());
        Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    private DatabaseConfigurationOptions CreateConfiguration(string? database = null) => new()
    {
        DatabaseType = "Jellyfin-MySQL",
        CustomProviderOptions = new CustomDatabaseOptions
        {
            PluginName = string.Empty,
            PluginAssembly = string.Empty,
            ConnectionString = $"Server=localhost;Database={database ?? _database}",
            Options = [new CustomDatabaseOption { Key = "server-version", Value = "8.4-mysql" }]
        }
    };

    private ServiceProvider BuildServiceProvider(ILogger<MySqlDatabaseProvider> logger)
    {
        var paths = new ServerApplicationPaths(
            Path.Combine(_root, "data"),
            Path.Combine(_root, "log"),
            Path.Combine(_root, "config"),
            Path.Combine(_root, "cache"),
            Path.Combine(_root, "web"));
        Directory.CreateDirectory(paths.ConfigurationDirectoryPath);
        var configurationManager = new ServerConfigurationManager(paths, NullLoggerFactory.Instance, new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        configurationManager.SaveConfiguration("database", CreateConfiguration());

        return new ServiceCollection()
            .AddLogging()
            .AddSingleton(logger)
            .AddSingleton<IApplicationPaths>(paths)
            .AddJellyfinDbContext(configurationManager, new ConfigurationBuilder().Build())
            .BuildServiceProvider();
    }
}
