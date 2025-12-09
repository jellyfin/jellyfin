#pragma warning disable RS0030 // Do not use banned APIs
// Do not enforce that because EFCore cannot deal with cultures well.
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;
using BaseItemEntity = Jellyfin.Database.Implementations.Entities.BaseItemEntity;

namespace Jellyfin.Server.Implementations.Item;

/*
    All queries in this class and all other nullable enabled EFCore repository classes will make liberal use of the null-forgiving operator "!".
    This is done as the code isn't actually executed client side, but only the expressions are interpret and the compiler cannot know that.
    This is your only warning/message regarding this topic.
*/

/// <summary>
/// Handles all storage logic for BaseItems.
/// </summary>
public sealed class BaseItemRepository
    : IItemRepository
{
    /// <summary>
    /// Gets the placeholder id for UserData detached items.
    /// </summary>
    public static readonly Guid PlaceholderId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// This holds all the types in the running assemblies
    /// so that we can de-serialize properly when we don't have strong types.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type?> _typeMap = new ConcurrentDictionary<string, Type?>();
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _appHost;
    private readonly IItemTypeLookup _itemTypeLookup;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ILogger<BaseItemRepository> _logger;

    private static readonly IReadOnlyList<ItemValueType> _getAllArtistsValueTypes = [ItemValueType.Artist, ItemValueType.AlbumArtist];
    private static readonly IReadOnlyList<ItemValueType> _getArtistValueTypes = [ItemValueType.Artist];
    private static readonly IReadOnlyList<ItemValueType> _getAlbumArtistValueTypes = [ItemValueType.AlbumArtist];
    private static readonly IReadOnlyList<ItemValueType> _getStudiosValueTypes = [ItemValueType.Studios];
    private static readonly IReadOnlyList<ItemValueType> _getGenreValueTypes = [ItemValueType.Genre];
    private static readonly IReadOnlyList<char> SearchWildcardTerms = ['%', '_', '[', ']', '^'];

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItemRepository"/> class.
    /// </summary>
    /// <param name="dbProvider">The db factory.</param>
    /// <param name="appHost">The Application host.</param>
    /// <param name="itemTypeLookup">The static type lookup.</param>
    /// <param name="serverConfigurationManager">The server Configuration manager.</param>
    /// <param name="logger">System logger.</param>
    public BaseItemRepository(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost appHost,
        IItemTypeLookup itemTypeLookup,
        IServerConfigurationManager serverConfigurationManager,
        ILogger<BaseItemRepository> logger)
    {
        _dbProvider = dbProvider;
        _appHost = appHost;
        _itemTypeLookup = itemTypeLookup;
        _serverConfigurationManager = serverConfigurationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public void DeleteItem(params IReadOnlyList<Guid> ids)
    {
        if (ids is null || ids.Count == 0 || ids.Any(f => f.Equals(PlaceholderId)))
        {
            throw new ArgumentException("Guid can't be empty or the placeholder id.", nameof(ids));
        }

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        var date = (DateTime?)DateTime.UtcNow;

        var relatedItems = ids.SelectMany(f => TraverseHirachyDown(f, context)).ToArray();

        // Remove any UserData entries for the placeholder item that would conflict with the UserData
        // being detached from the item being deleted. This is necessary because, during an update,
        // UserData may be reattached to a new entry, but some entries can be left behind.
        // Ensures there are no duplicate UserId/CustomDataKey combinations for the placeholder.
        context.UserData
            .Join(
                context.UserData.WhereOneOrMany(relatedItems, e => e.ItemId),
                placeholder => new { placeholder.UserId, placeholder.CustomDataKey },
                userData => new { userData.UserId, userData.CustomDataKey },
                (placeholder, userData) => placeholder)
            .Where(e => e.ItemId == PlaceholderId)
            .ExecuteDelete();

        // Detach all user watch data
        context.UserData.WhereOneOrMany(relatedItems, e => e.ItemId)
            .ExecuteUpdate(e => e
                .SetProperty(f => f.RetentionDate, date)
                .SetProperty(f => f.ItemId, PlaceholderId));

        context.AncestorIds.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.AncestorIds.WhereOneOrMany(relatedItems, e => e.ParentItemId).ExecuteDelete();
        context.AttachmentStreamInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemImageInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemMetadataFields.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemProviders.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemTrailerTypes.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItems.WhereOneOrMany(relatedItems, e => e.Id).ExecuteDelete();
        context.Chapters.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.CustomItemDisplayPreferences.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.ItemDisplayPreferences.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.ItemValues.Where(e => e.BaseItemsMap!.Count == 0).ExecuteDelete();
        context.ItemValuesMap.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.KeyframeData.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.MediaSegments.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.MediaStreamInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        var query = context.PeopleBaseItemMap.WhereOneOrMany(relatedItems, e => e.ItemId).Select(f => f.PeopleId).Distinct().ToArray();
        context.PeopleBaseItemMap.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.Peoples.WhereOneOrMany(query, e => e.Id).Where(e => e.BaseItems!.Count == 0).ExecuteDelete();
        context.TrickplayInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc />
    public void UpdateInheritedValues()
    {
        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        context.ItemValuesMap.Where(e => e.ItemValue.Type == ItemValueType.InheritedTags).ExecuteDelete();
        // ItemValue Inheritance is now correctly mapped via AncestorId on demand
        context.SaveChanges();

        transaction.Commit();
    }

    /// <inheritdoc />
    public IReadOnlyList<Guid> GetItemIdsList(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        return ApplyQueryFilter(context.BaseItems.AsNoTracking().Where(e => e.Id != EF.Constant(PlaceholderId)), context, filter).Select(e => e.Id).ToArray();
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetAllArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getAllArtistsValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getArtistValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetAlbumArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getAlbumArtistValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetStudios(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getStudiosValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Studio]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetGenres(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getGenreValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetMusicGenres(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getGenreValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicGenre]);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetStudioNames()
    {
        return GetItemValueNames(_getStudiosValueTypes, [], []);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllArtistNames()
    {
        return GetItemValueNames(_getAllArtistsValueTypes, [], []);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetMusicGenreNames()
    {
        return GetItemValueNames(
            _getGenreValueTypes,
            _itemTypeLookup.MusicGenreTypes,
            []);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetGenreNames()
    {
        return GetItemValueNames(
            _getGenreValueTypes,
            [],
            _itemTypeLookup.MusicGenreTypes);
    }

    /// <inheritdoc />
    public QueryResult<BaseItemDto> GetItems(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!filter.EnableTotalRecordCount || ((filter.Limit ?? 0) == 0 && (filter.StartIndex ?? 0) == 0))
        {
            var returnList = GetItemList(filter);
            return new QueryResult<BaseItemDto>(
                filter.StartIndex,
                returnList.Count,
                returnList);
        }

        PrepareFilterQuery(filter);
        var result = new QueryResult<BaseItemDto>();

        using var context = _dbProvider.CreateDbContext();

        IQueryable<BaseItemEntity> dbQuery = PrepareItemQuery(context, filter);

        dbQuery = TranslateQuery(dbQuery, context, filter);
        dbQuery = ApplyGroupingFilter(context, dbQuery, filter);

        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = dbQuery.Count();
        }

        dbQuery = ApplyQueryPaging(dbQuery, filter);
        dbQuery = ApplyNavigations(dbQuery, filter);

        result.Items = dbQuery.AsEnumerable().Where(e => e is not null).Select(w => DeserializeBaseItem(w, filter.SkipDeserialization)).ToArray();
        result.StartIndex = filter.StartIndex ?? 0;
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<BaseItemDto> GetItemList(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        IQueryable<BaseItemEntity> dbQuery = PrepareItemQuery(context, filter);

        dbQuery = TranslateQuery(dbQuery, context, filter);

        dbQuery = ApplyGroupingFilter(context, dbQuery, filter);
        dbQuery = ApplyQueryPaging(dbQuery, filter);
        dbQuery = ApplyNavigations(dbQuery, filter);

        return dbQuery.AsEnumerable().Where(e => e is not null).Select(w => DeserializeBaseItem(w, filter.SkipDeserialization)).ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<BaseItem> GetLatestItemList(InternalItemsQuery filter, CollectionType collectionType)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        // Early exit if collection type is not tvshows or music
        if (collectionType != CollectionType.tvshows && collectionType != CollectionType.music)
        {
            return Array.Empty<BaseItem>();
        }

        using var context = _dbProvider.CreateDbContext();

        // Subquery to group by SeriesNames/Album and get the max Date Created for each group.
        var subquery = PrepareItemQuery(context, filter);
        subquery = TranslateQuery(subquery, context, filter);
        var subqueryGrouped = subquery.GroupBy(g => collectionType == CollectionType.tvshows ? g.SeriesName : g.Album)
            .Select(g => new
            {
                Key = g.Key,
                MaxDateCreated = g.Max(a => a.DateCreated)
            })
            .OrderByDescending(g => g.MaxDateCreated)
            .Select(g => g);

        if (filter.Limit.HasValue && filter.Limit.Value > 0)
        {
            subqueryGrouped = subqueryGrouped.Take(filter.Limit.Value);
        }

        filter.Limit = null;

        var mainquery = PrepareItemQuery(context, filter);
        mainquery = TranslateQuery(mainquery, context, filter);
        mainquery = mainquery.Where(g => g.DateCreated >= subqueryGrouped.Min(s => s.MaxDateCreated));
        mainquery = ApplyGroupingFilter(context, mainquery, filter);
        mainquery = ApplyQueryPaging(mainquery, filter);

        mainquery = ApplyNavigations(mainquery, filter);

        return mainquery.AsEnumerable().Where(e => e is not null).Select(w => DeserializeBaseItem(w, filter.SkipDeserialization)).ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetNextUpSeriesKeys(InternalItemsQuery filter, DateTime dateCutoff)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);

        using var context = _dbProvider.CreateDbContext();

        var query = context.BaseItems
            .AsNoTracking()
            .Where(i => filter.TopParentIds.Contains(i.TopParentId!.Value))
            .Where(i => i.Type == _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode])
            .Join(
                context.UserData.AsNoTracking().Where(e => e.ItemId != EF.Constant(PlaceholderId)),
                i => new { UserId = filter.User.Id, ItemId = i.Id },
                u => new { UserId = u.UserId, ItemId = u.ItemId },
                (entity, data) => new { Item = entity, UserData = data })
            .GroupBy(g => g.Item.SeriesPresentationUniqueKey)
            .Select(g => new { g.Key, LastPlayedDate = g.Max(u => u.UserData.LastPlayedDate) })
            .Where(g => g.Key != null && g.LastPlayedDate != null && g.LastPlayedDate >= dateCutoff)
            .OrderByDescending(g => g.LastPlayedDate)
            .Select(g => g.Key!);

        if (filter.Limit.HasValue && filter.Limit.Value > 0)
        {
            query = query.Take(filter.Limit.Value);
        }

        return query.ToArray();
    }

    private IQueryable<BaseItemEntity> ApplyGroupingFilter(JellyfinDbContext context, IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        // This whole block is needed to filter duplicate entries on request
        // for the time being it cannot be used because it would destroy the ordering
        // this results in "duplicate" responses for queries that try to lookup individual series or multiple versions but
        // for that case the invoker has to run a DistinctBy(e => e.PresentationUniqueKey) on their own

        var enableGroupByPresentationUniqueKey = EnableGroupByPresentationUniqueKey(filter);
        if (enableGroupByPresentationUniqueKey && filter.GroupBySeriesPresentationUniqueKey)
        {
            var tempQuery = dbQuery.GroupBy(e => new { e.PresentationUniqueKey, e.SeriesPresentationUniqueKey }).Select(e => e.FirstOrDefault()).Select(e => e!.Id);
            dbQuery = context.BaseItems.Where(e => tempQuery.Contains(e.Id));
        }
        else if (enableGroupByPresentationUniqueKey)
        {
            var tempQuery = dbQuery.GroupBy(e => e.PresentationUniqueKey).Select(e => e.FirstOrDefault()).Select(e => e!.Id);
            dbQuery = context.BaseItems.Where(e => tempQuery.Contains(e.Id));
        }
        else if (filter.GroupBySeriesPresentationUniqueKey)
        {
            var tempQuery = dbQuery.GroupBy(e => e.SeriesPresentationUniqueKey).Select(e => e.FirstOrDefault()).Select(e => e!.Id);
            dbQuery = context.BaseItems.Where(e => tempQuery.Contains(e.Id));
        }
        else
        {
            dbQuery = dbQuery.Distinct();
        }

        dbQuery = ApplyOrder(dbQuery, filter, context);

        return dbQuery;
    }

    private static IQueryable<BaseItemEntity> ApplyNavigations(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        dbQuery = dbQuery.Include(e => e.TrailerTypes)
           .Include(e => e.Provider)
           .Include(e => e.LockedFields)
           .Include(e => e.UserData);

        if (filter.DtoOptions.EnableImages)
        {
            dbQuery = dbQuery.Include(e => e.Images);
        }

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyQueryPaging(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        if (filter.StartIndex.HasValue && filter.StartIndex.Value > 0)
        {
            dbQuery = dbQuery.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit.HasValue && filter.Limit.Value > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit.Value);
        }

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyQueryFilter(IQueryable<BaseItemEntity> dbQuery, JellyfinDbContext context, InternalItemsQuery filter)
    {
        dbQuery = TranslateQuery(dbQuery, context, filter);
        dbQuery = ApplyGroupingFilter(context, dbQuery, filter);
        dbQuery = ApplyQueryPaging(dbQuery, filter);
        dbQuery = ApplyNavigations(dbQuery, filter);
        return dbQuery;
    }

    private IQueryable<BaseItemEntity> PrepareItemQuery(JellyfinDbContext context, InternalItemsQuery filter)
    {
        IQueryable<BaseItemEntity> dbQuery = context.BaseItems.AsNoTracking();
        dbQuery = dbQuery.AsSingleQuery();

        return dbQuery;
    }

    /// <inheritdoc/>
    public int GetCount(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        // Hack for right now since we currently don't support filtering out these duplicates within a query
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems.AsNoTracking(), context, filter);

        return dbQuery.Count();
    }

    /// <inheritdoc />
    public ItemCounts GetItemCounts(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        // Hack for right now since we currently don't support filtering out these duplicates within a query
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems.AsNoTracking(), context, filter);

        var counts = dbQuery
            .GroupBy(x => x.Type)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToArray();

        var lookup = _itemTypeLookup.BaseItemKindNames;
        var result = new ItemCounts();
        foreach (var count in counts)
        {
            if (string.Equals(count.Key, lookup[BaseItemKind.MusicAlbum], StringComparison.Ordinal))
            {
                result.AlbumCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.MusicArtist], StringComparison.Ordinal))
            {
                result.ArtistCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Episode], StringComparison.Ordinal))
            {
                result.EpisodeCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Movie], StringComparison.Ordinal))
            {
                result.MovieCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.MusicVideo], StringComparison.Ordinal))
            {
                result.MusicVideoCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.LiveTvProgram], StringComparison.Ordinal))
            {
                result.ProgramCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Series], StringComparison.Ordinal))
            {
                result.SeriesCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Audio], StringComparison.Ordinal))
            {
                result.SongCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Trailer], StringComparison.Ordinal))
            {
                result.TrailerCount = count.Count;
            }
        }

        return result;
    }

