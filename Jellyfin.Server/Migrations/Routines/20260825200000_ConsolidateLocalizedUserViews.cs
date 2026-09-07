using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Moves the views whose id used to be derived from their localized name onto their name independent id.
/// </summary>
[JellyfinMigration("2026-08-25T20:00:00", nameof(ConsolidateLocalizedUserViews))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class ConsolidateLocalizedUserViews : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<ConsolidateLocalizedUserViews> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IFileSystem _fileSystem;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolidateLocalizedUserViews"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="dbProvider">The database context factory.</param>
    public ConsolidateLocalizedUserViews(
        IStartupLogger<ConsolidateLocalizedUserViews> logger,
        ILibraryManager libraryManager,
        IServerConfigurationManager configurationManager,
        IFileSystem fileSystem,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _configurationManager = configurationManager;
        _fileSystem = fileSystem;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        // The Live TV view is the one that hurts: every channel and program is parented to it, so a
        // translation update or a change of UI culture used to leave them behind under a view nothing
        // looks up any more.
        var views = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.UserView]
        }).OfType<UserView>().Where(view => view.ViewType.HasValue).ToArray();

        if (views.Length == 0)
        {
            return;
        }

        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            foreach (var group in views.GroupBy(view => view.ViewType!.Value))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var viewType = group.Key;
                var folderName = _fileSystem.GetValidFilename(viewType.ToString());
                var path = Path.Combine(_configurationManager.ApplicationPaths.InternalMetadataPath, "views", folderName);

                // Only the views created for a view type as a whole are named after it. The per user and
                // per parent ones get a folder of their own, and carry no children to lose. Match on the
                // folder rather than the whole path so a metadata directory that has since moved still
                // lines up.
                var candidates = group
                    .Where(view => !string.IsNullOrEmpty(view.Path)
                        && string.Equals(Path.GetFileName(view.Path.TrimEnd(Path.DirectorySeparatorChar)), folderName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (candidates.Length == 0)
                {
                    continue;
                }

                // Mirrors LibraryManager.GetNamedView(name, viewType, sortName).
                var canonicalId = _libraryManager.GetNewItemId(path + "_namedview_" + viewType.ToString(), typeof(UserView));

                var stale = candidates.Where(view => !view.Id.Equals(canonicalId)).ToArray();
                if (stale.Length == 0)
                {
                    continue;
                }

                await ConsolidateAsync(dbContext, viewType, path, canonicalId, candidates, stale, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ConsolidateAsync(
        JellyfinDbContext dbContext,
        CollectionType viewType,
        string path,
        Guid canonicalId,
        IReadOnlyList<UserView> candidates,
        IReadOnlyList<UserView> stale,
        CancellationToken cancellationToken)
    {
        var staleIds = stale.Select(view => view.Id).ToArray();
        Guid? newParentId = canonicalId;
        var sourceId = Guid.Empty;

        if (!candidates.Any(view => view.Id.Equals(canonicalId)))
        {
            // Whichever of the old views the items ended up under is the one worth keeping, so give the
            // canonical id a copy of it.
            var source = await PickSourceAsync(dbContext, stale, staleIds, cancellationToken).ConfigureAwait(false);
            sourceId = source.Id;

            _libraryManager.CreateItem(
                new UserView
                {
                    Path = path,
                    Id = canonicalId,
                    DateCreated = source.DateCreated,
                    DateModified = source.DateModified,
                    Name = source.Name,
                    ViewType = viewType,
                    ForcedSortName = source.ForcedSortName
                },
                null);
        }

        var reparented = await dbContext.BaseItems
            .Where(e => e.ParentId.HasValue)
            .WhereOneOrMany(staleIds, e => e.ParentId!.Value)
            .ExecuteUpdateAsync(e => e.SetProperty(f => f.ParentId, newParentId), cancellationToken)
            .ConfigureAwait(false);

        await dbContext.BaseItems
            .Where(e => e.TopParentId.HasValue)
            .WhereOneOrMany(staleIds, e => e.TopParentId!.Value)
            .ExecuteUpdateAsync(e => e.SetProperty(f => f.TopParentId, newParentId), cancellationToken)
            .ConfigureAwait(false);

        await MoveAncestorsAsync(dbContext, canonicalId, staleIds, cancellationToken).ConfigureAwait(false);
        await MoveUserSettingsAsync(dbContext, canonicalId, sourceId, staleIds, cancellationToken).ConfigureAwait(false);

        // Nothing points at them any more, and BaseItems cascades on ParentId, so this has to come last.
        await dbContext.BaseItems
            .WhereOneOrMany(staleIds, e => e.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Moved {Reparented} items and dropped {Stale} stale {ViewType} views in favour of {CanonicalId}",
            reparented,
            staleIds.Length,
            viewType,
            canonicalId);
    }

    private async Task<UserView> PickSourceAsync(
        JellyfinDbContext dbContext,
        IReadOnlyList<UserView> stale,
        IReadOnlyList<Guid> staleIds,
        CancellationToken cancellationToken)
    {
        var childCounts = await dbContext.BaseItems
            .Where(e => e.ParentId.HasValue)
            .WhereOneOrMany(staleIds, e => e.ParentId!.Value)
            .GroupBy(e => e.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(e => e.ParentId, e => e.Count, cancellationToken)
            .ConfigureAwait(false);

        return stale
            .OrderByDescending(view => childCounts.GetValueOrDefault(view.Id))
            .ThenBy(view => view.DateCreated)
            .First();
    }

    private static async Task MoveUserSettingsAsync(
        JellyfinDbContext dbContext,
        Guid canonicalId,
        Guid sourceId,
        IReadOnlyList<Guid> staleIds,
        CancellationToken cancellationToken)
    {
        // Everything below is keyed by the view's id, and a view holding no children still holds the
        // ordering it was given and whether it was hidden. Only the view that was promoted can hand
        // those over - the rest would collide on the one row per user, item and client - so the others
        // are dropped instead.
        var dropped = staleIds.Where(id => !id.Equals(sourceId)).ToArray();

        if (!sourceId.Equals(Guid.Empty))
        {
            var moved = new[] { sourceId };

            await dbContext.DisplayPreferences
                .WhereOneOrMany(moved, e => e.ItemId)
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.ItemId, canonicalId), cancellationToken)
                .ConfigureAwait(false);

            await dbContext.ItemDisplayPreferences
                .WhereOneOrMany(moved, e => e.ItemId)
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.ItemId, canonicalId), cancellationToken)
                .ConfigureAwait(false);

            await dbContext.CustomItemDisplayPreferences
                .WhereOneOrMany(moved, e => e.ItemId)
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.ItemId, canonicalId), cancellationToken)
                .ConfigureAwait(false);
        }

        if (dropped.Length > 0)
        {
            await dbContext.DisplayPreferences.WhereOneOrMany(dropped, e => e.ItemId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await dbContext.ItemDisplayPreferences.WhereOneOrMany(dropped, e => e.ItemId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await dbContext.CustomItemDisplayPreferences.WhereOneOrMany(dropped, e => e.ItemId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        var stale = staleIds.ToHashSet();
        var preferences = await dbContext.Preferences
            .Where(e => e.Kind == PreferenceKind.OrderedViews || e.Kind == PreferenceKind.MyMediaExcludes)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var changed = false;

        foreach (var preference in preferences)
        {
            var values = preference.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var rewritten = new List<string>(values.Length);
            var seen = new HashSet<Guid>();
            var touched = false;

            foreach (var value in values)
            {
                // Clients write these in both the dashed and the plain form, so compare them parsed.
                if (!Guid.TryParse(value, out var parsed))
                {
                    rewritten.Add(value);
                    continue;
                }

                var isStale = stale.Contains(parsed);
                if (isStale)
                {
                    parsed = canonicalId;
                    touched = true;
                }

                // The same view can be listed twice once both of its ids point at the same place.
                if (!seen.Add(parsed))
                {
                    continue;
                }

                rewritten.Add(isStale
                    ? parsed.ToString(value.Contains('-', StringComparison.Ordinal) ? "D" : "N", CultureInfo.InvariantCulture)
                    : value);
            }

            if (!touched)
            {
                continue;
            }

            preference.Value = string.Join(',', rewritten);
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task MoveAncestorsAsync(
        JellyfinDbContext dbContext,
        Guid canonicalId,
        IReadOnlyList<Guid> staleIds,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.AncestorIds
            .WhereOneOrMany(staleIds, e => e.ParentItemId)
            .Select(e => e.ItemId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.AncestorIds
            .WhereOneOrMany(staleIds, e => e.ParentItemId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (items.Count == 0)
        {
            return;
        }

        // The pair is the primary key, so anything already recorded against the canonical view stays put.
        var existing = await dbContext.AncestorIds
            .Where(e => e.ParentItemId.Equals(canonicalId))
            .Select(e => e.ItemId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in items.Except(existing))
        {
            dbContext.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = canonicalId,
                Item = null!,
                ParentItem = null!
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
