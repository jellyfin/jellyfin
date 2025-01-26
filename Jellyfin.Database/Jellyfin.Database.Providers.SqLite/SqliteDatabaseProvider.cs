using System;
using Jellyfin.Server.Implementations;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.SqLite;

/// <summary>
/// Configures jellyfin to use an SqLite database.
/// </summary>
public sealed class SqliteDatabaseProvider : IJellyfinDatabaseProvider
{
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<SqliteDatabaseProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDatabaseProvider"/> class.
    /// </summary>
    /// <param name="dbContextFactory">The Db context to interact with the database.</param>
    /// <param name="applicationPaths">Service to construct the fallback when the old data path configuration is used.</param>
    /// <param name="logger">A logger.</param>
    public SqliteDatabaseProvider(IDbContextFactory<JellyfinDbContext> dbContextFactory, IApplicationPaths applicationPaths, ILogger<SqliteDatabaseProvider> logger)
    {
        DbContextFactory = dbContextFactory;
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    private IDbContextFactory<JellyfinDbContext> DbContextFactory { get; }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder options)
    {
        options.UseSqlite(
            $"Filename={Path.Combine(_applicationPaths.DataPath, "jellyfin.db")};Pooling=false",
            sqLiteOptions => sqLiteOptions.MigrationsAssembly(GetType().Assembly));
    }

    /// <inheritdoc/>
    public async Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            if (context.Database.IsSqlite())
            {
                await context.Database.ExecuteSqlRawAsync("PRAGMA optimize", cancellationToken).ConfigureAwait(false);
                await context.Database.ExecuteSqlRawAsync("VACUUM", cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("jellyfin.db optimized successfully!");
            }
            else
            {
                _logger.LogInformation("This database doesn't support optimization");
            }
        }
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SetDefaultDateTimeKind(DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Run before disposing the application
        var context = await DbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA optimize").ConfigureAwait(false);
        }

        SqliteConnection.ClearAllPools();
    }
}
