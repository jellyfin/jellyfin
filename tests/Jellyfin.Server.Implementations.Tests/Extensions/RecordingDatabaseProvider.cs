using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Tests.Extensions;

/// <summary>
/// A database provider that records the configuration it is initialised with and otherwise uses an in-memory SQLite database.
/// It is loaded through the plugin provider path, which needs a public type.
/// </summary>
public sealed class RecordingDatabaseProvider : IJellyfinDatabaseProvider
{
    private readonly IList<DatabaseConfigurationOptions> _received;

    public RecordingDatabaseProvider(IList<DatabaseConfigurationOptions> received)
    {
        _received = received;
    }

    public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
    {
        _received.Add(databaseConfiguration);
        options.UseSqlite("Data Source=:memory:");
    }

    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
    }

    public Task RunScheduledOptimisation(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RunShutdownTask(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<string> MigrationBackupFast(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task RestoreBackupFast(string key, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task DeleteBackup(string key) => throw new NotSupportedException();

    public Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames) => throw new NotSupportedException();
}
