#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Turns the genre and studio values an item carries into links to the items they belong to.
/// </summary>
[JellyfinMigration("2026-08-16T00:00:00", nameof(ConvertGenreAndStudioValuesToLinks))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class ConvertGenreAndStudioValuesToLinks : IAsyncMigrationRoutine
{
    private const int BatchSize = 500;

    private static readonly ItemValueType[] _convertedValueTypes = [ItemValueType.Genre, ItemValueType.Studios];

    private readonly IStartupLogger<ConvertGenreAndStudioValuesToLinks> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemTypeLookup _itemTypeLookup;

    // By kind as well as clean name: a genre and a music genre of one name are two items.
    private readonly Dictionary<(BaseItemKind Kind, string CleanName), Guid> _byNameItems = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertGenreAndStudioValuesToLinks"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="itemTypeLookup">The static type lookup.</param>
    public ConvertGenreAndStudioValuesToLinks(
        IStartupLogger<ConvertGenreAndStudioValuesToLinks> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILibraryManager libraryManager,
        IItemTypeLookup itemTypeLookup)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _libraryManager = libraryManager;
        _itemTypeLookup = itemTypeLookup;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var values = await context.ItemValues
                .AsNoTracking()
                .Where(v => _convertedValueTypes.Contains(v.Type))
                .Select(v => new { v.ItemValueId, v.Type, v.Value, v.CleanValue })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (values.Count == 0)
            {
                _logger.LogInformation("No genre or studio values to convert.");
                return;
            }

            _logger.LogInformation("Converting {Count} genre and studio values into links.", values.Count);

            await LoadByNameItemsAsync(context, cancellationToken).ConfigureAwait(false);

            var existingGenreLinks = (await context.BaseItemGenres
                    .AsNoTracking()
                    .Select(g => new { g.ItemId, g.GenreItemId })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(g => (g.ItemId, g.GenreItemId))
                .ToHashSet();
            var existingStudioLinks = (await context.BaseItemStudios
                    .AsNoTracking()
                    .Select(s => new { s.ItemId, s.StudioItemId })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(s => (s.ItemId, s.StudioItemId))
                .ToHashSet();

            var linked = 0;
            var unresolved = 0;

            foreach (var batch in values.Chunk(BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var valueIds = batch.Select(v => v.ItemValueId).ToArray();
                var maps = (await context.ItemValuesMap
                        .AsNoTracking()
                        .Where(m => valueIds.Contains(m.ItemValueId))
                        .Select(m => new { m.ItemValueId, m.ItemId, ItemType = m.Item.Type })
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false))
                    .ToLookup(m => m.ItemValueId, m => (m.ItemId, m.ItemType));

                if (maps.Count == 0)
                {
                    continue;
                }

                foreach (var value in batch)
                {
                    foreach (var (itemId, itemType) in maps[value.ItemValueId])
                    {
                        var kind = value.Type == ItemValueType.Studios
                            ? BaseItemKind.Studio
                            : _itemTypeLookup.MusicGenreTypes.Contains(itemType)
                                ? BaseItemKind.MusicGenre
                                : BaseItemKind.Genre;

                        var byNameId = ResolveByNameItem(kind, value.Value, value.CleanValue);
                        if (byNameId.IsEmpty())
                        {
                            unresolved++;
                            continue;
                        }

                        if (kind == BaseItemKind.Studio)
                        {
                            if (existingStudioLinks.Add((itemId, byNameId)))
                            {
                                context.BaseItemStudios.Add(new BaseItemStudio
                                {
                                    Item = null!,
                                    ItemId = itemId,
                                    StudioItemId = byNameId
                                });

                                linked++;
                            }
                        }
                        else if (existingGenreLinks.Add((itemId, byNameId)))
                        {
                            context.BaseItemGenres.Add(new BaseItemGenre
                            {
                                Item = null!,
                                ItemId = itemId,
                                GenreItemId = byNameId
                            });

                            linked++;
                        }
                    }
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Created {Links} genre and studio links.", linked);
            if (unresolved > 0)
            {
                _logger.LogWarning("Could not resolve {Count} values to an item; a metadata refresh restores them.", unresolved);
            }

            // Last, so a failure part-way leaves the values to try again from.
            var removedMaps = await context.ItemValuesMap
                .Where(m => _convertedValueTypes.Contains(m.ItemValue.Type))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            var removedValues = await context.ItemValues
                .Where(v => _convertedValueTypes.Contains(v.Type))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Removed {Values} genre and studio values and {Maps} of their item links.", removedValues, removedMaps);
        }
    }

    // Oldest first, so a library holding two items for one name resolves to a stable one.
    private async Task LoadByNameItemsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        var kindsByTypeName = new[] { BaseItemKind.Genre, BaseItemKind.MusicGenre, BaseItemKind.Studio }
            .ToDictionary(k => _itemTypeLookup.BaseItemKindNames[k], k => k, StringComparer.Ordinal);
        var typeNames = kindsByTypeName.Keys.ToArray();

        var items = await context.BaseItems
            .AsNoTracking()
            .Where(b => typeNames.Contains(b.Type) && b.CleanName != null)
            .Select(b => new { b.Id, b.Type, b.CleanName, b.DateCreated })
            .OrderBy(b => b.DateCreated)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            _byNameItems.TryAdd((kindsByTypeName[item.Type!], item.CleanName!), item.Id);
        }
    }

    // Empty loses that one name rather than the whole migration.
    private Guid ResolveByNameItem(BaseItemKind kind, string name, string cleanName)
    {
        var key = (kind, cleanName);
        if (_byNameItems.TryGetValue(key, out var id))
        {
            return id;
        }

        try
        {
            BaseItem item = kind switch
            {
                BaseItemKind.Studio => _libraryManager.GetStudio(name),
                BaseItemKind.MusicGenre => _libraryManager.GetMusicGenre(name),
                _ => _libraryManager.GetGenre(name)
            };

            id = item.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get or create {Kind} {Name}", kind, name);
            id = Guid.Empty;
        }

        _byNameItems[key] = id;
        return id;
    }
}
