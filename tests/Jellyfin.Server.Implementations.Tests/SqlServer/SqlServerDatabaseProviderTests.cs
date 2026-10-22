using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the SQL Server provider, without a server, to how it sets EF Core up, what it logs, how it tells errors apart,
/// and the SQL it writes for what the shared queries ask of it.
/// </summary>
public sealed class SqlServerDatabaseProviderTests : IDisposable
{
    private const string Secret = "Pa55-w0rd-not-to-log";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-sqlserver-provider-tests", Guid.NewGuid().ToString("N"));

    // The warning is given once for each database in the process, so each test uses a database of its own.
    private readonly string _database = "jellyfin_provider_" + Guid.NewGuid().ToString("N");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public void AddJellyfinDbContext_SqlServerConfigured_CreatesTheSqlServerProviderAndWarnsOnce()
    {
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        using var serviceProvider = BuildServiceProvider(logger);

        var first = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
        var second = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
        using var scope = serviceProvider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>().CreateDbContext();

        Assert.IsType<SqlServerDatabaseProvider>(first);
        Assert.Same(first, second);
        Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void AddJellyfinDbContext_RegisteredInTheMigrationContainerAndTheHostContainer_WarnsOnce()
    {
        // The server registers the database context twice: for the migrations that run before it starts, and for the host.
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        using var migrationContainer = BuildServiceProvider(logger);
        using var hostContainer = BuildServiceProvider(logger);

        _ = migrationContainer.GetRequiredService<IDbContextFactory<JellyfinDbContext>>().CreateDbContext();
        _ = hostContainer.GetRequiredService<IDbContextFactory<JellyfinDbContext>>().CreateDbContext();

        Assert.NotSame(
            migrationContainer.GetRequiredService<IJellyfinDatabaseProvider>(),
            hostContainer.GetRequiredService<IJellyfinDatabaseProvider>());
        Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void Initialise_LogsOneWarningThatTheProviderIsExperimentalAndAConnectionStringWithoutThePassword()
    {
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();

        new SqlServerDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration());

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("SQL Server", warning.Message, StringComparison.Ordinal);
        Assert.Contains("experimental", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(logger.Entries, entry => entry.Message.Contains(_database, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(Secret, StringComparison.Ordinal));
    }

    [Fact]
    public void Initialise_AgainForTheSameDatabase_LogsNoSecondWarning()
    {
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();

        new SqlServerDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration());
        new SqlServerDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration());
        new SqlServerDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration(Guid.NewGuid().ToString("N")));

        Assert.Equal(2, logger.Entries.Count(entry => entry.Level == LogLevel.Warning));
    }

