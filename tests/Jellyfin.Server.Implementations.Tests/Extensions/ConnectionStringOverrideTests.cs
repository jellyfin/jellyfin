using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Server.Helpers;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Extensions;

/// <summary>
/// Covers the configuration override of the database connection string: it reaches the provider, and it is never persisted.
/// </summary>
public sealed class ConnectionStringOverrideTests : IDisposable
{
    private const string OverrideKey = "database:ConnectionString";
    private const string OverrideEnvironmentVariable = "JELLYFIN_DATABASE__CONNECTIONSTRING";
    private const string OriginalConnectionString = "Host=original;Username=jf;Password=file-secret";
    private const string OverrideConnectionString = "Host=override;Username=jf;Password=env-secret";
    private const string PluginName = "RecordingProvider";

    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly List<ServiceProvider> _serviceProviders = [];
    private readonly List<DatabaseConfigurationOptions> _received = [];
    private readonly CapturingLoggerProvider _logs = new();

    public ConnectionStringOverrideTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-connection-string-override-tests", Guid.NewGuid().ToString("N"));
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
        Directory.CreateDirectory(_paths.PluginsPath);
    }

    private static string PluginAssemblyFileName => Path.GetFileName(typeof(RecordingDatabaseProvider).Assembly.Location);

    private string DatabaseConfigurationPath => Path.Combine(_paths.ConfigurationDirectoryPath, "database.xml");

    [Fact]
    public async Task AddJellyfinDbContext_OverrideSet_PluginProviderReceivesOverride()
    {
        WritePluginConfiguration();

        await InitialiseProviderAsync(CreateConfigurationManager(), OverrideConfiguration(OverrideConnectionString));

        var received = Assert.Single(_received);
        Assert.Equal("PLUGIN_PROVIDER", received.DatabaseType);
        Assert.Equal(DatabaseLockingBehaviorTypes.Pessimistic, received.LockingBehavior);
        var options = Assert.IsType<CustomDatabaseOptions>(received.CustomProviderOptions);
        Assert.Equal(OverrideConnectionString, options.ConnectionString);
        Assert.Equal(PluginName, options.PluginName);
        Assert.Equal(PluginAssemblyFileName, options.PluginAssembly);
        var option = Assert.Single(options.Options);
        Assert.Equal("command-timeout", option.Key);
        Assert.Equal("30", option.Value);
    }

    [Fact]
    public async Task AddJellyfinDbContext_OverrideSet_CachedAndStoredConfigurationKeepOriginal()
    {
        WritePluginConfiguration();
        var configurationManager = CreateConfigurationManager();

        await InitialiseProviderAsync(configurationManager, OverrideConfiguration(OverrideConnectionString));

        var received = Assert.Single(_received);
        var cached = configurationManager.GetConfiguration<DatabaseConfigurationOptions>("database");
        Assert.NotSame(cached, received);
        Assert.NotSame(cached.CustomProviderOptions, received.CustomProviderOptions);
        Assert.Equal(OriginalConnectionString, cached.CustomProviderOptions!.ConnectionString);
        var onDisk = await File.ReadAllTextAsync(DatabaseConfigurationPath, TestContext.Current.CancellationToken);
        Assert.Contains(OriginalConnectionString, onDisk, StringComparison.Ordinal);
        Assert.DoesNotContain(OverrideConnectionString, onDisk, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddJellyfinDbContext_OverrideSet_LaterSaveOfDatabaseConfigurationDoesNotPersistOverride()
    {
        WritePluginConfiguration();
        var configurationManager = CreateConfigurationManager();

        await InitialiseProviderAsync(configurationManager, OverrideConfiguration(OverrideConnectionString));
        configurationManager.SaveConfiguration("database", configurationManager.GetConfiguration("database"));

        var onDisk = await File.ReadAllTextAsync(DatabaseConfigurationPath, TestContext.Current.CancellationToken);
        Assert.Contains(OriginalConnectionString, onDisk, StringComparison.Ordinal);
        Assert.DoesNotContain(OverrideConnectionString, onDisk, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddJellyfinDbContext_OverrideSetAndNoDatabaseConfiguration_WritesDefaultConfigurationWithoutOverride()
    {
        var configurationManager = CreateConfigurationManager();

        await InitialiseProviderAsync(configurationManager, OverrideConfiguration(OverrideConnectionString));

        Assert.True(File.Exists(DatabaseConfigurationPath));
        var onDisk = await File.ReadAllTextAsync(DatabaseConfigurationPath, TestContext.Current.CancellationToken);
        Assert.Contains("Jellyfin-SQLite", onDisk, StringComparison.Ordinal);
        Assert.DoesNotContain(OverrideConnectionString, onDisk, StringComparison.Ordinal);
        Assert.DoesNotContain("env-secret", onDisk, StringComparison.Ordinal);
        var cached = configurationManager.GetConfiguration<DatabaseConfigurationOptions>("database");
        Assert.Null(cached.CustomProviderOptions);
    }

    [Fact]
    public async Task AddJellyfinDbContext_NoOverride_PluginProviderReceivesStoredConfiguration()
    {
        WritePluginConfiguration();
        var configurationManager = CreateConfigurationManager();

        await InitialiseProviderAsync(configurationManager, new ConfigurationBuilder().Build());

        var received = Assert.Single(_received);
        Assert.Same(configurationManager.GetConfiguration<DatabaseConfigurationOptions>("database"), received);
        Assert.Equal(OriginalConnectionString, received.CustomProviderOptions!.ConnectionString);
        Assert.DoesNotContain(_logs.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains(OverrideKey, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddJellyfinDbContext_BlankOverride_PluginProviderReceivesStoredConfiguration(string value)
    {
        WritePluginConfiguration();
        var configurationManager = CreateConfigurationManager();

        await InitialiseProviderAsync(configurationManager, OverrideConfiguration(value));

        var received = Assert.Single(_received);
        Assert.Same(configurationManager.GetConfiguration<DatabaseConfigurationOptions>("database"), received);
        Assert.Equal(OriginalConnectionString, received.CustomProviderOptions!.ConnectionString);
    }

    [Fact]
    public async Task AddJellyfinDbContext_OverrideSetForSqlite_ProviderStillWorks()
    {
        WriteConfiguration(new DatabaseConfigurationOptions { DatabaseType = "Jellyfin-SQLite" });
        var configurationManager = CreateConfigurationManager();
        using var serviceProvider = BuildServiceProvider(configurationManager, OverrideConfiguration(OverrideConnectionString));

        var factory = serviceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        Assert.True(context.Database.IsSqlite());
        Assert.Contains("jellyfin.db", context.Database.GetConnectionString(), StringComparison.Ordinal);
        Assert.DoesNotContain("env-secret", context.Database.GetConnectionString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddJellyfinDbContext_EnvironmentVariable_MapsToOverrideKey()
    {
        WritePluginConfiguration();
        await StartupHelpers.InitLoggingConfigFile(_paths);
        var previous = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        IConfiguration configuration;
        try
        {
            Environment.SetEnvironmentVariable(OverrideEnvironmentVariable, OverrideConnectionString);
            configuration = Program.CreateAppConfiguration(new StartupOptions(), _paths);
        }
        finally
        {
            Environment.SetEnvironmentVariable(OverrideEnvironmentVariable, previous);
        }

        Assert.Equal(OverrideConnectionString, configuration[OverrideKey]);

        await InitialiseProviderAsync(CreateConfigurationManager(), configuration);

        var received = Assert.Single(_received);
        Assert.Equal(OverrideConnectionString, received.CustomProviderOptions!.ConnectionString);
    }

    [Fact]
    public async Task AddJellyfinDbContext_OverrideSet_LogsKeyOnceAndNeverTheValue()
    {
        WritePluginConfiguration();

        await InitialiseProviderAsync(CreateConfigurationManager(), OverrideConfiguration(OverrideConnectionString));

        var entry = Assert.Single(_logs.Entries, e => e.Level == LogLevel.Information && e.Message.Contains(OverrideKey, StringComparison.Ordinal));
        Assert.Null(entry.Exception);
        foreach (var logged in _logs.Entries)
        {
            Assert.DoesNotContain("env-secret", logged.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("Host=override", logged.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("env-secret", logged.State, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WithConnectionStringOverride_NoCustomProviderOptions_CreatesThemInTheCopyOnly()
    {
        var source = new DatabaseConfigurationOptions
        {
            DatabaseType = "Jellyfin-SQLite",
            LockingBehavior = DatabaseLockingBehaviorTypes.Optimistic
        };

        var copy = ServiceCollectionExtensions.WithConnectionStringOverride(source, OverrideConnectionString);

        Assert.NotSame(source, copy);
        Assert.Null(source.CustomProviderOptions);
        Assert.Equal("Jellyfin-SQLite", copy.DatabaseType);
        Assert.Equal(DatabaseLockingBehaviorTypes.Optimistic, copy.LockingBehavior);
        var options = Assert.IsType<CustomDatabaseOptions>(copy.CustomProviderOptions);
        Assert.Equal(OverrideConnectionString, options.ConnectionString);
        Assert.Equal(string.Empty, options.PluginName);
        Assert.Equal(string.Empty, options.PluginAssembly);
        Assert.Empty(options.Options);
    }

    [Fact]
    public void WithConnectionStringOverride_ExistingOptions_CopiesThemWithoutSharingState()
    {
        var source = new DatabaseConfigurationOptions
        {
            DatabaseType = "Jellyfin-PostgreSQL",
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = "name",
                PluginAssembly = "assembly",
                ConnectionString = OriginalConnectionString,
                Options = [new CustomDatabaseOption { Key = "key", Value = "value" }]
            }
        };

        var copy = ServiceCollectionExtensions.WithConnectionStringOverride(source, OverrideConnectionString);
        copy.CustomProviderOptions!.Options.Single().Value = "changed";
        copy.CustomProviderOptions.Options.Add(new CustomDatabaseOption { Key = "added", Value = "added" });

        Assert.Equal(OriginalConnectionString, source.CustomProviderOptions.ConnectionString);
        var option = Assert.Single(source.CustomProviderOptions.Options);
        Assert.Equal("value", option.Value);
        Assert.Equal("name", copy.CustomProviderOptions.PluginName);
        Assert.Equal("assembly", copy.CustomProviderOptions.PluginAssembly);
    }

    [Fact]
    public void WithConnectionStringOverride_NullOptions_CopiesAnEmptyCollection()
    {
        // Options is not nullable, but the configuration can reach us through the settings API as well as through the XML file.
        var source = new DatabaseConfigurationOptions
        {
            DatabaseType = "Jellyfin-PostgreSQL",
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = "name",
                PluginAssembly = "assembly",
                ConnectionString = OriginalConnectionString,
                Options = null!
            }
        };

        var copy = ServiceCollectionExtensions.WithConnectionStringOverride(source, OverrideConnectionString);

        Assert.Empty(copy.CustomProviderOptions!.Options);
        Assert.Equal(OverrideConnectionString, copy.CustomProviderOptions.ConnectionString);
        Assert.Null(source.CustomProviderOptions.Options);
    }

    [Fact]
    public void TestAssembly_ExposesExactlyOneDatabaseProvider()
    {
        // The plugin loader takes the first exported type that is a database provider. The plugin tests copy this
        // assembly as the plugin, so any second provider in here could be picked instead of RecordingDatabaseProvider.
        var providers = typeof(RecordingDatabaseProvider).Assembly
            .GetExportedTypes()
            .Where(type => type.IsAssignableTo(typeof(IJellyfinDatabaseProvider)))
            .ToList();

        Assert.True(
            providers.Count == 1 && providers[0] == typeof(RecordingDatabaseProvider),
            "RecordingDatabaseProvider must be the only public IJellyfinDatabaseProvider in this assembly, found: "
            + string.Join(", ", providers.Select(type => type.FullName)));
    }

    [Fact]
    public void WithConnectionStringOverride_EveryPublicProperty_IsCarriedIntoTheCopy()
    {
        // Guards the hand-written copy: a property added to one of the option types later
        // must be copied too, otherwise it would silently reset to its default whenever the
        // override is used. The copy is meant to be fully independent of the source, so
        // reference-typed values must be equal but never shared.
        var source = (DatabaseConfigurationOptions)Populate(typeof(DatabaseConfigurationOptions), "root");

        var copy = ServiceCollectionExtensions.WithConnectionStringOverride(source, OverrideConnectionString);

        Assert.NotSame(source, copy);
        AssertCarried(source, copy, "root", [nameof(CustomDatabaseOptions.ConnectionString)]);
        Assert.Equal(OverrideConnectionString, copy.CustomProviderOptions!.ConnectionString);
        Assert.NotEqual(OverrideConnectionString, source.CustomProviderOptions!.ConnectionString);
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
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

    /// <summary>
    /// Creates an instance of <paramref name="type"/> with every public settable property set to a non-default value.
    /// Fails for a property type it does not know, so that a new kind of property forces this guard to be extended.
    /// </summary>
    private static object Populate(Type type, string path)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var property in PublicSettableProperties(type))
        {
            var value = CreateValue(property.PropertyType, path + "." + property.Name);
            property.SetValue(instance, value);
            Assert.NotEqual(property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null, value);
        }

        return instance;
    }

    private static object CreateValue(Type type, string path)
    {
        if (type == typeof(string))
        {
            return "value-of-" + path;
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).Cast<object>().First(member => !Equals(member, Activator.CreateInstance(type)));
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Collection<>))
        {
            var collection = (IList)Activator.CreateInstance(type)!;
            collection.Add(Populate(type.GetGenericArguments()[0], path + "[0]"));
            return collection;
        }

        if (type.IsClass && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            return Populate(type, path);
        }

        throw new InvalidOperationException($"{path}: the guard test cannot populate a property of type {type}, extend it.");
    }

    private static void AssertCarried(object? source, object? copy, string path, string[] overriddenNames)
    {
        Assert.NotNull(source);
        Assert.NotNull(copy);
        var type = source.GetType();
        Assert.Equal(type, copy.GetType());

        foreach (var property in PublicSettableProperties(type))
        {
            var propertyPath = path + "." + property.Name;
            var sourceValue = property.GetValue(source);
            var copyValue = property.GetValue(copy);
            if (overriddenNames.Contains(property.Name, StringComparer.Ordinal))
            {
                Assert.True(copyValue is not null && !Equals(sourceValue, copyValue), propertyPath + " should hold the override");
            }
            else if (sourceValue is string or Enum)
            {
                Assert.True(Equals(sourceValue, copyValue), $"{propertyPath} was not carried into the copy");
            }
            else if (sourceValue is IList sourceList)
            {
                var copyList = Assert.IsAssignableFrom<IList>(copyValue);
                Assert.False(ReferenceEquals(sourceList, copyList), propertyPath + " is shared with the source");
                Assert.True(sourceList.Count == copyList.Count, propertyPath + " lost items in the copy");
                for (var i = 0; i < sourceList.Count; i++)
                {
                    Assert.False(ReferenceEquals(sourceList[i], copyList[i]), $"{propertyPath}[{i}] is shared with the source");
                    AssertCarried(sourceList[i], copyList[i], $"{propertyPath}[{i}]", []);
                }
            }
            else
            {
                Assert.False(ReferenceEquals(sourceValue, copyValue), propertyPath + " is shared with the source");
                AssertCarried(sourceValue, copyValue, propertyPath, overriddenNames);
            }
        }
    }

    private static PropertyInfo[] PublicSettableProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0 && property.SetMethod is { IsPublic: true })
            .ToArray();

    private static IConfiguration OverrideConfiguration(string value)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [OverrideKey] = value }).Build();

    private void WritePluginConfiguration()
    {
        var pluginDirectory = Path.Combine(_paths.PluginsPath, PluginName + "_1.0.0");
        Directory.CreateDirectory(pluginDirectory);
        File.Copy(typeof(RecordingDatabaseProvider).Assembly.Location, Path.Combine(pluginDirectory, PluginAssemblyFileName), true);

        WriteConfiguration(new DatabaseConfigurationOptions
        {
            DatabaseType = "PLUGIN_PROVIDER",
            LockingBehavior = DatabaseLockingBehaviorTypes.Pessimistic,
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = PluginName,
                PluginAssembly = PluginAssemblyFileName,
                ConnectionString = OriginalConnectionString,
                Options = [new CustomDatabaseOption { Key = "command-timeout", Value = "30" }]
            }
        });
    }

    private void WriteConfiguration(DatabaseConfigurationOptions configuration)
        => new MyXmlSerializer().SerializeToFile(configuration, DatabaseConfigurationPath);

    private ServerConfigurationManager CreateConfigurationManager()
    {
        var configurationManager = new ServerConfigurationManager(_paths, NullLoggerFactory.Instance, new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        return configurationManager;
    }

    private ServiceProvider BuildServiceProvider(ServerConfigurationManager configurationManager, IConfiguration configuration)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(_logs))
            .AddSingleton<IList<DatabaseConfigurationOptions>>(_received)
            .AddSingleton<IApplicationPaths>(_paths)
            .AddJellyfinDbContext(configurationManager, configuration)
            .BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);
        return serviceProvider;
    }

    private async Task InitialiseProviderAsync(ServerConfigurationManager configurationManager, IConfiguration configuration)
    {
        var serviceProvider = BuildServiceProvider(configurationManager, configuration);
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly object _gate = new();
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_gate)
                {
                    return _entries.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        internal sealed record LogEntry(LogLevel Level, string Message, string State, Exception? Exception);

        private sealed class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerProvider _provider;

            public CapturingLogger(CapturingLoggerProvider provider)
            {
                _provider = provider;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
                    ? string.Join(';', pairs.Select(pair => string.Format(CultureInfo.InvariantCulture, "{0}={1}", pair.Key, pair.Value)))
                    : string.Empty;
                lock (_provider._gate)
                {
                    _provider._entries.Add(new LogEntry(logLevel, formatter(state, exception), values, exception));
                }
            }
        }
    }
}
