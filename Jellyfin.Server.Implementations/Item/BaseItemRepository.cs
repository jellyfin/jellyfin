using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Providers.Sqlite.Extensions;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
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
public sealed partial class BaseItemRepository
    : IItemRepository, IItemQueryHelpers
{
    /// <summary>
    /// Gets the placeholder id for UserData detached items.
    /// </summary>
    public static readonly Guid PlaceholderId = Guid.Parse("00000000-0000-0000-0000-000000000001");

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

    /// <summary>
    /// Maps a Entity to the DTO. Delegates to <see cref="BaseItemMapper"/>.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <param name="dto">The target DTO.</param>
    /// <param name="appHost">The application host.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The mapped DTO.</returns>
    public static BaseItemDto Map(BaseItemEntity entity, BaseItemDto dto, IServerApplicationHost? appHost, ILogger logger)
    {
        return BaseItemMapper.Map(entity, dto, appHost);
    }

    /// <summary>
    /// Maps a DTO to a database entity. Delegates to <see cref="BaseItemMapper"/>.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <returns>The mapped database entity.</returns>
    public BaseItemEntity Map(BaseItemDto dto)
    {
        return BaseItemMapper.Map(dto, _appHost);
    }

    /// <summary>
    /// Deserializes a BaseItemEntity and sets all properties.
    /// </summary>
    /// <param name="baseItemEntity">The entity to deserialize.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="appHost">The application host.</param>
    /// <param name="skipDeserialization">Whether to skip JSON deserialization.</param>
    /// <returns>The deserialized item, or null.</returns>
    public static BaseItemDto? DeserializeBaseItem(BaseItemEntity baseItemEntity, ILogger logger, IServerApplicationHost? appHost, bool skipDeserialization = false)
    {
        return BaseItemMapper.DeserializeBaseItem(baseItemEntity, logger, appHost, skipDeserialization);
    }

    /// <inheritdoc />
    public void PrepareFilterQuery(InternalItemsQuery query)
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
            return query.OrderBy(e => e.SortName);
        }

        IOrderedQueryable<BaseItemEntity>? orderedQuery = null;

        var firstOrdering = orderBy.FirstOrDefault();
        if (firstOrdering != default)
        {
            var expression = OrderMapper.MapOrderByField(firstOrdering.OrderBy, filter);
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
            var expression = OrderMapper.MapOrderByField(item.OrderBy, filter);
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
            baseQuery = baseQuery.SearchByTextCaseInsensitive(context, filter.SearchTerm);
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
            baseQuery = baseQuery.Where(e => e.SortName!.StartsWith(filter.NameStartsWith));
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
            return null;
        }

        return _appHost.ReverseVirtualPath(path);
    }

    /// <inheritdoc />
    public BaseItemDto? DeserializeBaseItem(BaseItemEntity entity, bool skipDeserialization = false)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        if (_serverConfigurationManager?.Configuration is null)
        {
            throw new InvalidOperationException("Server Configuration manager or configuration is null");
        }

        var typeToSerialise = BaseItemMapper.GetType(entity.Type);
        return BaseItemMapper.DeserializeBaseItem(
            entity,
            _logger,
            _appHost,
            skipDeserialization || (_serverConfigurationManager.Configuration.SkipDeserializationForBasicTypes && (typeToSerialise == typeof(Channel) || typeToSerialise == typeof(UserRootFolder))));
    }
}
