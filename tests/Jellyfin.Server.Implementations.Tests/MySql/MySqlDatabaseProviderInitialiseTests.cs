using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Tests.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using MySqlConnector;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlDatabaseProviderInitialiseTests
{
    private const string ServerVersionOption = "server-version";

    [Theory]
    [InlineData("Server=db;Database=jellyfin;User ID=jellyfin;Password=s3cret-value")]
    [InlineData("Server=db;Database=jellyfin;Uid=jellyfin;Pwd=s3cret-value")]
    public void Initialise_ConnectionStringWithPassword_NeverLogsThePassword(string connectionString)
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();
        var provider = new MySqlDatabaseProvider(logger);

        provider.Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration(connectionString, Version("8.4-mysql")));

        Assert.NotEmpty(logger.Entries);
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("s3cret-value", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, e => e.Message.Contains("jellyfin", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Initialise_WithoutConnectionString_ThrowsAndNamesTheSettings(string connectionString)
    {
        var provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration(connectionString)));

        Assert.Contains("CustomProviderOptions/ConnectionString", ex.Message, StringComparison.Ordinal);
        Assert.Contains("database:ConnectionString", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Initialise_ConnectionStringThatCannotBeParsed_ThrowsWithoutRepeatingIt()
    {
        var provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.Initialise(
                new DbContextOptionsBuilder<JellyfinDbContext>(),
                CreateConfiguration("Server=db;Password=s3cret-value;NoSuchKeyword=1", Version("8.4-mysql"))));

        Assert.DoesNotContain("s3cret-value", ex.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("NoSuchKeyword", ex.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Server=db;Database=jellyfin", 60)]
    [InlineData("Server=db;Database=jellyfin;Default Command Timeout=15", 15)]
    [InlineData("Server=db;Database=jellyfin;DefaultCommandTimeout=15", 15)]
    [InlineData("Server=db;Database=jellyfin;Command Timeout=15", 15)]
    [InlineData("Server=db;Database=jellyfin;default command timeout=15", 15)]
    public void Initialise_CommandTimeout_DefaultsOnlyWhenTheConnectionStringHasNone(string connectionString, int expected)
    {
        var options = Initialise(connectionString);

        Assert.Equal(expected, (int)Read(options).DefaultCommandTimeout);
    }

    [Fact]
    public void Initialise_CommandTimeoutOption_OverridesTheConnectionString()
    {
        var options = Initialise(
            "Server=db;Database=jellyfin;Default Command Timeout=15",
            new CustomDatabaseOption { Key = "command-timeout", Value = "120" });

        Assert.Equal(120, (int)Read(options).DefaultCommandTimeout);
    }

    [Fact]
    public void Initialise_MaxPoolSizeOption_IsApplied()
    {
        var options = Initialise("Server=db;Database=jellyfin", new CustomDatabaseOption { Key = "max-pool-size", Value = "7" });

        Assert.Equal(7, (int)Read(options).MaximumPoolSize);
    }

    [Theory]
    [InlineData("Server=db;Database=jellyfin")]
    [InlineData("Server=db;Database=jellyfin;GUID Format=Char36;Allow User Variables=true")]
    public void Initialise_GuidsAndUserVariables_AreAlwaysSetForTheModel(string connectionString)
    {
        var builder = Read(Initialise(connectionString));

        Assert.Equal(MySqlGuidFormat.Binary16, builder.GuidFormat);
        Assert.False(builder.AllowUserVariables);
    }

    [Theory]
    [InlineData("Server=db;Database=jellyfin")]
    [InlineData("Server=db;Database=jellyfin;CharSet=utf8mb4")]
    [InlineData("Server=db;Database=jellyfin;Character Set=UTF8MB4")]
    public void Initialise_CharacterSet_IsUtf8Mb4WithoutAWarningWhenItIsNotChanged(string connectionString)
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();

        new MySqlDatabaseProvider(logger).Initialise(options, CreateConfiguration(connectionString, Version("8.4-mysql")));

        Assert.Equal("utf8mb4", Read(options).CharacterSet);
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("character set", StringComparison.OrdinalIgnoreCase) && e.Message.Contains("replaced", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Server=db;Database=jellyfin;CharSet=utf8")]
    [InlineData("Server=db;Database=jellyfin;CharSet=utf8mb3")]
    [InlineData("Server=db;Database=jellyfin;CharSet=latin1")]
    public void Initialise_OtherCharacterSet_IsReplacedAndTheUserIsWarned(string connectionString)
    {
        var logger = new CollectingLogger<MySqlDatabaseProvider>();
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();

        new MySqlDatabaseProvider(logger).Initialise(options, CreateConfiguration(connectionString, Version("8.4-mysql")));

        Assert.Equal("utf8mb4", Read(options).CharacterSet);
        Assert.Contains(logger.Entries, e => e.Message.Contains("replaced", StringComparison.Ordinal) && e.Message.Contains("utf8mb4", StringComparison.Ordinal));
    }

    [Fact]
    public void Initialise_ServerNamedInTheOptions_UsesTheInterceptorThatTrustsAnUnreadableVersion()
    {
        var named = Initialise("Server=db;Database=jellyfin", Version("8.4-mysql"));

        Assert.Contains(SessionSettingsInterceptor.ForConfiguredServer, named.Options.FindExtension<CoreOptionsExtension>()!.Interceptors!);
        Assert.DoesNotContain(SessionSettingsInterceptor.Instance, named.Options.FindExtension<CoreOptionsExtension>()!.Interceptors!);
    }

    [Theory]
    [InlineData("unreadable")]
    [InlineData("")]
    [InlineData(null)]
    public void ReadDetected_VersionThatCannotBeRead_ThrowsAndNamesTheOption(string? reported)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MySqlDatabaseProvider.ReadDetected(reported));

        Assert.Contains(ServerVersionOption, ex.Message, StringComparison.Ordinal);
        Assert.Contains("8.4-mysql", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadDetected_ReadableVersion_ReturnsTheServer()
    {
        var server = MySqlDatabaseProvider.ReadDetected("11.4.13-MariaDB-ubu2404");

        Assert.Equal(ServerFlavor.MariaDb, server.Flavor);
        Assert.Equal(new Version(11, 4, 13), server.Version);
    }

    [Theory]
    [InlineData("8.4-mysql", false, "8.4.0")]
    [InlineData("8.4.11-mysql", false, "8.4.11")]
    [InlineData("11.4-mariadb", true, "11.4.0")]
    [InlineData("11.4.13-MariaDB", true, "11.4.13")]
    public void Initialise_ServerVersionOption_SelectsTheKindOfServer(string option, bool mariaDb, string version)
    {
        var options = Initialise("Server=db;Database=jellyfin", Version(option));

#pragma warning disable EF1001 // Internal EF Core API usage: The options extension is the only place that holds the server the provider was set up for.
        var serverVersion = options.Options.FindExtension<MySqlOptionsExtension>()?.ServerVersion;
#pragma warning restore EF1001

        Assert.NotNull(serverVersion);
        Assert.Equal(mariaDb ? ServerType.MariaDb : ServerType.MySql, serverVersion.Type);
        Assert.Equal(System.Version.Parse(version), serverVersion.Version);
    }

    [Theory]
    [InlineData("8.0-mysql", "8.4")]
    [InlineData("8.0.40-mysql", "8.4")]
    [InlineData("11.4.4-mariadb", "11.4.7")]
    [InlineData("11.4.6-mariadb", "11.4.7")]
    [InlineData("10.11-mariadb", "11.4.7")]
    public void Initialise_ServerBelowTheFloor_ThrowsAndNamesTheFloor(string option, string floor)
    {
        var provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration("Server=db;Database=jellyfin", Version(option))));

        Assert.Contains(floor, ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("mysql")]
    [InlineData("8.4")]
    [InlineData("8.4-oracle")]
    public void Initialise_ServerVersionOptionThatIsNotValid_ThrowsAndShowsAnExample(string option)
    {
        var provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration("Server=db;Database=jellyfin", Version(option))));

        Assert.Contains(option, ex.Message, StringComparison.Ordinal);
        Assert.Contains("8.4-mysql", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Initialise_WithoutServerVersionOptionAndNoServer_ThrowsAndExplainsTheOption()
    {
        var provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.Initialise(
                new DbContextOptionsBuilder<JellyfinDbContext>(),
                CreateConfiguration("Server=127.0.0.1;Port=1;Database=jellyfin;Connection Timeout=2")));

        Assert.Contains(ServerVersionOption, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Initialise_WithoutServerVersionOptionAndAServerThatDoesNotAnswer_ThrowsAndExplainsTheOption()
    {
        // A server that accepts the connection and then says nothing makes the probe time out, which must not escape as it is.
        var cancellationToken = TestContext.Current.CancellationToken;
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var accepted = listener.AcceptTcpClientAsync(cancellationToken).AsTask();
            var provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

            var ex = Assert.Throws<InvalidOperationException>(
                () => provider.Initialise(
                    new DbContextOptionsBuilder<JellyfinDbContext>(),
                    CreateConfiguration($"Server=127.0.0.1;Port={port};Database=jellyfin;Connection Timeout=1")));

            Assert.Contains(ServerVersionOption, ex.Message, StringComparison.Ordinal);
            Assert.IsType<MySqlException>(ex.InnerException);
            using var client = await accepted;
        }
        finally
        {
            listener.Stop();
        }
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Initialise_WithoutServerVersionOptionAndADatabaseThatDoesNotExist_DetectsTheServer()
    {
        using var database = TestDatabaseEnvironment.Backend.CreateEmpty();
        var missing = new MySqlConnectionStringBuilder(RelationalOptionsExtension.Extract(database.Options).ConnectionString!)
        {
            Database = "jellyfin_missing_" + Guid.NewGuid().ToString("N")
        };
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();

        new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>())
            .Initialise(options, CreateConfiguration(missing.ConnectionString));

#pragma warning disable EF1001 // Internal EF Core API usage: The options extension is the only place that holds the server the provider was set up for.
        var serverVersion = options.Options.FindExtension<MySqlOptionsExtension>()?.ServerVersion;
#pragma warning restore EF1001

        Assert.NotNull(serverVersion);
        Assert.Equal(TestDatabaseEnvironment.Provider == TestDbProvider.MariaDb ? ServerType.MariaDb : ServerType.MySql, serverVersion.Type);
    }

    [Fact]
    public void Initialise_EnableSensitiveDataLoggingOption_IsApplied()
    {
        var options = Initialise("Server=db;Database=jellyfin", new CustomDatabaseOption { Key = "EnableSensitiveDataLogging", Value = "true" });

        Assert.True(options.Options.FindExtension<CoreOptionsExtension>()!.IsSensitiveDataLoggingEnabled);
    }

    [Fact]
    public void Initialise_Twice_SharesTheInterceptorsSoOneServiceProviderServesBoth()
    {
        var first = Initialise("Server=db;Database=jellyfin");
        var second = Initialise("Server=other;Database=jellyfin");

        Assert.Equal(
            first.Options.FindExtension<CoreOptionsExtension>()!.Interceptors,
            second.Options.FindExtension<CoreOptionsExtension>()!.Interceptors);
    }

    [Fact]
    public void SupportsTransactionalMigrations_IsFalse()
    {
        Assert.False(new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>()).SupportsTransactionalMigrations);
    }

    private static CustomDatabaseOption Version(string value) => new() { Key = ServerVersionOption, Value = value };

    private static DbContextOptionsBuilder<JellyfinDbContext> Initialise(string connectionString, params CustomDatabaseOption[] providerOptions)
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();
        var all = Array.Exists(providerOptions, o => o.Key == ServerVersionOption) ? providerOptions : [.. providerOptions, Version("8.4-mysql")];
        new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>())
            .Initialise(options, CreateConfiguration(connectionString, all));
        return options;
    }

    private static MySqlConnectionStringBuilder Read(DbContextOptionsBuilder<JellyfinDbContext> options)
    {
        return new MySqlConnectionStringBuilder(RelationalOptionsExtension.Extract(options.Options).ConnectionString!);
    }

    private static DatabaseConfigurationOptions CreateConfiguration(string connectionString, params CustomDatabaseOption[] providerOptions) => new()
    {
        DatabaseType = "Jellyfin-MySQL",
        CustomProviderOptions = new CustomDatabaseOptions
        {
            PluginName = string.Empty,
            PluginAssembly = string.Empty,
            ConnectionString = connectionString,
            Options = [.. providerOptions]
        }
    };
}