#pragma warning disable CA1307 // Specify StringComparison for clarity
    /// <summary>
    /// Gets the type.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <returns>Type.</returns>
    /// <exception cref="ArgumentNullException"><c>typeName</c> is null.</exception>
    private static Type? GetType(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        // TODO: this isn't great. Refactor later to be both globally handled by a dedicated service not just an static variable and be loaded eagerly.
        // currently this is done so that plugins may introduce their own type of baseitems as we dont know when we are first called, before or after plugins are loaded
        return _typeMap.GetOrAdd(typeName, k => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(k))
            .FirstOrDefault(t => t is not null));
    }

    /// <inheritdoc  />
    public void SaveImages(BaseItemDto item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var images = item.ImageInfos.Select(e => Map(item.Id, e));
        using var context = _dbProvider.CreateDbContext();

        if (!context.BaseItems.Any(bi => bi.Id == item.Id))
        {
            _logger.LogWarning("Unable to save ImageInfo for non existing BaseItem");
            return;
        }

        context.BaseItemImageInfos.Where(e => e.ItemId == item.Id).ExecuteDelete();
        context.BaseItemImageInfos.AddRange(images);
        context.SaveChanges();
    }

    /// <inheritdoc  />
    public void SaveItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        UpdateOrInsertItems(items, cancellationToken);
    }

    /// <inheritdoc cref="IItemRepository"/>
    public void UpdateOrInsertItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var tuples = new List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)>();
        foreach (var item in items.GroupBy(e => e.Id).Select(e => e.Last()).Where(e => e.Id != PlaceholderId))
        {
            var ancestorIds = item.SupportsAncestors ?
                item.GetAncestorIds().Distinct().ToList() :
                null;

            var topParent = item.GetTopParent();

            var userdataKey = item.GetUserDataKeys();
            var inheritedTags = item.GetInheritedTags();

            tuples.Add((item, ancestorIds, topParent, userdataKey, inheritedTags));
        }

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        var ids = tuples.Select(f => f.Item.Id).ToArray();
        var existingItems = context.BaseItems.Where(e => ids.Contains(e.Id)).Select(f => f.Id).ToArray();
        var newItems = tuples.Where(e => !existingItems.Contains(e.Item.Id)).ToArray();

        foreach (var item in tuples)
        {
            var entity = Map(item.Item);
            // TODO: refactor this "inconsistency"
            entity.TopParentId = item.TopParent?.Id;

            if (!existingItems.Any(e => e == entity.Id))
            {
                context.BaseItems.Add(entity);
            }
            else
            {
                context.BaseItemProviders.Where(e => e.ItemId == entity.Id).ExecuteDelete();
                context.BaseItemImageInfos.Where(e => e.ItemId == entity.Id).ExecuteDelete();
                context.BaseItemMetadataFields.Where(e => e.ItemId == entity.Id).ExecuteDelete();

                if (entity.Images is { Count: > 0 })
                {
                    context.BaseItemImageInfos.AddRange(entity.Images);
                }

                if (entity.LockedFields is { Count: > 0 })
                {
                    context.BaseItemMetadataFields.AddRange(entity.LockedFields);
                }

                context.BaseItems.Attach(entity).State = EntityState.Modified;
            }
        }

        context.SaveChanges();

        foreach (var item in newItems)
        {
            // reattach old userData entries
            var userKeys = item.UserDataKey.ToArray();
            var retentionDate = (DateTime?)null;
            context.UserData
                .Where(e => e.ItemId == PlaceholderId)
                .Where(e => userKeys.Contains(e.CustomDataKey))
                .ExecuteUpdate(e => e
                    .SetProperty(f => f.ItemId, item.Item.Id)
                    .SetProperty(f => f.RetentionDate, retentionDate));
        }

        var itemValueMaps = tuples
            .Select(e => (e.Item, Values: GetItemValuesToSave(e.Item, e.InheritedTags)))
            .ToArray();
        var allListedItemValues = itemValueMaps
            .SelectMany(f => f.Values)
            .Distinct()
            .ToArray();
        var existingValues = context.ItemValues
            .Select(e => new
            {
                item = e,
                Key = e.Type + "+" + e.Value
            })
            .Where(f => allListedItemValues.Select(e => $"{(int)e.MagicNumber}+{e.Value}").Contains(f.Key))
            .Select(e => e.item)
            .ToArray();
        var missingItemValues = allListedItemValues.Except(existingValues.Select(f => (MagicNumber: f.Type, f.Value))).Select(f => new ItemValue()
        {
            CleanValue = GetCleanValue(f.Value),
            ItemValueId = Guid.NewGuid(),
            Type = f.MagicNumber,
            Value = f.Value
        }).ToArray();
        context.ItemValues.AddRange(missingItemValues);
        context.SaveChanges();

        var itemValuesStore = existingValues.Concat(missingItemValues).ToArray();
        var valueMap = itemValueMaps
            .Select(f => (f.Item, Values: f.Values.Select(e => itemValuesStore.First(g => g.Value == e.Value && g.Type == e.MagicNumber)).DistinctBy(e => e.ItemValueId).ToArray()))
            .ToArray();

        var mappedValues = context.ItemValuesMap.Where(e => ids.Contains(e.ItemId)).ToList();

        foreach (var item in valueMap)
        {
            var itemMappedValues = mappedValues.Where(e => e.ItemId == item.Item.Id).ToList();
            foreach (var itemValue in item.Values)
            {
                var existingItem = itemMappedValues.FirstOrDefault(f => f.ItemValueId == itemValue.ItemValueId);
                if (existingItem is null)
                {
                    context.ItemValuesMap.Add(new ItemValueMap()
                    {
                        Item = null!,
                        ItemId = item.Item.Id,
                        ItemValue = null!,
                        ItemValueId = itemValue.ItemValueId
                    });
                }
                else
                {
                    // map exists, remove from list so its been handled.
                    itemMappedValues.Remove(existingItem);
                }
            }

            // all still listed values are not in the new list so remove them.
            context.ItemValuesMap.RemoveRange(itemMappedValues);
        }

        context.SaveChanges();

        foreach (var item in tuples)
        {
            if (item.Item.SupportsAncestors && item.AncestorIds != null)
            {
                var existingAncestorIds = context.AncestorIds.Where(e => e.ItemId == item.Item.Id).ToList();
                var validAncestorIds = context.BaseItems.Where(e => item.AncestorIds.Contains(e.Id)).Select(f => f.Id).ToArray();
                foreach (var ancestorId in validAncestorIds)
                {
                    var existingAncestorId = existingAncestorIds.FirstOrDefault(e => e.ParentItemId == ancestorId);
                    if (existingAncestorId is null)
                    {
                        context.AncestorIds.Add(new AncestorId()
                        {
                            ParentItemId = ancestorId,
                            ItemId = item.Item.Id,
                            Item = null!,
                            ParentItem = null!
                        });
                    }
                    else
                    {
                        existingAncestorIds.Remove(existingAncestorId);
                    }
                }

                context.AncestorIds.RemoveRange(existingAncestorIds);
            }
        }

        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc  />
    public BaseItemDto? RetrieveItem(Guid id)
    {
        if (id.IsEmpty())
        {
            throw new ArgumentException("Guid can't be empty", nameof(id));
        }

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = PrepareItemQuery(context, new()
        {
            DtoOptions = new()
            {
                EnableImages = true
            }
        });
        dbQuery = dbQuery.Include(e => e.TrailerTypes)
            .Include(e => e.Provider)
            .Include(e => e.LockedFields)
            .Include(e => e.UserData)
            .Include(e => e.Images);

        var item = dbQuery.FirstOrDefault(e => e.Id == id);
        if (item is null)
        {
            return null;
        }

        return DeserializeBaseItem(item);
    }

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="dto">The dto base instance.</param>
    /// <param name="appHost">The Application server Host.</param>
    /// <param name="logger">The applogger.</param>
    /// <returns>The dto to map.</returns>
    public static BaseItemDto Map(BaseItemEntity entity, BaseItemDto dto, IServerApplicationHost? appHost, ILogger logger)
    {
        dto.Id = entity.Id;
        dto.ParentId = entity.ParentId.GetValueOrDefault();
        dto.Path = appHost?.ExpandVirtualPath(entity.Path) ?? entity.Path;
        dto.EndDate = entity.EndDate;
        dto.CommunityRating = entity.CommunityRating;
        dto.CustomRating = entity.CustomRating;
        dto.IndexNumber = entity.IndexNumber;
        dto.IsLocked = entity.IsLocked;
        dto.Name = entity.Name;
        dto.OfficialRating = entity.OfficialRating;
        dto.Overview = entity.Overview;
        dto.ParentIndexNumber = entity.ParentIndexNumber;
        dto.PremiereDate = entity.PremiereDate;
        dto.ProductionYear = entity.ProductionYear;
        dto.SortName = entity.SortName;
        dto.ForcedSortName = entity.ForcedSortName;
        dto.RunTimeTicks = entity.RunTimeTicks;
        dto.PreferredMetadataLanguage = entity.PreferredMetadataLanguage;
        dto.PreferredMetadataCountryCode = entity.PreferredMetadataCountryCode;
        dto.IsInMixedFolder = entity.IsInMixedFolder;
        dto.InheritedParentalRatingValue = entity.InheritedParentalRatingValue;
        dto.InheritedParentalRatingSubValue = entity.InheritedParentalRatingSubValue;
        dto.CriticRating = entity.CriticRating;
        dto.PresentationUniqueKey = entity.PresentationUniqueKey;
        dto.OriginalTitle = entity.OriginalTitle;
        dto.Album = entity.Album;
        dto.LUFS = entity.LUFS;
        dto.NormalizationGain = entity.NormalizationGain;
        dto.IsVirtualItem = entity.IsVirtualItem;
        dto.ExternalSeriesId = entity.ExternalSeriesId;
        dto.Tagline = entity.Tagline;
        dto.TotalBitrate = entity.TotalBitrate;
        dto.ExternalId = entity.ExternalId;
        dto.Size = entity.Size;
        dto.Genres = string.IsNullOrWhiteSpace(entity.Genres) ? [] : entity.Genres.Split('|');
        dto.DateCreated = entity.DateCreated ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.DateModified = entity.DateModified ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.ChannelId = entity.ChannelId ?? Guid.Empty;
        dto.DateLastRefreshed = entity.DateLastRefreshed ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.DateLastSaved = entity.DateLastSaved ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.OwnerId = string.IsNullOrWhiteSpace(entity.OwnerId) ? Guid.Empty : (Guid.TryParse(entity.OwnerId, out var ownerId) ? ownerId : Guid.Empty);
        dto.Width = entity.Width.GetValueOrDefault();
        dto.Height = entity.Height.GetValueOrDefault();
        dto.UserData = entity.UserData;

        if (entity.Provider is not null)
        {
            dto.ProviderIds = entity.Provider.ToDictionary(e => e.ProviderId, e => e.ProviderValue);
        }

        if (entity.ExtraType is not null)
        {
            dto.ExtraType = (ExtraType)entity.ExtraType;
        }

        if (entity.LockedFields is not null)
        {
            dto.LockedFields = entity.LockedFields?.Select(e => (MetadataField)e.Id).ToArray() ?? [];
        }

        if (entity.Audio is not null)
        {
            dto.Audio = (ProgramAudio)entity.Audio;
        }

        dto.ExtraIds = string.IsNullOrWhiteSpace(entity.ExtraIds) ? [] : entity.ExtraIds.Split('|').Select(e => Guid.Parse(e)).ToArray();
        dto.ProductionLocations = entity.ProductionLocations?.Split('|') ?? [];
        dto.Studios = entity.Studios?.Split('|') ?? [];
        dto.Tags = string.IsNullOrWhiteSpace(entity.Tags) ? [] : entity.Tags.Split('|');

        if (dto is IHasProgramAttributes hasProgramAttributes)
        {
            hasProgramAttributes.IsMovie = entity.IsMovie;
            hasProgramAttributes.IsSeries = entity.IsSeries;
            hasProgramAttributes.EpisodeTitle = entity.EpisodeTitle;
            hasProgramAttributes.IsRepeat = entity.IsRepeat;
        }

        if (dto is LiveTvChannel liveTvChannel)
        {
            liveTvChannel.ServiceName = entity.ExternalServiceId;
        }

        if (dto is Trailer trailer)
        {
            trailer.TrailerTypes = entity.TrailerTypes?.Select(e => (TrailerType)e.Id).ToArray() ?? [];
        }

        if (dto is Video video)
        {
            video.PrimaryVersionId = entity.PrimaryVersionId;
        }

        if (dto is IHasSeries hasSeriesName)
        {
            hasSeriesName.SeriesName = entity.SeriesName;
            hasSeriesName.SeriesId = entity.SeriesId.GetValueOrDefault();
            hasSeriesName.SeriesPresentationUniqueKey = entity.SeriesPresentationUniqueKey;
        }

        if (dto is Episode episode)
        {
            episode.SeasonName = entity.SeasonName;
            episode.SeasonId = entity.SeasonId.GetValueOrDefault();
        }

        if (dto is IHasArtist hasArtists)
        {
            hasArtists.Artists = entity.Artists?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            hasAlbumArtists.AlbumArtists = entity.AlbumArtists?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        if (dto is LiveTvProgram program)
        {
            program.ShowId = entity.ShowId;
        }

        if (entity.Images is not null)
        {
            dto.ImageInfos = entity.Images.Select(e => Map(e, appHost)).ToArray();
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        // dto.MediaType = Enum.TryParse<MediaType>(entity.MediaType);
        if (dto is IHasStartDate hasStartDate)
        {
            hasStartDate.StartDate = entity.StartDate.GetValueOrDefault();
        }

        // Fields that are present in the DB but are never actually used
        // dto.UnratedType = entity.UnratedType;
        // dto.TopParentId = entity.TopParentId;
        // dto.CleanName = entity.CleanName;
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            folder.DateLastMediaAdded = entity.DateLastMediaAdded ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        return dto;
    }

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="dto">The entity.</param>
    /// <returns>The dto to map.</returns>
    public BaseItemEntity Map(BaseItemDto dto)
    {
        var dtoType = dto.GetType();
        var entity = new BaseItemEntity()
        {
            Type = dtoType.ToString(),
            Id = dto.Id
        };

        if (TypeRequiresDeserialization(dtoType))
        {
            entity.Data = JsonSerializer.Serialize(dto, dtoType, JsonDefaults.Options);
        }

        entity.ParentId = !dto.ParentId.IsEmpty() ? dto.ParentId : null;
        entity.Path = GetPathToSave(dto.Path);
        entity.EndDate = dto.EndDate;
        entity.CommunityRating = dto.CommunityRating;
        entity.CustomRating = dto.CustomRating;
        entity.IndexNumber = dto.IndexNumber;
        entity.IsLocked = dto.IsLocked;
        entity.Name = dto.Name;
        entity.CleanName = GetCleanValue(dto.Name);
        entity.OfficialRating = dto.OfficialRating;
        entity.Overview = dto.Overview;
        entity.ParentIndexNumber = dto.ParentIndexNumber;
        entity.PremiereDate = dto.PremiereDate;
        entity.ProductionYear = dto.ProductionYear;
        entity.SortName = dto.SortName;
        entity.ForcedSortName = dto.ForcedSortName;
        entity.RunTimeTicks = dto.RunTimeTicks;
        entity.PreferredMetadataLanguage = dto.PreferredMetadataLanguage;
        entity.PreferredMetadataCountryCode = dto.PreferredMetadataCountryCode;
        entity.IsInMixedFolder = dto.IsInMixedFolder;
        entity.InheritedParentalRatingValue = dto.InheritedParentalRatingValue;
        entity.InheritedParentalRatingSubValue = dto.InheritedParentalRatingSubValue;
        entity.CriticRating = dto.CriticRating;
        entity.PresentationUniqueKey = dto.PresentationUniqueKey;
        entity.OriginalTitle = dto.OriginalTitle;
        entity.Album = dto.Album;
        entity.LUFS = dto.LUFS;
        entity.NormalizationGain = dto.NormalizationGain;
        entity.IsVirtualItem = dto.IsVirtualItem;
        entity.ExternalSeriesId = dto.ExternalSeriesId;
        entity.Tagline = dto.Tagline;
        entity.TotalBitrate = dto.TotalBitrate;
        entity.ExternalId = dto.ExternalId;
        entity.Size = dto.Size;
        entity.Genres = string.Join('|', dto.Genres);
        entity.DateCreated = dto.DateCreated == DateTime.MinValue ? null : dto.DateCreated;
        entity.DateModified = dto.DateModified == DateTime.MinValue ? null : dto.DateModified;
        entity.ChannelId = dto.ChannelId;
        entity.DateLastRefreshed = dto.DateLastRefreshed == DateTime.MinValue ? null : dto.DateLastRefreshed;
        entity.DateLastSaved = dto.DateLastSaved == DateTime.MinValue ? null : dto.DateLastSaved;
        entity.OwnerId = dto.OwnerId.ToString();
        entity.Width = dto.Width;
        entity.Height = dto.Height;
        entity.Provider = dto.ProviderIds.Select(e => new BaseItemProvider()
        {
            Item = entity,
            ProviderId = e.Key,
            ProviderValue = e.Value
        }).ToList();

        if (dto.Audio.HasValue)
        {
            entity.Audio = (ProgramAudioEntity)dto.Audio;
        }

        if (dto.ExtraType.HasValue)
        {
            entity.ExtraType = (BaseItemExtraType)dto.ExtraType;
        }

        entity.ExtraIds = dto.ExtraIds is not null ? string.Join('|', dto.ExtraIds) : null;
        entity.ProductionLocations = dto.ProductionLocations is not null ? string.Join('|', dto.ProductionLocations) : null;
        entity.Studios = dto.Studios is not null ? string.Join('|', dto.Studios) : null;
        entity.Tags = dto.Tags is not null ? string.Join('|', dto.Tags) : null;
        entity.LockedFields = dto.LockedFields is not null ? dto.LockedFields
            .Select(e => new BaseItemMetadataField()
            {
                Id = (int)e,
                Item = entity,
                ItemId = entity.Id
            })
            .ToArray() : null;

        if (dto is IHasProgramAttributes hasProgramAttributes)
        {
            entity.IsMovie = hasProgramAttributes.IsMovie;
            entity.IsSeries = hasProgramAttributes.IsSeries;
            entity.EpisodeTitle = hasProgramAttributes.EpisodeTitle;
            entity.IsRepeat = hasProgramAttributes.IsRepeat;
        }

        if (dto is LiveTvChannel liveTvChannel)
        {
            entity.ExternalServiceId = liveTvChannel.ServiceName;
        }

        if (dto is Video video)
        {
            entity.PrimaryVersionId = video.PrimaryVersionId;
        }

        if (dto is IHasSeries hasSeriesName)
        {
            entity.SeriesName = hasSeriesName.SeriesName;
            entity.SeriesId = hasSeriesName.SeriesId;
            entity.SeriesPresentationUniqueKey = hasSeriesName.SeriesPresentationUniqueKey;
        }

        if (dto is Episode episode)
        {
            entity.SeasonName = episode.SeasonName;
            entity.SeasonId = episode.SeasonId;
        }

        if (dto is IHasArtist hasArtists)
        {
            entity.Artists = hasArtists.Artists is not null ? string.Join('|', hasArtists.Artists) : null;
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            entity.AlbumArtists = hasAlbumArtists.AlbumArtists is not null ? string.Join('|', hasAlbumArtists.AlbumArtists) : null;
        }

        if (dto is LiveTvProgram program)
        {
            entity.ShowId = program.ShowId;
        }

        if (dto.ImageInfos is not null)
        {
            entity.Images = dto.ImageInfos.Select(f => Map(dto.Id, f)).ToArray();
        }

        if (dto is Trailer trailer)
        {
            entity.TrailerTypes = trailer.TrailerTypes?.Select(e => new BaseItemTrailerType()
            {
                Id = (int)e,
                Item = entity,
                ItemId = entity.Id
            }).ToArray() ?? [];
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        entity.MediaType = dto.MediaType.ToString();
        if (dto is IHasStartDate hasStartDate)
        {
            entity.StartDate = hasStartDate.StartDate;
        }

        entity.UnratedType = dto.GetBlockUnratedType().ToString();

        // Fields that are present in the DB but are never actually used
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            entity.DateLastMediaAdded = folder.DateLastMediaAdded == DateTime.MinValue ? null : folder.DateLastMediaAdded;
            entity.IsFolder = folder.IsFolder;
        }

        return entity;
    }

    private string[] GetItemValueNames(IReadOnlyList<ItemValueType> itemValueTypes, IReadOnlyList<string> withItemTypes, IReadOnlyList<string> excludeItemTypes)
    {
        using var context = _dbProvider.CreateDbContext();

        var query = context.ItemValuesMap
            .AsNoTracking()
            .Where(e => itemValueTypes.Any(w => (ItemValueType)w == e.ItemValue.Type));
        if (withItemTypes.Count > 0)
        {
            query = query.Where(e => withItemTypes.Contains(e.Item.Type));
        }

        if (excludeItemTypes.Count > 0)
        {
            query = query.Where(e => !excludeItemTypes.Contains(e.Item.Type));
        }

        // query = query.DistinctBy(e => e.CleanValue);
        return query.Select(e => e.ItemValue)
            .GroupBy(e => e.CleanValue)
            .Select(e => e.First().Value)
            .ToArray();
    }

    private static bool TypeRequiresDeserialization(Type type)
    {
        return type.GetCustomAttribute<RequiresSourceSerialisationAttribute>() == null;
    }

    private BaseItemDto DeserializeBaseItem(BaseItemEntity baseItemEntity, bool skipDeserialization = false)
    {
        ArgumentNullException.ThrowIfNull(baseItemEntity, nameof(baseItemEntity));
        if (_serverConfigurationManager?.Configuration is null)
        {
            throw new InvalidOperationException("Server Configuration manager or configuration is null");
        }

        var typeToSerialise = GetType(baseItemEntity.Type);
        return BaseItemRepository.DeserializeBaseItem(
            baseItemEntity,
            _logger,
            _appHost,
            skipDeserialization || (_serverConfigurationManager.Configuration.SkipDeserializationForBasicTypes && (typeToSerialise == typeof(Channel) || typeToSerialise == typeof(UserRootFolder))));
    }

    /// <summary>
    /// Deserializes a BaseItemEntity and sets all properties.
    /// </summary>
    /// <param name="baseItemEntity">The DB entity.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="appHost">The application server Host.</param>
    /// <param name="skipDeserialization">If only mapping should be processed.</param>
    /// <returns>A mapped BaseItem.</returns>
    /// <exception cref="InvalidOperationException">Will be thrown if an invalid serialisation is requested.</exception>
    public static BaseItemDto DeserializeBaseItem(BaseItemEntity baseItemEntity, ILogger logger, IServerApplicationHost? appHost, bool skipDeserialization = false)
    {
        var type = GetType(baseItemEntity.Type) ?? throw new InvalidOperationException("Cannot deserialize unknown type.");
        BaseItemDto? dto = null;
        if (TypeRequiresDeserialization(type) && baseItemEntity.Data is not null && !skipDeserialization)
        {
            try
            {
                dto = JsonSerializer.Deserialize(baseItemEntity.Data, type, JsonDefaults.Options) as BaseItemDto;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Error deserializing item with JSON: {Data}", baseItemEntity.Data);
            }
        }

        if (dto is null)
        {
            dto = Activator.CreateInstance(type) as BaseItemDto ?? throw new InvalidOperationException("Cannot deserialize unknown type.");
        }

        return Map(baseItemEntity, dto, appHost, logger);
    }

    private QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetItemValues(InternalItemsQuery filter, IReadOnlyList<ItemValueType> itemValueTypes, string returnType)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (!(filter.Limit.HasValue && filter.Limit.Value > 0))
        {
            filter.EnableTotalRecordCount = false;
        }

        using var context = _dbProvider.CreateDbContext();

        var innerQueryFilter = TranslateQuery(context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)), context, new InternalItemsQuery(filter.User)
        {
            ExcludeItemTypes = filter.ExcludeItemTypes,
            IncludeItemTypes = filter.IncludeItemTypes,
            MediaTypes = filter.MediaTypes,
            AncestorIds = filter.AncestorIds,
            ItemIds = filter.ItemIds,
            TopParentIds = filter.TopParentIds,
            ParentId = filter.ParentId,
            IsAiring = filter.IsAiring,
            IsMovie = filter.IsMovie,
            IsSports = filter.IsSports,
            IsKids = filter.IsKids,
            IsNews = filter.IsNews,
            IsSeries = filter.IsSeries
        });

        var itemValuesQuery = context.ItemValues
            .Where(f => itemValueTypes.Contains(f.Type))
            .SelectMany(f => f.BaseItemsMap!, (f, w) => new { f, w })
            .Join(
                innerQueryFilter,
                fw => fw.w.ItemId,
                g => g.Id,
                (fw, g) => fw.f.CleanValue);

        var innerQuery = PrepareItemQuery(context, filter)
            .Where(e => e.Type == returnType)
            .Where(e => itemValuesQuery.Contains(e.CleanName));

        var outerQueryFilter = new InternalItemsQuery(filter.User)
        {
            IsPlayed = filter.IsPlayed,
            IsFavorite = filter.IsFavorite,
            IsFavoriteOrLiked = filter.IsFavoriteOrLiked,
            IsLiked = filter.IsLiked,
            IsLocked = filter.IsLocked,
            NameLessThan = filter.NameLessThan,
            NameStartsWith = filter.NameStartsWith,
            NameStartsWithOrGreater = filter.NameStartsWithOrGreater,
            Tags = filter.Tags,
            OfficialRatings = filter.OfficialRatings,
            StudioIds = filter.StudioIds,
            GenreIds = filter.GenreIds,
            Genres = filter.Genres,
            Years = filter.Years,
            NameContains = filter.NameContains,
            SearchTerm = filter.SearchTerm,
            ExcludeItemIds = filter.ExcludeItemIds
        };

        var masterQuery = TranslateQuery(innerQuery, context, outerQueryFilter)
            .GroupBy(e => e.PresentationUniqueKey)
            .Select(e => e.FirstOrDefault())
            .Select(e => e!.Id);

        var query = context.BaseItems
            .Include(e => e.TrailerTypes)
            .Include(e => e.Provider)
            .Include(e => e.LockedFields)
            .Include(e => e.Images)
            .AsSingleQuery()
            .Where(e => masterQuery.Contains(e.Id));

        query = ApplyOrder(query, filter, context);

        var result = new QueryResult<(BaseItemDto, ItemCounts?)>();
        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = query.Count();
        }

        if (filter.StartIndex.HasValue && filter.StartIndex.Value > 0)
        {
            query = query.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit.HasValue && filter.Limit.Value > 0)
        {
            query = query.Take(filter.Limit.Value);
        }

        IQueryable<BaseItemEntity>? itemCountQuery = null;

        if (filter.IncludeItemTypes.Length > 0)
        {
            // if we are to include more then one type, sub query those items beforehand.

            var typeSubQuery = new InternalItemsQuery(filter.User)
            {
                ExcludeItemTypes = filter.ExcludeItemTypes,
                IncludeItemTypes = filter.IncludeItemTypes,
                MediaTypes = filter.MediaTypes,
                AncestorIds = filter.AncestorIds,
                ExcludeItemIds = filter.ExcludeItemIds,
                ItemIds = filter.ItemIds,
                TopParentIds = filter.TopParentIds,
                ParentId = filter.ParentId,
                IsPlayed = filter.IsPlayed
            };

            itemCountQuery = TranslateQuery(context.BaseItems.AsNoTracking().Where(e => e.Id != EF.Constant(PlaceholderId)), context, typeSubQuery)
                .Where(e => e.ItemValues!.Any(f => itemValueTypes!.Contains(f.ItemValue.Type)));

            var seriesTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
            var movieTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
            var episodeTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];
            var musicAlbumTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicAlbum];
            var musicArtistTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist];
            var audioTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio];
            var trailerTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Trailer];

            var resultQuery = query.Select(e => new
            {
                item = e,
                // TODO: This is bad refactor!
                itemCount = new ItemCounts()
                {
                    SeriesCount = itemCountQuery!.Count(f => f.Type == seriesTypeName),
                    EpisodeCount = itemCountQuery!.Count(f => f.Type == episodeTypeName),
                    MovieCount = itemCountQuery!.Count(f => f.Type == movieTypeName),
                    AlbumCount = itemCountQuery!.Count(f => f.Type == musicAlbumTypeName),
                    ArtistCount = itemCountQuery!.Count(f => f.Type == musicArtistTypeName),
                    SongCount = itemCountQuery!.Count(f => f.Type == audioTypeName),
                    TrailerCount = itemCountQuery!.Count(f => f.Type == trailerTypeName),
                }
            });

            result.StartIndex = filter.StartIndex ?? 0;
            result.Items =
            [
                .. resultQuery
                    .AsEnumerable()
                    .Where(e => e is not null)
                    .Select(e =>
                    {
                        return (DeserializeBaseItem(e.item, filter.SkipDeserialization), e.itemCount);
                    })
            ];
        }
        else
        {
            result.StartIndex = filter.StartIndex ?? 0;
            result.Items =
            [
                .. query
                    .AsEnumerable()
                    .Where(e => e is not null)
                    .Select<BaseItemEntity, (BaseItemDto, ItemCounts?)>(e =>
                    {
                        return (DeserializeBaseItem(e, filter.SkipDeserialization), null);
                    })
            ];
        }

        return result;
    }

    private static void PrepareFilterQuery(InternalItemsQuery query)
    {
        if (query.Limit.HasValue && query.Limit.Value > 0 && query.EnableGroupByMetadataKey)
        {
            query.Limit = query.Limit.Value + 4;
        }

        if (query.IsResumable ?? false)
        {
            query.IsVirtualItem = false;
        }
    }

    /// <summary>
    /// Gets the clean value for search and sorting purposes.
    /// </summary>
    /// <param name="value">The value to clean.</param>
    /// <returns>The cleaned value.</returns>
    public static string GetCleanValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var noDiacritics = value.RemoveDiacritics();

        // Build a string where any punctuation or symbol is treated as a separator (space).
        var sb = new StringBuilder(noDiacritics.Length);
        var previousWasSpace = false;
        foreach (var ch in noDiacritics)
        {
            char outCh;
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            {
                outCh = ch;
            }
            else
            {
                outCh = ' ';
            }

            // normalize any whitespace character to a single ASCII space.
            if (char.IsWhiteSpace(outCh))
            {
                if (!previousWasSpace)
                {
                    sb.Append(' ');
                    previousWasSpace = true;
                }
            }
            else
            {
                sb.Append(outCh);
                previousWasSpace = false;
            }
        }

        // trim leading/trailing spaces that may have been added.
        var collapsed = sb.ToString().Trim();
        return collapsed.ToLowerInvariant();
    }

    private List<(ItemValueType MagicNumber, string Value)> GetItemValuesToSave(BaseItemDto item, List<string> inheritedTags)
    {
        var list = new List<(ItemValueType, string)>();

        if (item is IHasArtist hasArtist)
        {
            list.AddRange(hasArtist.Artists.Select(i => ((ItemValueType)0, i)));
        }

        if (item is IHasAlbumArtist hasAlbumArtist)
        {
            list.AddRange(hasAlbumArtist.AlbumArtists.Select(i => (ItemValueType.AlbumArtist, i)));
        }

        list.AddRange(item.Genres.Select(i => (ItemValueType.Genre, i)));
        list.AddRange(item.Studios.Select(i => (ItemValueType.Studios, i)));
        list.AddRange(item.Tags.Select(i => (ItemValueType.Tags, i)));

        // keywords was 5

        list.AddRange(inheritedTags.Select(i => (ItemValueType.InheritedTags, i)));

        // Remove all invalid values.
        list.RemoveAll(i => string.IsNullOrWhiteSpace(i.Item2));

        return list;
    }

    private static BaseItemImageInfo Map(Guid baseItemId, ItemImageInfo e)
    {
        return new BaseItemImageInfo()
        {
            ItemId = baseItemId,
            Id = Guid.NewGuid(),
            Path = e.Path,
            Blurhash = e.BlurHash is null ? null : Encoding.UTF8.GetBytes(e.BlurHash),
            DateModified = e.DateModified,
            Height = e.Height,
            Width = e.Width,
            ImageType = (ImageInfoImageType)e.Type,
            Item = null!
        };
    }

    private static ItemImageInfo Map(BaseItemImageInfo e, IServerApplicationHost? appHost)
    {
        return new ItemImageInfo()
        {
            Path = appHost?.ExpandVirtualPath(e.Path) ?? e.Path,
            BlurHash = e.Blurhash is null ? null : Encoding.UTF8.GetString(e.Blurhash),
            DateModified = e.DateModified ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            Height = e.Height,
            Width = e.Width,
            Type = (ImageType)e.ImageType
        };
    }

    private string? GetPathToSave(string path)
    {
        if (path is null)
        {
            return null;
        }

        return _appHost.ReverseVirtualPath(path);
    }

    private List<string> GetItemByNameTypesInQuery(InternalItemsQuery query)
    {
        var list = new List<string>();

        if (IsTypeInQuery(BaseItemKind.Person, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.Person]!);
        }

        if (IsTypeInQuery(BaseItemKind.Genre, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre]!);
        }

        if (IsTypeInQuery(BaseItemKind.MusicGenre, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicGenre]!);
        }

        if (IsTypeInQuery(BaseItemKind.MusicArtist, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]!);
        }

        if (IsTypeInQuery(BaseItemKind.Studio, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.Studio]!);
        }

        return list;
    }

    private bool IsTypeInQuery(BaseItemKind type, InternalItemsQuery query)
    {
        if (query.ExcludeItemTypes.Contains(type))
        {
            return false;
        }

        return query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(type);
    }

    private bool EnableGroupByPresentationUniqueKey(InternalItemsQuery query)
    {
        if (!query.GroupByPresentationUniqueKey)
        {
            return false;
        }

        if (query.GroupBySeriesPresentationUniqueKey)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
        {
            return false;
        }

        if (query.User is null)
        {
            return false;
        }

        if (query.IncludeItemTypes.Length == 0)
        {
            return true;
        }

        return query.IncludeItemTypes.Contains(BaseItemKind.Episode)
            || query.IncludeItemTypes.Contains(BaseItemKind.Video)
            || query.IncludeItemTypes.Contains(BaseItemKind.Movie)
            || query.IncludeItemTypes.Contains(BaseItemKind.MusicVideo)
            || query.IncludeItemTypes.Contains(BaseItemKind.Series)
            || query.IncludeItemTypes.Contains(BaseItemKind.Season);
    }

    private IQueryable<BaseItemEntity> ApplyOrder(IQueryable<BaseItemEntity> query, InternalItemsQuery filter, JellyfinDbContext context)
    {
        var orderBy = filter.OrderBy;
        var hasSearch = !string.IsNullOrEmpty(filter.SearchTerm);

        if (hasSearch)
        {
            orderBy = filter.OrderBy = [(ItemSortBy.SortName, SortOrder.Ascending), .. orderBy];
        }
        else if (orderBy.Count == 0)
        {
            return query.OrderBy(e => e.SortName);
        }

        IOrderedQueryable<BaseItemEntity>? orderedQuery = null;

        var firstOrdering = orderBy.FirstOrDefault();
        if (firstOrdering != default)
        {
            var expression = OrderMapper.MapOrderByField(firstOrdering.OrderBy, filter, context);
            if (firstOrdering.SortOrder == SortOrder.Ascending)
            {
                orderedQuery = query.OrderBy(expression);
            }
            else
            {
                orderedQuery = query.OrderByDescending(expression);
            }

            if (firstOrdering.OrderBy is ItemSortBy.Default or ItemSortBy.SortName)
            {
                if (firstOrdering.SortOrder is SortOrder.Ascending)
                {
                    orderedQuery = orderedQuery.ThenBy(e => e.Name);
                }
                else
                {
                    orderedQuery = orderedQuery.ThenByDescending(e => e.Name);
                }
            }
        }

        foreach (var item in orderBy.Skip(1))
        {
            var expression = OrderMapper.MapOrderByField(item.OrderBy, filter, context);
            if (item.SortOrder == SortOrder.Ascending)
            {
                orderedQuery = orderedQuery!.ThenBy(expression);
            }
            else
            {
                orderedQuery = orderedQuery!.ThenByDescending(expression);
            }
        }

        return orderedQuery ?? query;
    }

    private IQueryable<BaseItemEntity> TranslateQuery(
        IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        InternalItemsQuery filter)
    {
        const int HDWidth = 1200;
        const int UHDWidth = 3800;
        const int UHDHeight = 2100;

        var minWidth = filter.MinWidth;
        var maxWidth = filter.MaxWidth;
        var now = DateTime.UtcNow;

        if (filter.IsHD.HasValue || filter.Is4K.HasValue)
        {
            bool includeSD = false;
            bool includeHD = false;
            bool include4K = false;

            if (filter.IsHD.HasValue && !filter.IsHD.Value)
            {
                includeSD = true;
            }

            if (filter.IsHD.HasValue && filter.IsHD.Value)
            {
                includeHD = true;
            }

            if (filter.Is4K.HasValue && filter.Is4K.Value)
            {
                include4K = true;
            }

            baseQuery = baseQuery.Where(e =>
                (includeSD && e.Width < HDWidth) ||
                (includeHD && e.Width >= HDWidth && !(e.Width >= UHDWidth || e.Height >= UHDHeight)) ||
                (include4K && (e.Width >= UHDWidth || e.Height >= UHDHeight)));
        }

        if (minWidth.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Width >= minWidth);
        }

        if (filter.MinHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height >= filter.MinHeight);
        }

        if (maxWidth.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Width <= maxWidth);
        }

        if (filter.MaxHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height <= filter.MaxHeight);
        }

        if (filter.IsLocked.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsLocked == filter.IsLocked);
        }

        var tags = filter.Tags.ToList();
        var excludeTags = filter.ExcludeTags.ToList();

        if (filter.IsMovie.HasValue)
        {
            var shouldIncludeAllMovieTypes = filter.IsMovie.Value
                && (filter.IncludeItemTypes.Length == 0
                    || filter.IncludeItemTypes.Contains(BaseItemKind.Movie)
                    || filter.IncludeItemTypes.Contains(BaseItemKind.Trailer));

            if (!shouldIncludeAllMovieTypes)
            {
                baseQuery = baseQuery.Where(e => e.IsMovie == filter.IsMovie.Value);
            }
        }

        if (filter.IsSeries.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsSeries == filter.IsSeries);
        }

        if (filter.IsSports.HasValue)
        {
            if (filter.IsSports.Value)
            {
                tags.Add("Sports");
            }
            else
            {
                excludeTags.Add("Sports");
            }
        }

        if (filter.IsNews.HasValue)
        {
            if (filter.IsNews.Value)
            {
                tags.Add("News");
            }
            else
            {
                excludeTags.Add("News");
            }
        }

        if (filter.IsKids.HasValue)
        {
            if (filter.IsKids.Value)
            {
                tags.Add("Kids");
            }
            else
            {
                excludeTags.Add("Kids");
            }
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var cleanedSearchTerm = GetCleanValue(filter.SearchTerm);
            var originalSearchTerm = filter.SearchTerm.ToLower();
            if (SearchWildcardTerms.Any(f => cleanedSearchTerm.Contains(f)))
            {
                cleanedSearchTerm = $"%{cleanedSearchTerm.Trim('%')}%";
                baseQuery = baseQuery.Where(e => EF.Functions.Like(e.CleanName!, cleanedSearchTerm) || (e.OriginalTitle != null && EF.Functions.Like(e.OriginalTitle.ToLower(), originalSearchTerm)));
            }
            else
            {
                baseQuery = baseQuery.Where(e => e.CleanName!.Contains(cleanedSearchTerm) || (e.OriginalTitle != null && e.OriginalTitle.ToLower().Contains(originalSearchTerm)));
            }
        }

        if (filter.IsFolder.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsFolder == filter.IsFolder);
        }

        var includeTypes = filter.IncludeItemTypes;

        // Only specify excluded types if no included types are specified
        if (filter.IncludeItemTypes.Length == 0)
        {
            var excludeTypes = filter.ExcludeItemTypes;
            if (excludeTypes.Length == 1)
            {
                if (_itemTypeLookup.BaseItemKindNames.TryGetValue(excludeTypes[0], out var excludeTypeName))
                {
                    baseQuery = baseQuery.Where(e => e.Type != excludeTypeName);
                }
            }
            else if (excludeTypes.Length > 1)
            {
                var excludeTypeName = new List<string>();
                foreach (var excludeType in excludeTypes)
                {
                    if (_itemTypeLookup.BaseItemKindNames.TryGetValue(excludeType, out var baseItemKindName))
                    {
                        excludeTypeName.Add(baseItemKindName!);
                    }
                }

                baseQuery = baseQuery.Where(e => !excludeTypeName.Contains(e.Type));
            }
        }
        else
        {
            string[] types = includeTypes.Select(f => _itemTypeLookup.BaseItemKindNames.GetValueOrDefault(f)).Where(e => e != null).ToArray()!;
            baseQuery = baseQuery.WhereOneOrMany(types, f => f.Type);
        }

        if (filter.ChannelIds.Count > 0)
        {
            baseQuery = baseQuery.Where(e => e.ChannelId != null && filter.ChannelIds.Contains(e.ChannelId.Value));
        }

        if (!filter.ParentId.IsEmpty())
        {
            baseQuery = baseQuery.Where(e => e.ParentId!.Value == filter.ParentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Path))
        {
            var pathToQuery = GetPathToSave(filter.Path);
            baseQuery = baseQuery.Where(e => e.Path == pathToQuery);
        }

        if (!string.IsNullOrWhiteSpace(filter.PresentationUniqueKey))
        {
            baseQuery = baseQuery.Where(e => e.PresentationUniqueKey == filter.PresentationUniqueKey);
        }

        if (filter.MinCommunityRating.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.CommunityRating >= filter.MinCommunityRating);
        }

        if (filter.MinIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber >= filter.MinIndexNumber);
        }

        if (filter.MinParentAndIndexNumber.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => (e.ParentIndexNumber == filter.MinParentAndIndexNumber.Value.ParentIndexNumber && e.IndexNumber >= filter.MinParentAndIndexNumber.Value.IndexNumber) || e.ParentIndexNumber > filter.MinParentAndIndexNumber.Value.ParentIndexNumber);
        }

        if (filter.MinDateCreated.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateCreated >= filter.MinDateCreated);
        }

        if (filter.MinDateLastSaved.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= filter.MinDateLastSaved.Value);
        }

        if (filter.MinDateLastSavedForUser.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= filter.MinDateLastSavedForUser.Value);
        }

        if (filter.IndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber == filter.IndexNumber.Value);
        }

        if (filter.ParentIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber == filter.ParentIndexNumber.Value);
        }

        if (filter.ParentIndexNumberNotEquals.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber != filter.ParentIndexNumberNotEquals.Value || e.ParentIndexNumber == null);
        }

        var minEndDate = filter.MinEndDate;
        var maxEndDate = filter.MaxEndDate;

        if (filter.HasAired.HasValue)
        {
            if (filter.HasAired.Value)
            {
                maxEndDate = DateTime.UtcNow;
            }
            else
            {
                minEndDate = DateTime.UtcNow;
            }
        }

        if (minEndDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.EndDate >= minEndDate);
        }

        if (maxEndDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.EndDate <= maxEndDate);
        }

        if (filter.MinStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate >= filter.MinStartDate.Value);
        }

        if (filter.MaxStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate <= filter.MaxStartDate.Value);
        }

        if (filter.MinPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate >= filter.MinPremiereDate.Value);
        }

        if (filter.MaxPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate <= filter.MaxPremiereDate.Value);
        }

        if (filter.TrailerTypes.Length > 0)
        {
            var trailerTypes = filter.TrailerTypes.Select(e => (int)e).ToArray();
            baseQuery = baseQuery.Where(e => trailerTypes.Any(f => e.TrailerTypes!.Any(w => w.Id == f)));
        }

        if (filter.IsAiring.HasValue)
        {
            if (filter.IsAiring.Value)
            {
                baseQuery = baseQuery.Where(e => e.StartDate <= now && e.EndDate >= now);
            }
            else
            {
                baseQuery = baseQuery.Where(e => e.StartDate > now && e.EndDate < now);
            }
        }

        if (filter.PersonIds.Length > 0)
        {
            var peopleEntityIds = context.BaseItems
                .WhereOneOrMany(filter.PersonIds, b => b.Id)
                .Join(
                    context.Peoples,
                    b => b.Name,
                    p => p.Name,
                    (b, p) => p.Id);

            baseQuery = baseQuery
                .Where(e => context.PeopleBaseItemMap
                    .Any(m => m.ItemId == e.Id && peopleEntityIds.Contains(m.PeopleId)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Person))
        {
            baseQuery = baseQuery.Where(e => e.Peoples!.Any(f => f.People.Name == filter.Person));
        }

        if (!string.IsNullOrWhiteSpace(filter.MinSortName))
        {
            // this does not makes sense.
            // baseQuery = baseQuery.Where(e => e.SortName >= query.MinSortName);
            // whereClauses.Add("SortName>=@MinSortName");
            // statement?.TryBind("@MinSortName", query.MinSortName);
        }

        if (!string.IsNullOrWhiteSpace(filter.ExternalSeriesId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalSeriesId == filter.ExternalSeriesId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ExternalId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalId == filter.ExternalId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var cleanName = GetCleanValue(filter.Name);
            baseQuery = baseQuery.Where(e => e.CleanName == cleanName);
        }

        // These are the same, for now
        var nameContains = filter.NameContains;
        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            if (SearchWildcardTerms.Any(f => nameContains.Contains(f)))
            {
                nameContains = $"%{nameContains.Trim('%')}%";
                baseQuery = baseQuery.Where(e => EF.Functions.Like(e.CleanName, nameContains) || EF.Functions.Like(e.OriginalTitle, nameContains));
            }
            else
            {
                baseQuery = baseQuery.Where(e =>
                                    e.CleanName!.Contains(nameContains)
                                    || e.OriginalTitle!.ToLower().Contains(nameContains!));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWith))
        {
            var startsWithLower = filter.NameStartsWith.ToLowerInvariant();
            baseQuery = baseQuery.Where(e => e.SortName!.StartsWith(startsWithLower));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWithOrGreater))
        {
            var startsOrGreaterLower = filter.NameStartsWithOrGreater.ToLowerInvariant();
            baseQuery = baseQuery.Where(e => e.SortName!.CompareTo(startsOrGreaterLower) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(filter.NameLessThan))
        {
            var lessThanLower = filter.NameLessThan.ToLowerInvariant();
            baseQuery = baseQuery.Where(e => e.SortName!.CompareTo(lessThanLower ) < 0);
        }

        if (filter.ImageTypes.Length > 0)
        {
            var imgTypes = filter.ImageTypes.Select(e => (ImageInfoImageType)e).ToArray();
            baseQuery = baseQuery.Where(e => imgTypes.Any(f => e.Images!.Any(w => w.ImageType == f)));
        }

        if (filter.IsLiked.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.UserData!.FirstOrDefault(f => f.UserId == filter.User!.Id)!.Rating >= UserItemData.MinLikeValue);
        }

        if (filter.IsFavoriteOrLiked.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.UserData!.FirstOrDefault(f => f.UserId == filter.User!.Id)!.IsFavorite == filter.IsFavoriteOrLiked);
        }

        if (filter.IsFavorite.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.UserData!.FirstOrDefault(f => f.UserId == filter.User!.Id)!.IsFavorite == filter.IsFavorite);
        }

        if (filter.IsPlayed.HasValue)
        {
            // We should probably figure this out for all folders, but for right now, this is the only place where we need it
            if (filter.IncludeItemTypes.Length == 1 && filter.IncludeItemTypes[0] == BaseItemKind.Series)
            {
                baseQuery = baseQuery.Where(e => context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId))
                    .Where(e => e.IsFolder == false && e.IsVirtualItem == false)
                    .Where(f => f.UserData!.FirstOrDefault(e => e.UserId == filter.User!.Id && e.Played)!.Played)
                    .Any(f => f.SeriesPresentationUniqueKey == e.PresentationUniqueKey) == filter.IsPlayed);
            }
            else
            {
                baseQuery = baseQuery
                    .Select(e => new
                    {
                        IsPlayed = e.UserData!.Where(f => f.UserId == filter.User!.Id).Select(f => (bool?)f.Played).FirstOrDefault() ?? false,
                        Item = e
                    })
                    .Where(e => e.IsPlayed == filter.IsPlayed)
                    .Select(f => f.Item);
            }
        }

        if (filter.IsResumable.HasValue)
        {
            if (filter.IsResumable.Value)
            {
                baseQuery = baseQuery
                       .Where(e => e.UserData!.FirstOrDefault(f => f.UserId == filter.User!.Id)!.PlaybackPositionTicks > 0);
            }
            else
            {
                baseQuery = baseQuery
                       .Where(e => e.UserData!.FirstOrDefault(f => f.UserId == filter.User!.Id)!.PlaybackPositionTicks == 0);
            }
        }

        if (filter.ArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItemMultipleTypes(context, [ItemValueType.Artist, ItemValueType.AlbumArtist], filter.ArtistIds);
        }

        if (filter.AlbumArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.AlbumArtist, filter.AlbumArtistIds);
        }

        if (filter.ContributingArtistIds.Length > 0)
        {
            var contributingNames = context.BaseItems
                .Where(b => filter.ContributingArtistIds.Contains(b.Id))
                .Select(b => b.CleanName);

            baseQuery = baseQuery.Where(e =>
                e.ItemValues!.Any(ivm =>
                    ivm.ItemValue.Type == ItemValueType.Artist &&
                    contributingNames.Contains(ivm.ItemValue.CleanValue))
                &&
                !e.ItemValues!.Any(ivm =>
                    ivm.ItemValue.Type == ItemValueType.AlbumArtist &&
                    contributingNames.Contains(ivm.ItemValue.CleanValue)));
        }

        if (filter.AlbumIds.Length > 0)
        {
            var subQuery = context.BaseItems.WhereOneOrMany(filter.AlbumIds, f => f.Id);
            baseQuery = baseQuery.Where(e => subQuery.Any(f => f.Name == e.Album));
        }

        if (filter.ExcludeArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItemMultipleTypes(context, [ItemValueType.Artist, ItemValueType.AlbumArtist], filter.ExcludeArtistIds, true);
        }

        if (filter.GenreIds.Count > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Genre, filter.GenreIds.ToArray());
        }

        if (filter.Genres.Count > 0)
        {
            var cleanGenres = filter.Genres.Select(e => GetCleanValue(e)).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Genre).Any(cleanGenres));
        }

        if (tags.Count > 0)
        {
            var cleanValues = tags.Select(e => GetCleanValue(e)).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Tags).Any(cleanValues));
        }

        if (excludeTags.Count > 0)
        {
            var cleanValues = excludeTags.Select(e => GetCleanValue(e)).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => !e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Tags).Any(cleanValues));
        }

        if (filter.StudioIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Studios, filter.StudioIds.ToArray());
        }

        if (filter.OfficialRatings.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => filter.OfficialRatings.Contains(e.OfficialRating));
        }

        Expression<Func<BaseItemEntity, bool>>? minParentalRatingFilter = null;
        if (filter.MinParentalRating != null)
        {
            var min = filter.MinParentalRating;
            var minScore = min.Score;
            var minSubScore = min.SubScore ?? 0;

            minParentalRatingFilter = e =>
                e.InheritedParentalRatingValue == null ||
                e.InheritedParentalRatingValue > minScore ||
                (e.InheritedParentalRatingValue == minScore && (e.InheritedParentalRatingSubValue ?? 0) >= minSubScore);
        }

        Expression<Func<BaseItemEntity, bool>>? maxParentalRatingFilter = null;
        if (filter.MaxParentalRating != null)
        {
            var max = filter.MaxParentalRating;
            var maxScore = max.Score;
            var maxSubScore = max.SubScore ?? 0;

            maxParentalRatingFilter = e =>
                e.InheritedParentalRatingValue == null ||
                e.InheritedParentalRatingValue < maxScore ||
                (e.InheritedParentalRatingValue == maxScore && (e.InheritedParentalRatingSubValue ?? 0) <= maxSubScore);
        }

        if (filter.HasParentalRating ?? false)
        {
            if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(minParentalRatingFilter);
            }

            if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(maxParentalRatingFilter);
            }
        }
        else if (filter.BlockUnratedItems.Length > 0)
        {
            var unratedItemTypes = filter.BlockUnratedItems.Select(f => f.ToString()).ToArray();
            Expression<Func<BaseItemEntity, bool>> unratedItemFilter = e => e.InheritedParentalRatingValue != null || !unratedItemTypes.Contains(e.UnratedType);

            if (minParentalRatingFilter != null && maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(minParentalRatingFilter.And(maxParentalRatingFilter)));
            }
            else if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(minParentalRatingFilter));
            }
            else if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(maxParentalRatingFilter));
            }
            else
            {
                baseQuery = baseQuery.Where(unratedItemFilter);
            }
        }
        else if (minParentalRatingFilter != null || maxParentalRatingFilter != null)
        {
            if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(minParentalRatingFilter);
            }

            if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(maxParentalRatingFilter);
            }
        }
        else if (!filter.HasParentalRating ?? false)
        {
            baseQuery = baseQuery
                .Where(e => e.InheritedParentalRatingValue == null);
        }

        if (filter.HasOfficialRating.HasValue)
        {
            if (filter.HasOfficialRating.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.OfficialRating != null && e.OfficialRating != string.Empty);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.OfficialRating == null || e.OfficialRating == string.Empty);
            }
        }

        if (filter.HasOverview.HasValue)
        {
            if (filter.HasOverview.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.Overview != null && e.Overview != string.Empty);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.Overview == null || e.Overview == string.Empty);
            }
        }

        if (filter.HasOwnerId.HasValue)
        {
            if (filter.HasOwnerId.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.OwnerId != null);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.OwnerId == null);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoAudioTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Audio && f.Language == filter.HasNoAudioTrackWithLanguage));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoInternalSubtitleTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle && !f.IsExternal && f.Language == filter.HasNoInternalSubtitleTrackWithLanguage));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoExternalSubtitleTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle && f.IsExternal && f.Language == filter.HasNoExternalSubtitleTrackWithLanguage));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoSubtitleTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle && f.Language == filter.HasNoSubtitleTrackWithLanguage));
        }

        if (filter.HasSubtitles.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle) == filter.HasSubtitles.Value);
        }

        if (filter.HasChapterImages.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.Chapters!.Any(f => f.ImagePath != null) == filter.HasChapterImages.Value);
        }

        if (filter.HasDeadParentId.HasValue && filter.HasDeadParentId.Value)
        {
            baseQuery = baseQuery
                .Where(e => e.ParentId.HasValue && !context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)).Any(f => f.Id == e.ParentId.Value));
        }

        if (filter.IsDeadArtist.HasValue && filter.IsDeadArtist.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getAllArtistsValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadStudio.HasValue && filter.IsDeadStudio.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getStudiosValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadGenre.HasValue && filter.IsDeadGenre.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getGenreValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadPerson.HasValue && filter.IsDeadPerson.Value)
        {
            baseQuery = baseQuery
                .Where(e => !context.Peoples.Any(f => f.Name == e.Name));
        }

        if (filter.Years.Length > 0)
        {
            baseQuery = baseQuery.WhereOneOrMany(filter.Years, e => e.ProductionYear!.Value);
        }

        var isVirtualItem = filter.IsVirtualItem ?? filter.IsMissing;
        if (isVirtualItem.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.IsVirtualItem == isVirtualItem.Value);
        }

        if (filter.IsSpecialSeason.HasValue)
        {
            if (filter.IsSpecialSeason.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.IndexNumber == 0);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.IndexNumber != 0);
            }
        }

        if (filter.IsUnaired.HasValue)
        {
            if (filter.IsUnaired.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.PremiereDate >= now);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.PremiereDate < now);
            }
        }

        if (filter.MediaTypes.Length > 0)
        {
            var mediaTypes = filter.MediaTypes.Select(f => f.ToString()).ToArray();
            baseQuery = baseQuery.WhereOneOrMany(mediaTypes, e => e.MediaType);
        }

        if (filter.ItemIds.Length > 0)
        {
            baseQuery = baseQuery.WhereOneOrMany(filter.ItemIds, e => e.Id);
        }

        if (filter.ExcludeItemIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => !filter.ExcludeItemIds.Contains(e.Id));
        }

        if (filter.ExcludeProviderIds is not null && filter.ExcludeProviderIds.Count > 0)
        {
            var exclude = filter.ExcludeProviderIds.Select(e => $"{e.Key}:{e.Value}").ToArray();
            baseQuery = baseQuery.Where(e => e.Provider!.Select(f => f.ProviderId + ":" + f.ProviderValue)!.All(f => !exclude.Contains(f)));
        }

        if (filter.HasAnyProviderId is not null && filter.HasAnyProviderId.Count > 0)
        {
            // Allow setting a null or empty value to get all items that have the specified provider set.
            var includeAny = filter.HasAnyProviderId.Where(e => string.IsNullOrEmpty(e.Value)).Select(e => e.Key).ToArray();
            if (includeAny.Length > 0)
            {
                baseQuery = baseQuery.Where(e => e.Provider!.Any(f => includeAny.Contains(f.ProviderId)));
            }

            var includeSelected = filter.HasAnyProviderId.Where(e => !string.IsNullOrEmpty(e.Value)).Select(e => $"{e.Key}:{e.Value}").ToArray();
            if (includeSelected.Length > 0)
            {
                baseQuery = baseQuery.Where(e => e.Provider!.Select(f => f.ProviderId + ":" + f.ProviderValue)!.Any(f => includeSelected.Contains(f)));
            }
        }

        if (filter.HasImdbId.HasValue)
        {
            baseQuery = filter.HasImdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == MetadataProvider.Imdb.ToString().ToLower()))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != MetadataProvider.Imdb.ToString().ToLower()));
        }

        if (filter.HasTmdbId.HasValue)
        {
            baseQuery = filter.HasTmdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == MetadataProvider.Tmdb.ToString().ToLower()))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != MetadataProvider.Tmdb.ToString().ToLower()));
        }

        if (filter.HasTvdbId.HasValue)
        {
            baseQuery = filter.HasTvdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == MetadataProvider.Tvdb.ToString().ToLower()))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != MetadataProvider.Tvdb.ToString().ToLower()));
        }

        var queryTopParentIds = filter.TopParentIds;

        if (queryTopParentIds.Length > 0)
        {
            var includedItemByNameTypes = GetItemByNameTypesInQuery(filter);
            var enableItemsByName = (filter.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0;
            if (enableItemsByName && includedItemByNameTypes.Count > 0)
            {
                baseQuery = baseQuery.Where(e => includedItemByNameTypes.Contains(e.Type) || queryTopParentIds.Any(w => w == e.TopParentId!.Value));
            }
            else
            {
                baseQuery = baseQuery.WhereOneOrMany(queryTopParentIds, e => e.TopParentId!.Value);
            }
        }

        if (filter.AncestorIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => e.Parents!.Any(f => filter.AncestorIds.Contains(f.ParentItemId)));
        }

        if (!string.IsNullOrWhiteSpace(filter.AncestorWithPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)).Where(f => f.PresentationUniqueKey == filter.AncestorWithPresentationUniqueKey).Any(f => f.Children!.Any(w => w.ItemId == e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.SeriesPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => e.SeriesPresentationUniqueKey == filter.SeriesPresentationUniqueKey);
        }

        if (filter.ExcludeInheritedTags.Length > 0)
        {
            baseQuery = baseQuery.Where(e =>
                !e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Tags && filter.ExcludeInheritedTags.Contains(f.ItemValue.CleanValue))
                && (e.Type != _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode] || !e.SeriesId.HasValue ||
                !context.ItemValuesMap.Any(f => f.ItemId == e.SeriesId.Value && f.ItemValue.Type == ItemValueType.Tags && filter.ExcludeInheritedTags.Contains(f.ItemValue.CleanValue))));
        }

        if (filter.IncludeInheritedTags.Length > 0)
        {
            // For seasons and episodes, we also need to check the parent series' tags.
            if (includeTypes.Any(t => t == BaseItemKind.Episode || t == BaseItemKind.Season))
            {
                baseQuery = baseQuery.Where(e =>
                    e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Tags && filter.IncludeInheritedTags.Contains(f.ItemValue.CleanValue))
                    || (e.SeriesId.HasValue && context.ItemValuesMap.Any(f => f.ItemId == e.SeriesId.Value && f.ItemValue.Type == ItemValueType.Tags && filter.IncludeInheritedTags.Contains(f.ItemValue.CleanValue))));
            }

            // A playlist should be accessible to its owner regardless of allowed tags.
            else if (includeTypes.Length == 1 && includeTypes.FirstOrDefault() is BaseItemKind.Playlist)
            {
                baseQuery = baseQuery.Where(e =>
                    e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Tags && filter.IncludeInheritedTags.Contains(f.ItemValue.CleanValue))
                    || e.Data!.Contains($"OwnerUserId\":\"{filter.User!.Id:N}\""));
                // d        ^^ this is stupid it hate this.
            }
            else
            {
                baseQuery = baseQuery.Where(e =>
                    e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Tags && filter.IncludeInheritedTags.Contains(f.ItemValue.CleanValue)));
            }
        }

        if (filter.SeriesStatuses.Length > 0)
        {
            var seriesStatus = filter.SeriesStatuses.Select(e => e.ToString()).ToArray();
            baseQuery = baseQuery
                .Where(e => seriesStatus.Any(f => e.Data!.Contains(f)));
        }

        if (filter.BoxSetLibraryFolders.Length > 0)
        {
            var boxsetFolders = filter.BoxSetLibraryFolders.Select(e => e.ToString("N", CultureInfo.InvariantCulture)).ToArray();
            baseQuery = baseQuery
                .Where(e => boxsetFolders.Any(f => e.Data!.Contains(f)));
        }

        if (filter.VideoTypes.Length > 0)
        {
            var videoTypeBs = filter.VideoTypes.Select(e => $"\"VideoType\":\"{e}\"");
            baseQuery = baseQuery
                .Where(e => videoTypeBs.Any(f => e.Data!.Contains(f)));
        }

        if (filter.Is3D.HasValue)
        {
            if (filter.Is3D.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.Data!.Contains("Video3DFormat"));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => !e.Data!.Contains("Video3DFormat"));
            }
        }

        if (filter.IsPlaceHolder.HasValue)
        {
            if (filter.IsPlaceHolder.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.Data!.Contains("IsPlaceHolder\":true"));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => !e.Data!.Contains("IsPlaceHolder\":true"));
            }
        }

        if (filter.HasSpecialFeature.HasValue)
        {
            if (filter.HasSpecialFeature.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.ExtraIds != null);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.ExtraIds == null);
            }
        }

        if (filter.HasTrailer.HasValue || filter.HasThemeSong.HasValue || filter.HasThemeVideo.HasValue)
        {
            if (filter.HasTrailer.GetValueOrDefault() || filter.HasThemeSong.GetValueOrDefault() || filter.HasThemeVideo.GetValueOrDefault())
            {
                baseQuery = baseQuery
                    .Where(e => e.ExtraIds != null);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.ExtraIds == null);
            }
        }

        return baseQuery;
    }

    /// <inheritdoc/>
    public async Task<bool> ItemExistsAsync(Guid id)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.BaseItems.AnyAsync(f => f.Id == id).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public bool GetIsPlayed(User user, Guid id, bool recursive)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        if (recursive)
        {
            var folderList = TraverseHirachyDown(id, dbContext, item => (item.IsFolder || item.IsVirtualItem));

            return dbContext.BaseItems
                    .Where(e => folderList.Contains(e.ParentId!.Value) && !e.IsFolder && !e.IsVirtualItem)
                    .All(f => f.UserData!.Any(e => e.UserId == user.Id && e.Played));
        }

        return dbContext.BaseItems.Where(e => e.ParentId == id).All(f => f.UserData!.Any(e => e.UserId == user.Id && e.Played));
    }

    private static HashSet<Guid> TraverseHirachyDown(Guid parentId, JellyfinDbContext dbContext, Expression<Func<BaseItemEntity, bool>>? filter = null)
    {
        var folderStack = new HashSet<Guid>()
            {
                parentId
            };
        var folderList = new HashSet<Guid>()
            {
                parentId
            };

        while (folderStack.Count != 0)
        {
            var items = folderStack.ToArray();
            folderStack.Clear();
            var query = dbContext.BaseItems
                .WhereOneOrMany(items, e => e.ParentId!.Value);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var item in query.Select(e => e.Id).ToArray())
            {
                if (folderList.Add(item))
                {
                    folderStack.Add(item);
                }
            }
        }

        return folderList;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var artists = dbContext.BaseItems.Where(e => e.Type == _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]!)
            .Where(e => artistNames.Contains(e.Name))
            .ToArray();

        return artists.GroupBy(e => e.Name).ToDictionary(e => e.Key!, e => e.Select(f => DeserializeBaseItem(f)).Cast<MusicArtist>().ToArray());
    }
}
