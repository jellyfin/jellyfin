using System;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Jellyfin.Database.Providers.PgSql;

/// <summary>
/// Configures jellyfin to use an SqLite database.
/// </summary>
[JellyfinDatabaseProviderKey("Jellyfin-PgSql")]
public sealed class PgSqlDatabaseProvider : IJellyfinDatabaseProvider
{
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<PgSqlDatabaseProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgSqlDatabaseProvider"/> class.
    /// </summary>
    /// <param name="configurationManager">Configuration manager to get PgSQL connection data.</param>
    /// <param name="logger">A logger.</param>
    public PgSqlDatabaseProvider(IConfigurationManager configurationManager, ILogger<PgSqlDatabaseProvider> logger)
    {
        _configurationManager = configurationManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder options)
    {
        var dbSettings = _configurationManager.GetConfiguration<DatabaseConfigurationOptions>("database");

        if (dbSettings.PostgreSql is null)
        {
            throw new InvalidOperationException("Selected PgSQL as database provider but did not provide required configuration. Please see docs.");
        }

        var connectionBuilder = new NpgsqlConnectionStringBuilder();
        connectionBuilder.ApplicationName = "jellyfin";
        connectionBuilder.CommandTimeout = dbSettings.PostgreSql.Timeout;
        connectionBuilder.Database = dbSettings.PostgreSql.DatabaseName;
        connectionBuilder.Username = dbSettings.PostgreSql.Username;
        connectionBuilder.Password = dbSettings.PostgreSql.Password;
        connectionBuilder.Host = dbSettings.PostgreSql.ServerName;
        connectionBuilder.Port = dbSettings.PostgreSql.Port;

        var connectionString = connectionBuilder.ToString();

        options
            .UseNpgsql(connectionString, pgSqlOptions => pgSqlOptions.MigrationsAssembly(GetType().Assembly));
    }

    /// <inheritdoc/>
    public Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
