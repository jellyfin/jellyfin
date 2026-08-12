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
            var values = await LegacyItemValues
                .ReadValuesAsync(context, [LegacyItemValues.Genre, LegacyItemValues.Studio], cancellationToken)
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

            // A chunk at a time, because a value has a link for every item carrying it.
            foreach (var chunk in values.Chunk(LegacyItemValues.ValueChunkSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var links = await LegacyItemValues.ReadLinksAsync(context, chunk, cancellationToken).ConfigureAwait(false);
                foreach (var link in links)
                {
                    var value = link.Value;
                    var kind = value.Type == LegacyItemValues.Studio
                        ? BaseItemKind.Studio
                        : _itemTypeLookup.MusicGenreTypes.Contains(link.ItemType)
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
                        if (existingStudioLinks.Add((link.ItemId, byNameId)))
                        {
                            context.BaseItemStudios.Add(new BaseItemStudio
                            {
                                Item = null!,
                                ItemId = link.ItemId,
                                StudioItemId = byNameId
                            });

                            linked++;
                        }
                    }
                    else if (existingGenreLinks.Add((link.ItemId, byNameId)))
                    {
                        context.BaseItemGenres.Add(new BaseItemGenre
                        {
                            Item = null!,
                            ItemId = link.ItemId,
                            GenreItemId = byNameId
                        });

                        linked++;
                    }
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.ChangeTracker.Clear();
            }

            _logger.LogInformation("Created {Links} genre and studio links.", linked);
            if (unresolved > 0)
            {
                _logger.LogWarning("Could not resolve {Count} values to an item; a metadata refresh restores them.", unresolved);
            }

            // Last, so a failure part-way leaves the values to try again from.
            var removed = await LegacyItemValues
                .DeleteAsync(context, [LegacyItemValues.Genre, LegacyItemValues.Studio], cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Removed {Values} genre and studio values.", removed);
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
            .Where(b => typeNames.Contains(b.Type) && b.Name != null)
            .Select(b => new { b.Id, b.Type, b.Name, b.DateCreated })
            .OrderBy(b => b.DateCreated)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            // Cleaned here, because an older server filled the column by an older rule.
            _byNameItems.TryAdd((kindsByTypeName[item.Type!], item.Name!.GetCleanValue()), item.Id);
        }
    }

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
