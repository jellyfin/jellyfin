#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Fills in Peoples.CleanName and merges the people it reveals to be the same person.
/// </summary>
/// <remarks>
/// A person is identified by its clean name, matching how ItemValues link a studio or genre to its
/// by-name item. Until that column existed a person was identified by its raw name, so the same human
/// spelled with different diacritics or punctuation ("Zoe Saldana" / "Zoe Saldaña") held two lookup
/// rows and two person pages, each carrying half of the filmography. The column cannot be filled in
/// SQL because SQLite has no way to strip diacritics.
/// </remarks>
[JellyfinMigration("2026-08-08T00:00:00", nameof(NormalizePeopleIdentity))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class NormalizePeopleIdentity : IAsyncMigrationRoutine
{
    private const int BatchSize = 1000;

    private readonly IStartupLogger<NormalizePeopleIdentity> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemPersistenceService _persistenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizePeopleIdentity"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="persistenceService">The item persistence service.</param>
    public NormalizePeopleIdentity(
        IStartupLogger<NormalizePeopleIdentity> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILibraryManager libraryManager,
        IItemPersistenceService persistenceService)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _libraryManager = libraryManager;
        _persistenceService = persistenceService;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await FillCleanNamesAsync(context, cancellationToken).ConfigureAwait(false);

            var merger = new DuplicatePeopleMerger(_logger, _libraryManager, _persistenceService);
            await merger.MergePersonBaseItemsAsync(context, KeyOf, "spelling-only", cancellationToken).ConfigureAwait(false);
            await merger.MergePeoplesRowsAsync(context, KeyOf, "spelling-only", cancellationToken).ConfigureAwait(false);
        }
    }

    private static string KeyOf(string name) => name.GetCleanValue();

    private async Task FillCleanNamesAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Nearly every name cleans up to nothing more than its lowercase form, and that much the
        // database can do for the whole table in a single statement. Only what lower() cannot express
        // - diacritics, punctuation, repeated spaces - has to come back through GetCleanValue.
        await context.Peoples
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CleanName, p => p.Name.ToLower()), cancellationToken)
            .ConfigureAwait(false);

        var stored = await context.Peoples
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.CleanName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Compared against what the database actually stored rather than against an assumption about
        // how its lower() treats non-ascii, so this stays correct on any provider.
        var corrections = new List<(Guid Id, string CleanName)>();
        foreach (var person in stored)
        {
            var cleanName = KeyOf(person.Name);
            if (!string.Equals(person.CleanName, cleanName, StringComparison.Ordinal))
            {
                corrections.Add((person.Id, cleanName));
            }
        }

        _logger.LogInformation("Filled in the clean name of {Total} people, correcting {Count} of them.", stored.Count, corrections.Count);

        foreach (var batch in corrections.Chunk(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ids = batch.Select(c => c.Id).ToArray();
            var entities = await context.Peoples
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken)
                .ConfigureAwait(false);

            foreach (var (id, cleanName) in batch)
            {
                if (entities.TryGetValue(id, out var entity))
                {
                    entity.CleanName = cleanName;
                }
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
        }
    }
}