    [Theory]
    [InlineData("Not A Keyword=1")]
    [InlineData("Encrypt=maybe")]
    public void Initialise_InvalidConnectionString_IsRefusedWithoutShowingIt(string invalidPart)
    {
        var configuration = CreateConfiguration();
        configuration.CustomProviderOptions!.ConnectionString = $"Server=localhost;Password={Secret};{invalidPart}";

        var refusal = Assert.Throws<InvalidOperationException>(
            () => new SqlServerDatabaseProvider(NullLogger<SqlServerDatabaseProvider>.Instance).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), configuration));

        Assert.DoesNotContain(Secret, refusal.ToString(), StringComparison.Ordinal);
        Assert.Null(refusal.InnerException);
    }

    [Fact]
    public void Initialise_NoConnectionString_IsRefused()
    {
        var configuration = CreateConfiguration();
        configuration.CustomProviderOptions!.ConnectionString = " ";

        Assert.Throws<InvalidOperationException>(
            () => new SqlServerDatabaseProvider(NullLogger<SqlServerDatabaseProvider>.Instance).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), configuration));
    }

    [Theory]
    [InlineData(null, null, 60)]
    [InlineData("Command Timeout=15", null, 15)]
    [InlineData(null, "120", 120)]
    [InlineData("Command Timeout=15", "120", 120)]
    public void Initialise_CommandTimeout_IsTheOptionTheConnectionStringOrAMinute(string? inConnectionString, string? option, int expected)
    {
        var configuration = CreateConfiguration();
        configuration.CustomProviderOptions!.ConnectionString += inConnectionString is null ? string.Empty : ";" + inConnectionString;
        if (option is not null)
        {
            configuration.CustomProviderOptions.Options.Add(new CustomDatabaseOption { Key = "command-timeout", Value = option });
        }

        var options = Initialise(configuration);

        Assert.Equal(expected, new SqlConnectionStringBuilder(RelationalOptionsExtension.Extract(options.Options).ConnectionString).CommandTimeout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Column Encryption Setting=Enabled")]
    [InlineData("Application Name='Encrypt=False'")]
    [InlineData("Encrypt=")]
    public void Initialise_ConnectionStringWithoutEncrypt_StatesThatTheConnectionIsEncrypted(string? extra)
    {
        var configuration = CreateConfiguration();
        if (extra is not null)
        {
            configuration.CustomProviderOptions!.ConnectionString += ";" + extra;
        }

        var options = Initialise(configuration);

        // SqlClient encrypts by default, but the string has to say so, as the driver is not what states it.
        var connectionString = RelationalOptionsExtension.Extract(options.Options).ConnectionString!;
        Assert.Equal("True", NamedEncrypt(connectionString));
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, new SqlConnectionStringBuilder(connectionString).Encrypt);
    }

    [Theory]
    [InlineData("Encrypt=True", "Mandatory")]
    [InlineData("Encrypt=False", "Optional")]
    [InlineData("encrypt = false", "Optional")]
    [InlineData("Encrypt=Optional", "Optional")]
    [InlineData("Encrypt=Mandatory", "Mandatory")]
    [InlineData("Encrypt=Strict", "Strict")]
    public void Initialise_ConnectionStringWithEncrypt_KeepsTheChoiceOfTheOperator(string inConnectionString, string expected)
    {
        var configuration = CreateConfiguration();
        configuration.CustomProviderOptions!.ConnectionString += ";" + inConnectionString;

        var options = Initialise(configuration);

        var connectionString = RelationalOptionsExtension.Extract(options.Options).ConnectionString!;
        Assert.NotNull(NamedEncrypt(connectionString));
        Assert.Equal(SqlConnectionEncryptOption.Parse(expected), new SqlConnectionStringBuilder(connectionString).Encrypt);
    }

    [Theory]
    [InlineData(null, "True")]
    [InlineData("Encrypt=False", "False")]
    [InlineData("Encrypt=Strict", "Strict")]
    public void Initialise_LogsTheEncryptionOfTheConnectionStringWithoutThePassword(string? inConnectionString, string expected)
    {
        var configuration = CreateConfiguration();
        configuration.CustomProviderOptions!.ConnectionString += inConnectionString is null ? string.Empty : ";" + inConnectionString;
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();

        new SqlServerDatabaseProvider(logger).Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), configuration);

        var logged = Assert.Single(logger.Entries, entry => entry.Message.Contains(_database, StringComparison.Ordinal)).Message;
        Assert.Contains("Encrypt=" + expected, logged, StringComparison.Ordinal);
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(Secret, StringComparison.Ordinal));
    }

    [Fact]
    public void Initialise_SetsEfCoreUpForTheProvider()
    {
        using var context = OfflineContexts.SqlServer();
        var debugInfo = new Dictionary<string, string>();
        foreach (var extension in context.GetService<IDbContextOptions>().Extensions)
        {
            extension.Info.PopulateDebugInfo(debugInfo);
        }

        Assert.IsType<BinaryCollationHistoryRepository>(context.GetService<IHistoryRepository>());
        Assert.Contains(SessionSettingsInterceptor.Instance, context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()!.Interceptors!);
        Assert.Equal("160", debugInfo["SqlServerCompatibilityLevel"]);
        Assert.Equal("1", debugInfo["Jellyfin.SqlServer:DateTimeConstantTranslator"]);
        Assert.Equal(SqlServerDatabaseProvider.CompatibilityLevel.ToString(System.Globalization.CultureInfo.InvariantCulture), debugInfo["SqlServerCompatibilityLevel"]);
    }

    [Fact]
    public void Initialise_ForTwoDatabasesWithOtherInterceptors_SharesOneInternalServiceProvider()
    {
        // EF Core keeps an internal service provider for each configuration of the options in the process; options that
        // differ only in the database and in interceptors that are not singletons share one.
        var first = Initialise(CreateConfiguration()).AddInterceptors(new NoOpCommandInterceptor()).Options;
        var second = Initialise(CreateConfiguration(Guid.NewGuid().ToString("N"))).Options;

        using var firstContext = CreateContext(first);
        using var secondContext = CreateContext(second);

        Assert.Same(firstContext.GetService<IModelSource>(), secondContext.GetService<IModelSource>());
    }

    [Fact]
    public void Initialise_AgainOnTheSameOptions_KeepsOneTranslatorExtension()
    {
        var builder = Initialise(CreateConfiguration());
        new SqlServerDatabaseProvider(NullLogger<SqlServerDatabaseProvider>.Instance).Initialise(builder, CreateConfiguration());

        Assert.Single(builder.Options.Extensions, e => e is DateTimeConstantTranslator.Extension);
    }

    [Theory]
    [InlineData(3, "16.00.4295", "Supported")]
    [InlineData(3, "17.00.5015", "Supported")]
    [InlineData(2, "16.00.1000", "Supported")]
    [InlineData(4, "16.00.1000", "Supported")]
    [InlineData(42, "16.00.1000", "Supported")]
    [InlineData(null, "16.00.1000", "Supported")]
    [InlineData(3, "15.00.4430", "TooOld")]
    [InlineData(3, "12.00.2000", "TooOld")]
    [InlineData(5, "12.00.2000", "Unsupported")]
    [InlineData(5, "16.00.1000", "Unsupported")]
    [InlineData(8, "12.00.2000", "Unsupported")]
    [InlineData(8, "16.00.1000", "Unsupported")]
    [InlineData(8, "17.00.1000", "Unsupported")]
    [InlineData(6, "12.00.2000", "Unsupported")]
    [InlineData(6, "16.00.1000", "Unsupported")]
    [InlineData(11, "12.00.2000", "Unsupported")]
    [InlineData(11, "16.00.1000", "Unsupported")]
    [InlineData(3, "", "Unreadable")]
    [InlineData(3, null, "Unreadable")]
    [InlineData(3, "not a version", "Unreadable")]
    public void Decide_RefusesAServerOlderThan2022AndTheServicesOfAzure(int? edition, string? version, string expected)
    {
        Assert.Equal(expected, SessionSettingsInterceptor.Decide(edition, version).ToString());
    }

    [Theory]
    [InlineData(5, "12.00.2000", "Azure SQL Database, Azure SQL Managed Instance and Azure Synapse are not supported")]
    [InlineData(8, "16.00.1000", "Azure SQL Database, Azure SQL Managed Instance and Azure Synapse are not supported")]
    [InlineData(11, "16.00.1000", "Azure SQL Database, Azure SQL Managed Instance and Azure Synapse are not supported")]
    [InlineData(3, "15.00.4430", "SQL Server 15.00.4430 is not supported. Jellyfin needs SQL Server 2022 (16.0) or later.")]
    [InlineData(3, "x", "cannot be read")]
    public void ThrowIfUnsupported_SaysWhyTheServerIsRefused(int edition, string version, string message)
    {
        var refusal = Assert.Throws<InvalidOperationException>(() => SessionSettingsInterceptor.ThrowIfUnsupported(edition, version));

        Assert.Contains(message, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfUnsupported_SupportedServer_DoesNotThrow()
    {
        SessionSettingsInterceptor.ThrowIfUnsupported(3, "16.00.4295");
    }

    [Fact]
    public void Classifiers_ErrorWithoutASqlException_IsNeitherAConflictNorAboutTheData()
    {
        var provider = new SqlServerDatabaseProvider(NullLogger<SqlServerDatabaseProvider>.Instance);
        Exception[] others = [new InvalidOperationException("duplicate key"), new DbUpdateException("save", new TimeoutException()), new IOException("2627")];

        Assert.All(others, other =>
        {
            Assert.False(provider.IsRetryableWriteConflict(other));
            Assert.False(provider.IsDataError(other));
        });
        Assert.Throws<ArgumentNullException>(() => provider.IsRetryableWriteConflict(null!));
        Assert.Throws<ArgumentNullException>(() => provider.IsDataError(null!));
    }

    [Fact]
    public void LikeIgnoreAsciiCase_FoldsTheAsciiCapitalsOfTheValueUnderTheBinaryCollation()
    {
        using var context = OfflineContexts.SqlServer();

        var sql = context.BaseItems.Where(e => JellyfinDbFunctions.LikeIgnoreAsciiCase(e.CleanName, "abc%")).ToQueryString();

        Assert.Contains(
            "TRANSLATE([b].[CleanName] COLLATE Latin1_General_100_BIN2, N'ABCDEFGHIJKLMNOPQRSTUVWXYZ', N'abcdefghijklmnopqrstuvwxyz') LIKE ",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("ESCAPE N'\\'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LOWER(", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("year")]
    [InlineData("month")]
    [InlineData("day")]
    [InlineData("hour")]
    [InlineData("minute")]
    [InlineData("second")]
    [InlineData("millisecond")]
    public void AddToAConstantDate_CastsTheConstantToDatetime2(string datePart)
    {
        // The date is written in the query, as the shared queries write it, so that it is a constant and not a parameter.
        using var context = OfflineContexts.SqlServer();
        var query = datePart switch
        {
            "year" => context.BaseItems.Select(e => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddYears(e.ProductionYear!.Value)),
            "month" => context.BaseItems.Select(e => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(e.ProductionYear!.Value)),
            "day" => context.BaseItems.Select(e => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(e.ProductionYear!.Value)),
            "hour" => context.BaseItems.Select(e => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(e.ProductionYear!.Value)),
            "minute" => context.BaseItems.Select(e => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(e.ProductionYear!.Value)),
            "second" => context.BaseItems.Select(e => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(e.ProductionYear!.Value)),
            _ => context.BaseItems.Select(e => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(e.ProductionYear!.Value))
        };

        var sql = query.ToQueryString();

        Assert.True(sql.Contains($"DATEADD({datePart}, ", StringComparison.Ordinal), sql);
        Assert.True(Regex.IsMatch(sql, @"CAST\('2000-01-01T00:00:00(\.0+)?Z?' AS datetime2\)"), sql);
    }

    [Fact]
    public void AddToADateColumn_IsLeftToEfCore()
    {
        using var context = OfflineContexts.SqlServer();

        var sql = context.BaseItems.Select(e => e.DateCreated!.Value.AddDays(1)).ToQueryString();

        Assert.Contains("DATEADD(day, CAST(1.0E0 AS int), [b].[DateCreated])", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("AS datetime2)", sql, StringComparison.Ordinal);
    }

    private static JellyfinDbContext CreateContext(DbContextOptions<JellyfinDbContext> options)
        => new(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            new SqlServerDatabaseProvider(NullLogger<SqlServerDatabaseProvider>.Instance),
            new Jellyfin.Database.Implementations.Locking.NoLockBehavior(NullLogger<Jellyfin.Database.Implementations.Locking.NoLockBehavior>.Instance));

    // The value the connection string names for Encrypt, or null when it names none.
    private static string? NamedEncrypt(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return builder.TryGetValue("Encrypt", out var value) ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) : null;
    }

    private static DbContextOptionsBuilder<JellyfinDbContext> Initialise(DatabaseConfigurationOptions configuration)
    {
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        new SqlServerDatabaseProvider(NullLogger<SqlServerDatabaseProvider>.Instance).Initialise(builder, configuration);
        return builder;
    }

    private DatabaseConfigurationOptions CreateConfiguration(string? database = null) => new()
    {
        DatabaseType = "Jellyfin-SqlServer",
        CustomProviderOptions = new CustomDatabaseOptions
        {
            PluginName = string.Empty,
            PluginAssembly = string.Empty,
            ConnectionString = $"Server=localhost;Database={database ?? _database};User Id=sa;Password={Secret}",
            Options = []
        }
    };

    private ServiceProvider BuildServiceProvider(ILogger<SqlServerDatabaseProvider> logger)
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

    private sealed class NoOpCommandInterceptor : DbCommandInterceptor;
}
