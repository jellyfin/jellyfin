using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Helpers;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Server.Tests;

/// <summary>
/// The server registers the database in two service containers: one for the startup migrations, which reads the
/// startup configuration, and the one of the web host, which reads the configuration of the generic host. Both must
/// take the connection string override from the same place, otherwise the migrations and the server would
/// work on different databases.
/// </summary>
[Collection(EnvironmentTestCollection.Name)]
public sealed class StartupDatabaseConfigurationTests : IDisposable
{
    private const string OverrideKey = "database:ConnectionString";
    private const string Secret = "Host=override;Username=jf;Password=env-secret";

    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly List<IDisposable> _disposables = [];
    private readonly CapturingLoggerProvider _logs = new();

    public StartupDatabaseConfigurationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-startup-database-configuration-tests", Guid.NewGuid().ToString("N"));
        _paths = new ServerApplicationPaths(
            Path.Combine(_root, "data"),
            Path.Combine(_root, "log"),
            Path.Combine(_root, "config"),
            Path.Combine(_root, "cache"),
            Path.Combine(_root, "web"));
        Directory.CreateDirectory(_paths.DataPath);
        Directory.CreateDirectory(_paths.LogDirectoryPath);
        Directory.CreateDirectory(_paths.ConfigurationDirectoryPath);
        Directory.CreateDirectory(_paths.CachePath);
    }

    [Theory]
    [InlineData("DOTNET_DATABASE__CONNECTIONSTRING")]
    [InlineData("DATABASE__CONNECTIONSTRING")]
    [InlineData("ASPNETCORE_DATABASE__CONNECTIONSTRING")]
    public async Task VariableOutsideTheJellyfinPrefix_IsAppliedByNeitherContainer(string variable)
    {
        using var scope = new EnvironmentVariableScope(variable, Secret);
        var (startupConfig, hostConfig) = await CreateConfigurationsAsync();

        // The generic host does see this variable, which is why the web host container must not read it from there.
        Assert.Equal(Secret, hostConfig[OverrideKey]);
        Assert.Null(startupConfig[OverrideKey]);

        var webHostContainerConfig = Startup.CreateDatabaseConfiguration(hostConfig, startupConfig);
        Assert.Null(webHostContainerConfig[OverrideKey]);

        await AssertContainerAppliesNoOverrideAsync(startupConfig);
        await AssertContainerAppliesNoOverrideAsync(webHostContainerConfig);
    }

    [Fact]
    public async Task JellyfinPrefixedVariable_IsAppliedByBothContainers()
    {
        using var scope = new EnvironmentVariableScope("JELLYFIN_DATABASE__CONNECTIONSTRING", Secret);
        var (startupConfig, hostConfig) = await CreateConfigurationsAsync();

        var webHostContainerConfig = Startup.CreateDatabaseConfiguration(hostConfig, startupConfig);

        Assert.Equal(Secret, startupConfig[OverrideKey]);
        Assert.Equal(Secret, webHostContainerConfig[OverrideKey]);
        await AssertContainerAppliesOverrideAsync(startupConfig);
        await AssertContainerAppliesOverrideAsync(webHostContainerConfig);
    }

    [Fact]
    public async Task OtherKeysOfTheHostConfiguration_StayAvailableToTheWebHostContainer()
    {
        var (startupConfig, hostConfig) = await CreateConfigurationsAsync();
        var hostWithKey = new ConfigurationBuilder()
            .AddConfiguration(hostConfig)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["migration-provider"] = "Jellyfin-SQLite" })
            .Build();

        var webHostContainerConfig = Startup.CreateDatabaseConfiguration(hostWithKey, startupConfig);

        Assert.Equal("Jellyfin-SQLite", webHostContainerConfig["migration-provider"]);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        _logs.Dispose();
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private async Task<(IConfiguration StartupConfig, IConfiguration HostConfig)> CreateConfigurationsAsync()
    {
        await StartupHelpers.InitLoggingConfigFile(_paths);

        // The configuration the startup migrations use, and the one the server hands to its web host startup.
        var startupConfig = Program.CreateAppConfiguration(new StartupOptions(), _paths);

        // The configuration of the web host: the generic host defaults, which add the unprefixed, DOTNET_ and ASPNETCORE_ variables.
        // The server adds its JELLYFIN_ variables on top of those, see Program.ConfigureAppConfiguration.
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webHost => webHost.Configure(_ => { }))
            .ConfigureAppConfiguration(config => config.AddEnvironmentVariables("JELLYFIN_"))
            .Build();
        _disposables.Add(host);

        return (startupConfig, host.Services.GetRequiredService<IConfiguration>());
    }

    private async Task AssertContainerAppliesOverrideAsync(IConfiguration configuration)
    {
        var before = _logs.Entries.Count;

        await ResolveDatabaseAsync(configuration);

        var logged = _logs.Entries;
        Assert.Contains(logged.Skip(before), entry => entry.State.Contains(OverrideKey, StringComparison.Ordinal));
    }

    private async Task AssertContainerAppliesNoOverrideAsync(IConfiguration configuration)
    {
        var before = _logs.Entries.Count;

        await ResolveDatabaseAsync(configuration);

        foreach (var entry in _logs.Entries.Skip(before))
        {
            Assert.DoesNotContain(OverrideKey, entry.State, StringComparison.Ordinal);
        }
    }

    private async Task ResolveDatabaseAsync(IConfiguration configuration)
    {
        var configurationManager = new ServerConfigurationManager(_paths, new LoggerFactory(), new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(_logs))
            .AddSingleton<IApplicationPaths>(_paths)
            .AddJellyfinDbContext(configurationManager, configuration)
            .BuildServiceProvider();
        _disposables.Add(serviceProvider);

        var factory = serviceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }
}
