using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Helpers;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Tests;

/// <summary>
/// The error the PostgreSQL provider raises without a connection string tells the administrator where to put one.
/// These tests follow that advice literally, through the same configuration the server starts with, and expect the
/// provider to pick the connection string up. None of them connects to a database.
/// </summary>
[Collection(EnvironmentTestCollection.Name)]
public sealed class PostgresConnectionStringTests : IDisposable
{
    private const string EnvironmentVariable = "JELLYFIN_DATABASE__CONNECTIONSTRING";
    private const string ConfigurationKey = "database:ConnectionString";

    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly List<ServiceProvider> _serviceProviders = [];

    public PostgresConnectionStringTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-postgres-connection-string-tests", Guid.NewGuid().ToString("N"));
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

    [Fact]
    public async Task NoConnectionStringAnywhere_FailsAndNamesEveryWayToProvideOne()
    {
        WriteDatabaseXml(connectionString: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateContextAsync(new ConfigurationBuilder().Build()));

        Assert.Contains("database.xml", exception.Message, StringComparison.Ordinal);
        Assert.Contains("CustomProviderOptions/ConnectionString", exception.Message, StringComparison.Ordinal);
        Assert.Contains(ConfigurationKey, exception.Message, StringComparison.Ordinal);
        Assert.Contains(EnvironmentVariable, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectionStringInDatabaseXml_IsUsed()
    {
        WriteDatabaseXml("Host=from-xml;Database=jellyfin");

        var connectionString = await CreateContextAsync(new ConfigurationBuilder().Build());

        Assert.Equal("from-xml", new NpgsqlConnectionStringBuilder(connectionString).Host);
    }

    [Fact]
    public async Task ConfigurationKeyNamedInTheMessage_ProvidesTheConnectionString()
    {
        WriteDatabaseXml(connectionString: null);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigurationKey] = "Host=from-key;Database=jellyfin" })
            .Build();

        var connectionString = await CreateContextAsync(configuration);

        Assert.Equal("from-key", new NpgsqlConnectionStringBuilder(connectionString).Host);
    }

    [Fact]
    public async Task EnvironmentVariableNamedInTheMessage_ProvidesTheConnectionStringToTheServerConfiguration()
    {
        WriteDatabaseXml(connectionString: null);
        using var scope = new EnvironmentVariableScope(EnvironmentVariable, "Host=from-environment;Database=jellyfin");
        await StartupHelpers.InitLoggingConfigFile(_paths);

        var connectionString = await CreateContextAsync(Program.CreateAppConfiguration(new StartupOptions(), _paths));

        Assert.Equal("from-environment", new NpgsqlConnectionStringBuilder(connectionString).Host);
    }

    [Fact]
    public async Task EnvironmentVariableNamedInTheMessage_TakesPrecedenceOverDatabaseXml()
    {
        WriteDatabaseXml("Host=from-xml;Database=jellyfin");
        using var scope = new EnvironmentVariableScope(EnvironmentVariable, "Host=from-environment;Database=jellyfin");
        await StartupHelpers.InitLoggingConfigFile(_paths);

        var connectionString = await CreateContextAsync(Program.CreateAppConfiguration(new StartupOptions(), _paths));

        Assert.Equal("from-environment", new NpgsqlConnectionStringBuilder(connectionString).Host);
    }

    [Theory]
    [InlineData("Host=localhost;Database=jellyfin;Password=fake-password-1f9c;Bogus Keyword=1")]
    [InlineData("Host=localhost;Database=jellyfin;Password=fake-password-1f9c;Port=not-a-port")]
    [InlineData("Host=localhost;Database=jellyfin;Password=fake-password-1f9c;Pooling=maybe")]
    [InlineData("Host=localhost;Database=jellyfin;Password=fake-password-1f9c;=1")]
    [InlineData("fake-password-1f9c")]
    public async Task MalformedConnectionString_FailsWithGuidanceAndNeverRepeatsTheConnectionString(string connectionString)
    {
        WriteDatabaseXml(connectionString: null);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigurationKey] = connectionString })
            .Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateContextAsync(configuration));

        Assert.Contains("not valid", exception.Message, StringComparison.Ordinal);
        Assert.Contains("database.xml", exception.Message, StringComparison.Ordinal);
        Assert.Contains("CustomProviderOptions/ConnectionString", exception.Message, StringComparison.Ordinal);
        Assert.Contains(ConfigurationKey, exception.Message, StringComparison.Ordinal);
        Assert.Contains(EnvironmentVariable, exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("fake-password-1f9c", exception.ToString(), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
        }

        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private void WriteDatabaseXml(string? connectionString)
    {
        var customProviderOptions = connectionString is null
            ? string.Empty
            : $"<CustomProviderOptions><PluginName /><PluginAssembly /><ConnectionString>{connectionString}</ConnectionString></CustomProviderOptions>";
        File.WriteAllText(
            Path.Combine(_paths.ConfigurationDirectoryPath, "database.xml"),
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <DatabaseConfigurationOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <DatabaseType>Jellyfin-PostgreSQL</DatabaseType>
              {customProviderOptions}
            </DatabaseConfigurationOptions>
            """);
    }

    private async Task<string> CreateContextAsync(IConfiguration configuration)
    {
        var configurationManager = new ServerConfigurationManager(_paths, new LoggerFactory(), new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IApplicationPaths>(_paths)
            .AddJellyfinDbContext(configurationManager, configuration)
            .BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);

        var factory = serviceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        return context.Database.GetConnectionString()!;
    }
}
