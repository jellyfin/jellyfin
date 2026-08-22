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
/// Until this column existed a person was identified by its raw name, so one human spelled two ways
/// held two lookup rows and two person pages. It cannot be filled in SQL: SQLite cannot strip diacritics.
/// </remarks>
[JellyfinMigration("2026-08-08T00:00:00", nameof(NormalizePeopleIdentity))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class NormalizePeopleIdentity : IAsyncMigrationRoutine
{
    private const int BatchSize = 1000;
    private const string PersonItemType = "MediaBrowser.Controller.Entities.Person";

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

            await FillItemIdsAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string KeyOf(string name) => name.GetCleanValue();

    private async Task FillCleanNamesAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Nearly every name cleans to its lowercase form, which the database can do in one statement.
        // Only what lower() cannot express, such as diacritics, comes back through GetCleanValue.
        await context.Peoples
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CleanName, p => p.Name.ToLower()), cancellationToken)
            .ConfigureAwait(false);

        var stored = await context.Peoples
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.CleanName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Compared against what the database stored, not against an assumption about its lower().
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

    private async Task FillItemIdsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // A credit belongs to the person item whose name cleans to the same key.
        var personItems = await context.BaseItems
            .AsNoTracking()
            .Where(b => b.Type == PersonItemType && b.Name != null)
            .Select(b => new { b.Id, b.Name, b.DateCreated })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Oldest first, so duplicates the merge could not collapse resolve to a stable one.
        var itemsByCleanName = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var item in personItems.OrderBy(p => p.DateCreated))
        {
            var key = KeyOf(item.Name!);
            if (!string.IsNullOrEmpty(key))
            {
                itemsByCleanName.TryAdd(key, item.Id);
            }
        }

        var stored = await context.Peoples
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.CleanName, p.ItemId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Created here so every credit leaves the migration linked, rather than waiting for the first
        // people validation. Ordinal-first per clean name, so the chosen spelling is deterministic.
        var missing = stored
            .Where(p => !itemsByCleanName.ContainsKey(p.CleanName))
            .GroupBy(p => p.CleanName, StringComparer.Ordinal)
            .Select(g => g.Select(p => p.Name).Order(StringComparer.Ordinal).First())
            .ToArray();

        var created = 0;
        foreach (var name in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Null when the folder cannot be created; the credit stays unlinked and is retried later.
            var personItem = _libraryManager.GetOrCreatePerson(name);
            if (personItem is not null)
            {
                itemsByCleanName[KeyOf(name)] = personItem.Id;
                created++;
            }
        }

        _logger.LogInformation("Created {Count} missing person items for credits that had none.", created);

        var links = new List<(Guid Id, Guid ItemId)>();
        foreach (var person in stored)
        {
            if (itemsByCleanName.TryGetValue(person.CleanName, out var itemId) && !person.ItemId.Equals(itemId))
            {
                links.Add((person.Id, itemId));
            }
        }

        _logger.LogInformation(
            "Linked {Count} of {Total} people to their person item.",
            links.Count,
            stored.Count);

        foreach (var batch in links.Chunk(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ids = batch.Select(c => c.Id).ToArray();
            var entities = await context.Peoples
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken)
                .ConfigureAwait(false);

            foreach (var (id, itemId) in batch)
            {
                if (entities.TryGetValue(id, out var entity))
                {
                    entity.ItemId = itemId;
                }
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
        }
    }
}
