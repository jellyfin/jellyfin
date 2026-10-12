using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public class PostgresDatabaseProviderInitialiseTests
{
    [Theory]
    [InlineData("Host=db;Database=jellyfin;Username=jellyfin;Password=s3cret-value")]
    [InlineData("Host=db;Database=jellyfin;Username=jellyfin;Pwd=s3cret-value")]
    [InlineData("Host=db;Database=jellyfin;Username=jellyfin;Password=s3cret-value;SSL Password=s3cret-value")]
    public void Initialise_ConnectionStringWithPassword_NeverLogsThePassword(string connectionString)
    {
        var logger = new CollectingLogger<PostgresDatabaseProvider>();
        var provider = new PostgresDatabaseProvider(logger);

        provider.Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration(connectionString));

        Assert.NotEmpty(logger.Entries);
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("s3cret-value", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, e => e.Message.Contains("Host=db", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Initialise_WithoutConnectionString_ThrowsAndNamesTheSettings(string connectionString)
    {
        var provider = new PostgresDatabaseProvider(new CollectingLogger<PostgresDatabaseProvider>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.Initialise(new DbContextOptionsBuilder<JellyfinDbContext>(), CreateConfiguration(connectionString)));

        Assert.Contains("CustomProviderOptions/ConnectionString", ex.Message, StringComparison.Ordinal);
        Assert.Contains("database:ConnectionString", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Host=db;Database=jellyfin", 60)]
    [InlineData("Host=db;Database=jellyfin;Command Timeout=15", 15)]
    [InlineData("Host=db;Database=jellyfin;CommandTimeout=15", 15)]
    [InlineData("Host=db;Database=jellyfin;command timeout=15", 15)]
    public void Initialise_CommandTimeout_DefaultsOnlyWhenTheConnectionStringHasNone(string connectionString, int expected)
    {
        var options = Initialise(connectionString);

        Assert.Equal(expected, ReadCommandTimeout(options));
    }

    [Fact]
    public void Initialise_CommandTimeoutOption_OverridesTheConnectionString()
    {
        var options = Initialise(
            "Host=db;Database=jellyfin;Command Timeout=15",
            new CustomDatabaseOption { Key = "command-timeout", Value = "120" });

        Assert.Equal(120, ReadCommandTimeout(options));
    }

    [Theory]
    [InlineData("Host=db;Database=jellyfin")]
    [InlineData("Host=db;Database=jellyfin;Timezone=Europe/Paris")]
    public void Initialise_Timezone_IsAlwaysUtc(string connectionString)
    {
        var options = Initialise(connectionString);

        var effective = RelationalOptionsExtension.Extract(options.Options).ConnectionString;
        Assert.Equal("UTC", new NpgsqlConnectionStringBuilder(effective).Timezone);
    }

    private static DbContextOptionsBuilder<JellyfinDbContext> Initialise(string connectionString, params CustomDatabaseOption[] providerOptions)
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();
        new PostgresDatabaseProvider(new CollectingLogger<PostgresDatabaseProvider>())
            .Initialise(options, CreateConfiguration(connectionString, providerOptions));
        return options;
    }

    private static int ReadCommandTimeout(DbContextOptionsBuilder<JellyfinDbContext> options)
    {
        var connectionString = RelationalOptionsExtension.Extract(options.Options).ConnectionString;
        return new NpgsqlConnectionStringBuilder(connectionString).CommandTimeout;
    }

    private static DatabaseConfigurationOptions CreateConfiguration(string connectionString, params CustomDatabaseOption[] providerOptions) => new()
    {
        DatabaseType = "Jellyfin-PostgreSQL",
        CustomProviderOptions = new CustomDatabaseOptions
        {
            PluginName = string.Empty,
            PluginAssembly = string.Empty,
            ConnectionString = connectionString,
            Options = [.. providerOptions]
        }
    };
}
