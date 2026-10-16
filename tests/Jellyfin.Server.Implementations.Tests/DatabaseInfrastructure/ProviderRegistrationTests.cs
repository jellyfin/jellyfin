using System;
using System.IO;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Holds <see cref="EngineTraits.IsRegistered"/> to what the server does: it selects the provider of an engine that is
/// registered, and refuses a database configuration that names one that is not. The trait of an engine whose provider is
/// registered fails here until it is changed, and with it the tests that start the server run on the engine.
/// </summary>
public sealed class ProviderRegistrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-provider-registration-tests", Guid.NewGuid().ToString("N"));

    public static TheoryData<TestDbProvider> Engines()
    {
        var engines = new TheoryData<TestDbProvider>();
        foreach (var provider in Enum.GetValues<TestDbProvider>())
        {
            engines.Add(provider);
        }

        return engines;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public void AddJellyfinDbContext_KeyOfTheEngine_IsSelectedExactlyWhenTheEngineIsRegistered(TestDbProvider provider)
    {
        var traits = EngineTraits.For(provider);

        var refusal = Record.Exception(() => AddJellyfinDbContext(traits.ProviderKey, Path.Combine(_root, provider.ToString())));

        if (traits.IsRegistered)
        {
            Assert.Null(refusal);
        }
        else
        {
            var exception = Assert.IsType<InvalidOperationException>(refusal);
            Assert.Contains($"cannot find the database provider of type '{traits.ProviderKey}'", exception.Message, StringComparison.Ordinal);
        }
    }

    // Registering the database context neither creates the provider nor connects, so no database is needed.
    private static void AddJellyfinDbContext(string key, string root)
    {
        var paths = new ServerApplicationPaths(
            Path.Combine(root, "data"),
            Path.Combine(root, "log"),
            Path.Combine(root, "config"),
            Path.Combine(root, "cache"),
            Path.Combine(root, "web"));
        Directory.CreateDirectory(paths.ConfigurationDirectoryPath);
        var configurationManager = new ServerConfigurationManager(paths, NullLoggerFactory.Instance, new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        configurationManager.SaveConfiguration("database", new DatabaseConfigurationOptions
        {
            DatabaseType = key,
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = string.Empty,
                PluginAssembly = string.Empty,
                ConnectionString = "Server=localhost;Database=jellyfin_registration"
            }
        });

        new ServiceCollection()
            .AddLogging()
            .AddSingleton<IApplicationPaths>(paths)
            .AddJellyfinDbContext(configurationManager, new ConfigurationBuilder().Build());
    }
}
