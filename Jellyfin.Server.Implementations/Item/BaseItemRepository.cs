#pragma warning disable RS0030 // Do not use banned APIs
// Do not enforce that because EFCore cannot deal with cultures well.
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
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
using BaseItemEntity = Jellyfin.Data.Entities.BaseItemEntity;

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
    private static readonly IReadOnlyList<ItemValueType> _getGenreValueTypes = [ItemValueType.Studios];

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
    public void DeleteItem(Guid id)
    {
        if (id.IsEmpty())
        {
            throw new ArgumentException("Guid can't be empty", nameof(id));
        }

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        context.PeopleBaseItemMap.Where(e => e.ItemId == id).ExecuteDelete();
        context.Peoples.Where(e => e.BaseItems!.Count == 0).ExecuteDelete();
        context.Chapters.Where(e => e.ItemId == id).ExecuteDelete();
        context.MediaStreamInfos.Where(e => e.ItemId == id).ExecuteDelete();
        context.AncestorIds.Where(e => e.ItemId == id || e.ParentItemId == id).ExecuteDelete();
        context.ItemValuesMap.Where(e => e.ItemId == id).ExecuteDelete();
        context.ItemValues.Where(e => e.BaseItemsMap!.Count == 0).ExecuteDelete();
        context.BaseItemImageInfos.Where(e => e.ItemId == id).ExecuteDelete();
        context.BaseItemProviders.Where(e => e.ItemId == id).ExecuteDelete();
        context.BaseItems.Where(e => e.Id == id).ExecuteDelete();
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
        return ApplyQueryFilter(context.BaseItems.AsNoTracking(), context, filter).Select(e => e.Id).ToArray();
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getAllArtistsValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getArtistValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getAlbumArtistValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getStudiosValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Studio]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getGenreValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery filter)
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
        if (!filter.EnableTotalRecordCount || (!filter.Limit.HasValue && (filter.StartIndex ?? 0) == 0))
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
        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = dbQuery.Count();
        }

        dbQuery = ApplyGroupingFilter(dbQuery, filter);
        dbQuery = ApplyQueryPageing(dbQuery, filter);

        result.Items = dbQuery.AsEnumerable().Where(e => e is not null).Select(w => DeserialiseBaseItem(w, filter.SkipDeserialization)).ToArray();
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

        dbQuery = ApplyGroupingFilter(dbQuery, filter);
        dbQuery = ApplyQueryPageing(dbQuery, filter);

        return dbQuery.AsEnumerable().Where(e => e is not null).Select(w => DeserialiseBaseItem(w, filter.SkipDeserialization)).ToArray();
    }

    private IQueryable<BaseItemEntity> ApplyGroupingFilter(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        // This whole block is needed to filter duplicate entries on request
        // for the time being it cannot be used because it would destroy the ordering
        // this results in "duplicate" responses for queries that try to lookup individual series or multiple versions but
        // for that case the invoker has to run a DistinctBy(e => e.PresentationUniqueKey) on their own

        // var enableGroupByPresentationUniqueKey = EnableGroupByPresentationUniqueKey(filter);
        // if (enableGroupByPresentationUniqueKey && filter.GroupBySeriesPresentationUniqueKey)
        // {
        //     dbQuery = ApplyOrder(dbQuery, filter);
        //     dbQuery = dbQuery.GroupBy(e => new { e.PresentationUniqueKey, e.SeriesPresentationUniqueKey }).Select(e => e.First());
        // }
        // else if (enableGroupByPresentationUniqueKey)
        // {
        //     dbQuery = ApplyOrder(dbQuery, filter);
        //     dbQuery = dbQuery.GroupBy(e => e.PresentationUniqueKey).Select(e => e.First());
        // }
        // else if (filter.GroupBySeriesPresentationUniqueKey)
        // {
        //     dbQuery = ApplyOrder(dbQuery, filter);
        //     dbQuery = dbQuery.GroupBy(e => e.SeriesPresentationUniqueKey).Select(e => e.First());
        // }
        // else
        // {
        //     dbQuery = dbQuery.Distinct();
        //     dbQuery = ApplyOrder(dbQuery, filter);
        // }
        dbQuery = dbQuery.Distinct();
        dbQuery = ApplyOrder(dbQuery, filter);

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyQueryPageing(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        if (filter.Limit.HasValue || filter.StartIndex.HasValue)
        {
            var offset = filter.StartIndex ?? 0;

            if (offset > 0)
            {
                dbQuery = dbQuery.Skip(offset);
            }

            if (filter.Limit.HasValue)
            {
                dbQuery = dbQuery.Take(filter.Limit.Value);
            }
        }

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyQueryFilter(IQueryable<BaseItemEntity> dbQuery, JellyfinDbContext context, InternalItemsQuery filter)
    {
        dbQuery = TranslateQuery(dbQuery, context, filter);
        dbQuery = ApplyOrder(dbQuery, filter);
        dbQuery = ApplyGroupingFilter(dbQuery, filter);
        dbQuery = ApplyQueryPageing(dbQuery, filter);
        return dbQuery;
    }

    private IQueryable<BaseItemEntity> PrepareItemQuery(JellyfinDbContext context, InternalItemsQuery filter)
    {
        IQueryable<BaseItemEntity> dbQuery = context.BaseItems.AsNoTracking().AsSplitQuery()
            .Include(e => e.TrailerTypes)
            .Include(e => e.Provider)
            .Include(e => e.LockedFields);

        if (filter.DtoOptions.EnableImages)
        {
            dbQuery = dbQuery.Include(e => e.Images);
        }

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

        // TODO: this isn't great. Refactor later to be both globally handled by a dedicated service not just an static variable and be loaded eagar.
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
        using var transaction = context.Database.BeginTransaction();
        context.BaseItemImageInfos.Where(e => e.ItemId == item.Id).ExecuteDelete();
        context.BaseItemImageInfos.AddRange(images);
        context.SaveChanges();
        transaction.Commit();
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
        foreach (var item in items.GroupBy(e => e.Id).Select(e => e.Last()))
        {
            var ancestorIds = item.SupportsAncestors ?
                item.GetAncestorIds().Distinct().ToList() :
                null;

            var topParent = item.GetTopParent();

            var userdataKey = item.GetUserDataKeys();
            var inheritedTags = item.GetInheritedTags();

            tuples.Add((item, ancestorIds, topParent, userdataKey, inheritedTags));
        }

        var localItemValueCache = new Dictionary<(int MagicNumber, string Value), Guid>();

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        foreach (var item in tuples)
        {
            var entity = Map(item.Item);
            // TODO: refactor this "inconsistency"
            entity.TopParentId = item.TopParent?.Id;

            if (!context.BaseItems.Any(e => e.Id == entity.Id))
            {
                context.BaseItems.Add(entity);
            }
            else
            {
                context.BaseItemProviders.Where(e => e.ItemId == entity.Id).ExecuteDelete();
                context.BaseItems.Attach(entity).State = EntityState.Modified;
            }

            context.AncestorIds.Where(e => e.ItemId == entity.Id).ExecuteDelete();
            if (item.Item.SupportsAncestors && item.AncestorIds != null)
            {
                foreach (var ancestorId in item.AncestorIds)
                {
                    if (!context.BaseItems.Any(f => f.Id == ancestorId))
                    {
                        continue;
                    }

                    context.AncestorIds.Add(new AncestorId()
                    {
                        ParentItemId = ancestorId,
                        ItemId = entity.Id,
                        Item = null!,
                        ParentItem = null!
                    });
                }
            }

            // Never save duplicate itemValues as they are now mapped anyway.
            var itemValuesToSave = GetItemValuesToSave(item.Item, item.InheritedTags).DistinctBy(e => (GetCleanValue(e.Value), e.MagicNumber));
            context.ItemValuesMap.Where(e => e.ItemId == entity.Id).ExecuteDelete();
            foreach (var itemValue in itemValuesToSave)
            {
                if (!localItemValueCache.TryGetValue(itemValue, out var refValue))
                {
                    refValue = context.ItemValues
                                .Where(f => f.CleanValue == GetCleanValue(itemValue.Value) && (int)f.Type == itemValue.MagicNumber)
                                .Select(e => e.ItemValueId)
                                .FirstOrDefault();
                }

                if (refValue.IsEmpty())
                {
                    context.ItemValues.Add(new ItemValue()
                    {
                        CleanValue = GetCleanValue(itemValue.Value),
                        Type = (ItemValueType)itemValue.MagicNumber,
                        ItemValueId = refValue = Guid.NewGuid(),
                        Value = itemValue.Value
                    });
                    localItemValueCache[itemValue] = refValue;
                }

                context.ItemValuesMap.Add(new ItemValueMap()
                {
                    Item = null!,
                    ItemId = entity.Id,
                    ItemValue = null!,
                    ItemValueId = refValue
                });
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
        var item = PrepareItemQuery(context, new()
        {
            DtoOptions = new()
            {
                EnableImages = true
            }
        }).FirstOrDefault(e => e.Id == id);
        if (item is null)
        {
            return null;
        }

        return DeserialiseBaseItem(item);
    }

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="dto">The dto base instance.</param>
    /// <param name="appHost">The Application server Host.</param>
    /// <returns>The dto to map.</returns>
    public static BaseItemDto Map(BaseItemEntity entity, BaseItemDto dto, IServerApplicationHost? appHost)
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
        dto.Genres = entity.Genres?.Split('|') ?? [];
        dto.DateCreated = entity.DateCreated.GetValueOrDefault();
        dto.DateModified = entity.DateModified.GetValueOrDefault();
        dto.ChannelId = string.IsNullOrWhiteSpace(entity.ChannelId) ? Guid.Empty : (Guid.TryParse(entity.ChannelId, out var channelId) ? channelId : Guid.Empty);
        dto.DateLastRefreshed = entity.DateLastRefreshed.GetValueOrDefault();
        dto.DateLastSaved = entity.DateLastSaved.GetValueOrDefault();
        dto.OwnerId = string.IsNullOrWhiteSpace(entity.OwnerId) ? Guid.Empty : (Guid.TryParse(entity.OwnerId, out var ownerId) ? ownerId : Guid.Empty);
        dto.Width = entity.Width.GetValueOrDefault();
        dto.Height = entity.Height.GetValueOrDefault();
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
        dto.Tags = entity.Tags?.Split('|') ?? [];

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
            hasStartDate.StartDate = entity.StartDate;
        }

        // Fields that are present in the DB but are never actually used
        // dto.UnratedType = entity.UnratedType;
        // dto.TopParentId = entity.TopParentId;
        // dto.CleanName = entity.CleanName;
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            folder.DateLastMediaAdded = entity.DateLastMediaAdded;
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
        entity.EndDate = dto.EndDate.GetValueOrDefault();
        entity.CommunityRating = dto.CommunityRating;
        entity.CustomRating = dto.CustomRating;
        entity.IndexNumber = dto.IndexNumber;
        entity.IsLocked = dto.IsLocked;
        entity.Name = dto.Name;
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
        entity.DateCreated = dto.DateCreated;
        entity.DateModified = dto.DateModified;
        entity.ChannelId = dto.ChannelId.ToString();
        entity.DateLastRefreshed = dto.DateLastRefreshed;
        entity.DateLastSaved = dto.DateLastSaved;
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

        // Fields that are present in the DB but are never actually used
        // dto.UnratedType = entity.UnratedType;
        // dto.TopParentId = entity.TopParentId;
        // dto.CleanName = entity.CleanName;
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            entity.DateLastMediaAdded = folder.DateLastMediaAdded;
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
        return query.Select(e => e.ItemValue.CleanValue).ToArray();
    }

    private static bool TypeRequiresDeserialization(Type type)
    {
        return type.GetCustomAttribute<RequiresSourceSerialisationAttribute>() == null;
    }

    private BaseItemDto DeserialiseBaseItem(BaseItemEntity baseItemEntity, bool skipDeserialization = false)
    {
        ArgumentNullException.ThrowIfNull(baseItemEntity, nameof(baseItemEntity));
        if (_serverConfigurationManager?.Configuration is null)
        {
            throw new InvalidOperationException("Server Configuration manager or configuration is null");
        }

        var typeToSerialise = GetType(baseItemEntity.Type);
        return BaseItemRepository.DeserialiseBaseItem(
            baseItemEntity,
            _logger,
            _appHost,
            skipDeserialization || (_serverConfigurationManager.Configuration.SkipDeserializationForBasicTypes && (typeToSerialise == typeof(Channel) || typeToSerialise == typeof(UserRootFolder))));
    }

    /// <summary>
    /// Deserialises a BaseItemEntity and sets all properties.
    /// </summary>
    /// <param name="baseItemEntity">The DB entity.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="appHost">The application server Host.</param>
    /// <param name="skipDeserialization">If only mapping should be processed.</param>
    /// <returns>A mapped BaseItem.</returns>
    /// <exception cref="InvalidOperationException">Will be thrown if an invalid serialisation is requested.</exception>
    public static BaseItemDto DeserialiseBaseItem(BaseItemEntity baseItemEntity, ILogger logger, IServerApplicationHost? appHost, bool skipDeserialization = false)
    {
        var type = GetType(baseItemEntity.Type) ?? throw new InvalidOperationException("Cannot deserialise unknown type.");
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
            dto = Activator.CreateInstance(type) as BaseItemDto ?? throw new InvalidOperationException("Cannot deserialise unknown type.");
        }

        return Map(baseItemEntity, dto, appHost);
    }

    private QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetItemValues(InternalItemsQuery filter, IReadOnlyList<ItemValueType> itemValueTypes, string returnType)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (!filter.Limit.HasValue)
        {
            filter.EnableTotalRecordCount = false;
        }

        using var context = _dbProvider.CreateDbContext();

        var innerQuery = new InternalItemsQuery(filter.User)
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
        };
        var query = TranslateQuery(context.BaseItems.AsNoTracking(), context, innerQuery);

        query = query.Where(e => e.Type == returnType && e.ItemValues!.Any(f => e.CleanName == f.ItemValue.CleanValue && itemValueTypes.Any(w => (ItemValueType)w == f.ItemValue.Type)));

        if (filter.OrderBy.Count != 0
            || !string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = ApplyOrder(query, filter);
        }
        else
        {
            query = query.OrderBy(e => e.SortName);
        }

        if (filter.Limit.HasValue || filter.StartIndex.HasValue)
        {
            var offset = filter.StartIndex ?? 0;

            if (offset > 0)
            {
                query = query.Skip(offset);
            }

            if (filter.Limit.HasValue)
            {
                query = query.Take(filter.Limit.Value);
            }
        }

        var result = new QueryResult<(BaseItemDto, ItemCounts)>();
        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = query.GroupBy(e => e.PresentationUniqueKey).Select(e => e.First()).Count();
        }

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
                SeriesCount = e.ItemValues!.Count(f => f.Item.Type == seriesTypeName),
                EpisodeCount = e.ItemValues!.Count(f => f.Item.Type == episodeTypeName),
                MovieCount = e.ItemValues!.Count(f => f.Item.Type == movieTypeName),
                AlbumCount = e.ItemValues!.Count(f => f.Item.Type == musicAlbumTypeName),
                ArtistCount = e.ItemValues!.Count(f => f.Item.Type == musicArtistTypeName),
                SongCount = e.ItemValues!.Count(f => f.Item.Type == audioTypeName),
                TrailerCount = e.ItemValues!.Count(f => f.Item.Type == trailerTypeName),
            }
        });

        result.StartIndex = filter.StartIndex ?? 0;
        result.Items = resultQuery.ToArray().Where(e => e is not null).Select(e =>
        {
            return (DeserialiseBaseItem(e.item, filter.SkipDeserialization), e.itemCount);
        }).ToArray();

        return result;
    }

    private static void PrepareFilterQuery(InternalItemsQuery query)
    {
        if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
        {
            query.Limit = query.Limit.Value + 4;
        }

        if (query.IsResumable ?? false)
        {
            query.IsVirtualItem = false;
        }
    }

    private string GetCleanValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.RemoveDiacritics().ToLowerInvariant();
    }

    private List<(int MagicNumber, string Value)> GetItemValuesToSave(BaseItemDto item, List<string> inheritedTags)
    {
        var list = new List<(int, string)>();

        if (item is IHasArtist hasArtist)
        {
            list.AddRange(hasArtist.Artists.Select(i => (0, i)));
        }

        if (item is IHasAlbumArtist hasAlbumArtist)
        {
            list.AddRange(hasAlbumArtist.AlbumArtists.Select(i => (1, i)));
        }

        list.AddRange(item.Genres.Select(i => (2, i)));
        list.AddRange(item.Studios.Select(i => (3, i)));
        list.AddRange(item.Tags.Select(i => (4, i)));

        // keywords was 5

        list.AddRange(inheritedTags.Select(i => (6, i)));

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
            DateModified = e.DateModified,
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

    private Expression<Func<BaseItemEntity, object>> MapOrderByField(ItemSortBy sortBy, InternalItemsQuery query)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return sortBy switch
        {
            ItemSortBy.AirTime => e => e.SortName, // TODO
            ItemSortBy.Runtime => e => e.RunTimeTicks,
            ItemSortBy.Random => e => EF.Functions.Random(),
            ItemSortBy.DatePlayed => e => e.UserData!.FirstOrDefault(f => f.UserId == query.User!.Id)!.LastPlayedDate,
            ItemSortBy.PlayCount => e => e.UserData!.FirstOrDefault(f => f.UserId == query.User!.Id)!.PlayCount,
            ItemSortBy.IsFavoriteOrLiked => e => e.UserData!.FirstOrDefault(f => f.UserId == query.User!.Id)!.IsFavorite,
            ItemSortBy.IsFolder => e => e.IsFolder,
            ItemSortBy.IsPlayed => e => e.UserData!.FirstOrDefault(f => f.UserId == query.User!.Id)!.Played,
            ItemSortBy.IsUnplayed => e => !e.UserData!.FirstOrDefault(f => f.UserId == query.User!.Id)!.Played,
            ItemSortBy.DateLastContentAdded => e => e.DateLastMediaAdded,
            ItemSortBy.Artist => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.Artist).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            ItemSortBy.AlbumArtist => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.AlbumArtist).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            ItemSortBy.Studio => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.Studios).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            ItemSortBy.OfficialRating => e => e.InheritedParentalRatingValue,
            // ItemSortBy.SeriesDatePlayed => "(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where Played=1 and B.SeriesPresentationUniqueKey=A.PresentationUniqueKey)",
            ItemSortBy.SeriesSortName => e => e.SeriesName,
            // ItemSortBy.AiredEpisodeOrder => "AiredEpisodeOrder",
            ItemSortBy.Album => e => e.Album,
            ItemSortBy.DateCreated => e => e.DateCreated,
            ItemSortBy.PremiereDate => e => e.PremiereDate,
            ItemSortBy.StartDate => e => e.StartDate,
            ItemSortBy.Name => e => e.Name,
            ItemSortBy.CommunityRating => e => e.CommunityRating,
            ItemSortBy.ProductionYear => e => e.ProductionYear,
            ItemSortBy.CriticRating => e => e.CriticRating,
            ItemSortBy.VideoBitRate => e => e.TotalBitrate,
            ItemSortBy.ParentIndexNumber => e => e.ParentIndexNumber,
            ItemSortBy.IndexNumber => e => e.IndexNumber,
            _ => e => e.SortName
        };
#pragma warning restore CS8603 // Possible null reference return.

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

    private IQueryable<BaseItemEntity> ApplyOrder(IQueryable<BaseItemEntity> query, InternalItemsQuery filter)
    {
        var orderBy = filter.OrderBy;
        var hasSearch = !string.IsNullOrEmpty(filter.SearchTerm);

        if (hasSearch)
        {
            orderBy = filter.OrderBy = [(ItemSortBy.SortName, SortOrder.Ascending), .. orderBy];
        }
        else if (orderBy.Count == 0)
        {
            return query;
        }

        IOrderedQueryable<BaseItemEntity>? orderedQuery = null;

        var firstOrdering = orderBy.FirstOrDefault();
        if (firstOrdering != default)
        {
            var expression = MapOrderByField(firstOrdering.OrderBy, filter);
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
            var expression = MapOrderByField(item.OrderBy, filter);
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
        var minWidth = filter.MinWidth;
        var maxWidth = filter.MaxWidth;
        var now = DateTime.UtcNow;

        if (filter.IsHD.HasValue)
        {
            const int Threshold = 1200;
            if (filter.IsHD.Value)
            {
                minWidth = Threshold;
            }
            else
            {
                maxWidth = Threshold - 1;
            }
        }

        if (filter.Is4K.HasValue)
        {
            const int Threshold = 3800;
            if (filter.Is4K.Value)
            {
                minWidth = Threshold;
            }
            else
            {
                maxWidth = Threshold - 1;
            }
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
            baseQuery = baseQuery.Where(e => e.Width >= maxWidth);
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

        if (filter.IsMovie == true)
        {
            if (filter.IncludeItemTypes.Length == 0
                || filter.IncludeItemTypes.Contains(BaseItemKind.Movie)
                || filter.IncludeItemTypes.Contains(BaseItemKind.Trailer))
            {
                baseQuery = baseQuery.Where(e => e.IsMovie);
            }
        }
        else if (filter.IsMovie.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsMovie == filter.IsMovie);
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
            var searchTerm = filter.SearchTerm.ToLower();
            baseQuery = baseQuery.Where(e => e.CleanName!.ToLower().Contains(searchTerm) || (e.OriginalTitle != null && e.OriginalTitle.ToLower().Contains(searchTerm)));
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
        else if (includeTypes.Length == 1)
        {
            if (_itemTypeLookup.BaseItemKindNames.TryGetValue(includeTypes[0], out var includeTypeName))
            {
                baseQuery = baseQuery.Where(e => e.Type == includeTypeName);
            }
        }
        else if (includeTypes.Length > 1)
        {
            var includeTypeName = new List<string>();
            foreach (var includeType in includeTypes)
            {
                if (_itemTypeLookup.BaseItemKindNames.TryGetValue(includeType, out var baseItemKindName))
                {
                    includeTypeName.Add(baseItemKindName!);
                }
            }

            baseQuery = baseQuery.Where(e => includeTypeName.Contains(e.Type));
        }

        if (filter.ChannelIds.Count > 0)
        {
            var channelIds = filter.ChannelIds.Select(e => e.ToString("N", CultureInfo.InvariantCulture)).ToArray();
            baseQuery = baseQuery.Where(e => channelIds.Contains(e.ChannelId));
        }

        if (!filter.ParentId.IsEmpty())
        {
            baseQuery = baseQuery.Where(e => e.ParentId!.Value == filter.ParentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Path))
        {
            baseQuery = baseQuery.Where(e => e.Path == filter.Path);
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
            baseQuery = baseQuery.Where(e => e.PremiereDate <= filter.MinPremiereDate.Value);
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
            baseQuery = baseQuery
                .Where(e =>
                    context.PeopleBaseItemMap.Where(w => context.BaseItems.Where(r => filter.PersonIds.Contains(r.Id)).Any(f => f.Name == w.People.Name))
                        .Any(f => f.ItemId == e.Id));
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
            baseQuery = baseQuery.Where(e =>
                e.CleanName!.Contains(nameContains)
                || e.OriginalTitle!.ToLower().Contains(nameContains!));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWith))
        {
            baseQuery = baseQuery.Where(e => e.SortName!.StartsWith(filter.NameStartsWith) || e.Name!.StartsWith(filter.NameStartsWith));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWithOrGreater))
        {
            // i hate this
            baseQuery = baseQuery.Where(e => e.SortName!.FirstOrDefault() > filter.NameStartsWithOrGreater[0] || e.Name!.FirstOrDefault() > filter.NameStartsWithOrGreater[0]);
        }

        if (!string.IsNullOrWhiteSpace(filter.NameLessThan))
        {
            // i hate this
            baseQuery = baseQuery.Where(e => e.SortName!.FirstOrDefault() < filter.NameLessThan[0] || e.Name!.FirstOrDefault() < filter.NameLessThan[0]);
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
                baseQuery = baseQuery.Where(e => context.BaseItems
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
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.ItemValue.Type <= ItemValueType.Artist && filter.ArtistIds.Contains(f.ItemId)));
        }

        if (filter.AlbumArtistIds.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Artist && filter.AlbumArtistIds.Contains(f.ItemId)));
        }

        if (filter.ContributingArtistIds.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Artist && filter.ContributingArtistIds.Contains(f.ItemId)));
        }

        if (filter.AlbumIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => context.BaseItems.Where(f => filter.AlbumIds.Contains(f.Id)).Any(f => f.Name == e.Album));
        }

        if (filter.ExcludeArtistIds.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => !e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Artist && filter.ExcludeArtistIds.Contains(f.ItemId)));
        }

        if (filter.GenreIds.Count > 0)
        {
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Genre && filter.GenreIds.Contains(f.ItemId)));
        }

        if (filter.Genres.Count > 0)
        {
            var cleanGenres = filter.Genres.Select(e => GetCleanValue(e)).ToArray();
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Genre && cleanGenres.Contains(f.ItemValue.CleanValue)));
        }

        if (tags.Count > 0)
        {
            var cleanValues = tags.Select(e => GetCleanValue(e)).ToArray();
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Tags && cleanValues.Contains(f.ItemValue.CleanValue)));
        }

        if (excludeTags.Count > 0)
        {
            var cleanValues = excludeTags.Select(e => GetCleanValue(e)).ToArray();
            baseQuery = baseQuery
                    .Where(e => !e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Tags && cleanValues.Contains(f.ItemValue.CleanValue)));
        }

        if (filter.StudioIds.Length > 0)
        {
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Any(f => f.ItemValue.Type == ItemValueType.Studios && filter.StudioIds.Contains(f.ItemId)));
        }

        if (filter.OfficialRatings.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => filter.OfficialRatings.Contains(e.OfficialRating));
        }

        if (filter.HasParentalRating ?? false)
        {
            if (filter.MinParentalRating.HasValue)
            {
                baseQuery = baseQuery
                   .Where(e => e.InheritedParentalRatingValue >= filter.MinParentalRating.Value);
            }

            if (filter.MaxParentalRating.HasValue)
            {
                baseQuery = baseQuery
                   .Where(e => e.InheritedParentalRatingValue < filter.MaxParentalRating.Value);
            }
        }
        else if (filter.BlockUnratedItems.Length > 0)
        {
            var unratedItems = filter.BlockUnratedItems.Select(f => f.ToString()).ToArray();
            if (filter.MinParentalRating.HasValue)
            {
                if (filter.MaxParentalRating.HasValue)
                {
                    baseQuery = baseQuery
                        .Where(e => (e.InheritedParentalRatingValue == null && !unratedItems.Contains(e.UnratedType))
                        || (e.InheritedParentalRatingValue >= filter.MinParentalRating && e.InheritedParentalRatingValue <= filter.MaxParentalRating));
                }
                else
                {
                    baseQuery = baseQuery
                        .Where(e => (e.InheritedParentalRatingValue == null && !unratedItems.Contains(e.UnratedType))
                        || e.InheritedParentalRatingValue >= filter.MinParentalRating);
                }
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.InheritedParentalRatingValue != null && !unratedItems.Contains(e.UnratedType));
            }
        }
        else if (filter.MinParentalRating.HasValue)
        {
            if (filter.MaxParentalRating.HasValue)
            {
                baseQuery = baseQuery
                    .Where(e => e.InheritedParentalRatingValue != null && e.InheritedParentalRatingValue >= filter.MinParentalRating.Value && e.InheritedParentalRatingValue <= filter.MaxParentalRating.Value);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.InheritedParentalRatingValue != null && e.InheritedParentalRatingValue >= filter.MinParentalRating.Value);
            }
        }
        else if (filter.MaxParentalRating.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.InheritedParentalRatingValue != null && e.InheritedParentalRatingValue >= filter.MaxParentalRating.Value);
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
                .Where(e => e.ParentId.HasValue && !context.BaseItems.Any(f => f.Id == e.ParentId.Value));
        }

        if (filter.IsDeadArtist.HasValue && filter.IsDeadArtist.Value)
        {
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Count(f => f.ItemValue.Type == ItemValueType.Artist || f.ItemValue.Type == ItemValueType.AlbumArtist) == 1);
        }

        if (filter.IsDeadStudio.HasValue && filter.IsDeadStudio.Value)
        {
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Count(f => f.ItemValue.Type == ItemValueType.Studios) == 1);
        }

        if (filter.IsDeadPerson.HasValue && filter.IsDeadPerson.Value)
        {
            baseQuery = baseQuery
                .Where(e => !context.Peoples.Any(f => f.Name == e.Name));
        }

        if (filter.Years.Length == 1)
        {
            baseQuery = baseQuery
                .Where(e => e.ProductionYear == filter.Years[0]);
        }
        else if (filter.Years.Length > 1)
        {
            baseQuery = baseQuery
                .Where(e => filter.Years.Any(f => f == e.ProductionYear));
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
            baseQuery = baseQuery
                .Where(e => mediaTypes.Contains(e.MediaType));
        }

        if (filter.ItemIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => filter.ItemIds.Contains(e.Id));
        }

        if (filter.ExcludeItemIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => !filter.ItemIds.Contains(e.Id));
        }

        if (filter.ExcludeProviderIds is not null && filter.ExcludeProviderIds.Count > 0)
        {
            baseQuery = baseQuery.Where(e => !e.Provider!.All(f => !filter.ExcludeProviderIds.All(w => f.ProviderId == w.Key && f.ProviderValue == w.Value)));
        }

        if (filter.HasAnyProviderId is not null && filter.HasAnyProviderId.Count > 0)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => !filter.HasAnyProviderId.Any(w => f.ProviderId == w.Key && f.ProviderValue == w.Value)));
        }

        if (filter.HasImdbId.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId == "imdb"));
        }

        if (filter.HasTmdbId.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId == "tmdb"));
        }

        if (filter.HasTvdbId.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId == "tvdb"));
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
                baseQuery = baseQuery.Where(e => queryTopParentIds.Contains(e.TopParentId!.Value));
            }
        }

        if (filter.AncestorIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => e.Children!.Any(f => filter.AncestorIds.Contains(f.ParentItemId)));
        }

        if (!string.IsNullOrWhiteSpace(filter.AncestorWithPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => context.BaseItems.Where(f => f.PresentationUniqueKey == filter.AncestorWithPresentationUniqueKey).Any(f => f.ParentAncestors!.Any(w => w.ItemId == f.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.SeriesPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => e.SeriesPresentationUniqueKey == filter.SeriesPresentationUniqueKey);
        }

        if (filter.ExcludeInheritedTags.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => !e.ItemValues!.Where(w => w.ItemValue.Type == ItemValueType.InheritedTags)
                    .Any(f => filter.ExcludeInheritedTags.Contains(f.ItemValue.CleanValue)));
        }

        if (filter.IncludeInheritedTags.Length > 0)
        {
            // Episodes do not store inherit tags from their parents in the database, and the tag may be still required by the client.
            // In addition to the tags for the episodes themselves, we need to manually query its parent (the season)'s tags as well.
            if (includeTypes.Length == 1 && includeTypes.FirstOrDefault() is BaseItemKind.Episode)
            {
                baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.InheritedTags)
                        .Any(f => filter.IncludeInheritedTags.Contains(f.ItemValue.CleanValue))
                        ||
                        (e.ParentId.HasValue && context.ItemValuesMap.Where(w => w.ItemId == e.ParentId.Value)!.Where(w => w.ItemValue.Type == ItemValueType.InheritedTags)
                        .Any(f => filter.IncludeInheritedTags.Contains(f.ItemValue.CleanValue))));
            }

            // A playlist should be accessible to its owner regardless of allowed tags.
            else if (includeTypes.Length == 1 && includeTypes.FirstOrDefault() is BaseItemKind.Playlist)
            {
                baseQuery = baseQuery
                    .Where(e =>
                    e.ParentAncestors!
                        .Any(f =>
                            f.ParentItem.ItemValues!.Any(w => w.ItemValue.Type == ItemValueType.Tags && filter.IncludeInheritedTags.Contains(w.ItemValue.CleanValue))
                            || e.Data!.Contains($"OwnerUserId\":\"{filter.User!.Id:N}\"")));
                // d        ^^ this is stupid it hate this.
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.ParentAncestors!.Any(f => f.ParentItem.ItemValues!.Any(w => w.ItemValue.Type == ItemValueType.Tags && filter.IncludeInheritedTags.Contains(w.ItemValue.CleanValue))));
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
}
