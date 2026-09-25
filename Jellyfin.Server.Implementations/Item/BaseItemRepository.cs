using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
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

    private static readonly BaseItemKind[] _itemByNameKinds =
    [
        BaseItemKind.Person,
        BaseItemKind.Genre,
        BaseItemKind.MusicGenre,
        BaseItemKind.MusicArtist,
        BaseItemKind.Studio
    ];

    private static readonly (BaseItemKind Kind, IReadOnlyList<ItemValueType> ValueTypes)[] _itemByNameValueTypes =
    [
        (BaseItemKind.Genre, _getGenreValueTypes),
        (BaseItemKind.MusicGenre, _getGenreValueTypes),
        (BaseItemKind.MusicArtist, _getAllArtistsValueTypes),
        (BaseItemKind.Studio, _getStudiosValueTypes)
    ];

    // The only folder kinds whose children form a single viewing sequence, so playback progress on a
    // child rolls up to them. Every other folder kind is a container that cannot be resumed.
    private static readonly BaseItemKind[] _resumableFolderKinds =
    [
        BaseItemKind.Series,
        BaseItemKind.Season
    ];

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

        // Resume queries surface the actually-played version (which may be an alternate sharing the
        // primary's presentation key). The resumable filter already keeps one version per group, so
        // presentation-key grouping must not collapse the surfaced version back onto the primary.
        if (query.IsResumable == true)
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

    private static BaseItemImageInfo Map(Guid baseItemId, ItemImageInfo e)
    {
        return BaseItemMapper.MapImageToEntity(baseItemId, e);
    }

    private static ItemImageInfo Map(BaseItemImageInfo e, IServerApplicationHost? appHost)
    {
        return BaseItemMapper.MapImageFromEntity(e, appHost);
    }

    private string? GetPathToSave(string path)
    {
        if (path is null)
        {
            return null;
        }

        return _appHost.ReverseVirtualPath(path);
    }

    private static LinkedChild ToLinkedChild(Guid childId, Database.Implementations.Entities.LinkedChildType childType)
        => new()
        {
            ItemId = childId,
            Type = (MediaBrowser.Controller.Entities.LinkedChildType)childType
        };

    /// <inheritdoc />
    public IReadOnlyList<BaseItemDto> LoadCollections(JellyfinDbContext context, InternalItemsQuery filter, IReadOnlyList<BaseItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return items;
        }

        var byId = new Dictionary<Guid, BaseItemDto>(items.Count);
        foreach (var item in items)
        {
            byId[item.Id] = item;
        }

        var ids = byId.Keys.ToArray();

        LoadLinkedChildren(context, byId);

        // Each of these is gated on the same option its join used to be, so a caller that asked for
        // less still gets less - it just no longer pays the product of everything it did ask for.
        if (filter.DtoOptions.ContainsField(ItemFields.ProviderIds))
        {
            var providers = context.BaseItemProviders
                .AsNoTracking()
                .WhereOneOrMany(ids, e => e.ItemId)
                .Select(e => new { e.ItemId, e.ProviderId, e.ProviderValue })
                .ToLookup(e => e.ItemId);

            foreach (var (id, item) in byId)
            {
                var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var provider in providers[id])
                {
                    dictionary[provider.ProviderId] = provider.ProviderValue;
                }

                item.ProviderIds = dictionary;
                item.MarkOwnedRowsRead(OwnedItemRows.Providers);
            }
        }

        if (filter.DtoOptions.ContainsField(ItemFields.Settings))
        {
            var lockedFields = context.BaseItemMetadataFields
                .AsNoTracking()
                .WhereOneOrMany(ids, e => e.ItemId)
                .Select(e => new { e.ItemId, e.Id })
                .ToLookup(e => e.ItemId);

            foreach (var (id, item) in byId)
            {
                item.LockedFields = [.. lockedFields[id].Select(e => (MetadataField)e.Id)];
                item.MarkOwnedRowsRead(OwnedItemRows.LockedFields);
            }
        }

        if (filter.DtoOptions.EnableUserData)
        {
            // Detached copies: the rows outlive the context the query ran on.
            var userData = context.UserData
                .AsNoTracking()
                .WhereOneOrMany(ids, e => e.ItemId)
                .ToList()
                .ToLookup(e => e.ItemId);

            foreach (var (id, item) in byId)
            {
                item.UserData = [.. userData[id].Select(DetachUserData)];
            }
        }

        if (filter.DtoOptions.EnableImages)
        {
            var images = context.BaseItemImageInfos
                .AsNoTracking()
                .WhereOneOrMany(ids, e => e.ItemId)
                .OrderBy(e => e.Id)
                .ToList()
                .ToLookup(e => e.ItemId);

            foreach (var (id, item) in byId)
            {
                item.ImageInfos = [.. images[id].Select(e => BaseItemMapper.MapImageFromEntity(e, _appHost))];
                item.MarkOwnedRowsRead(OwnedItemRows.Images);
            }
        }

        return items;
    }

    private static UserData DetachUserData(UserData row)
        => new()
        {
            ItemId = row.ItemId,
            Item = null,
            UserId = row.UserId,
            User = null,
            CustomDataKey = row.CustomDataKey,
            Rating = row.Rating,
            PlaybackPositionTicks = row.PlaybackPositionTicks,
            PlayCount = row.PlayCount,
            IsFavorite = row.IsFavorite,
            LastPlayedDate = row.LastPlayedDate,
            Played = row.Played,
            AudioStreamIndex = row.AudioStreamIndex,
            SubtitleStreamIndex = row.SubtitleStreamIndex,
            Likes = row.Likes,
            RetentionDate = row.RetentionDate
        };

    private static void LoadLinkedChildren(JellyfinDbContext context, Dictionary<Guid, BaseItemDto> byId)
    {
        // Only containers and videos can own links, so a result set of plain items costs no query.
        var owners = new Dictionary<Guid, BaseItemDto>();
        foreach (var (id, item) in byId)
        {
            if (item is Folder or Video)
            {
                owners[id] = item;
            }
        }

        if (owners.Count == 0)
        {
            return;
        }

        var linksByParent = context.LinkedChildren
            .AsNoTracking()
            .WhereOneOrMany(owners.Keys.ToArray(), e => e.ParentId)
            .OrderBy(e => e.SortOrder)
            .Select(e => new { e.ParentId, e.ChildId, e.ChildType })
            .ToLookup(e => e.ParentId);

        foreach (var (id, item) in owners)
        {
            // Assigned even when there are no rows: an unread container carries the same empty array,
            // and the save path tells "no links" from "not read yet" by nothing but that assignment.
            var links = linksByParent[id];

            if (item is Folder folder)
            {
                folder.LinkedChildren = [.. links.Select(e => ToLinkedChild(e.ChildId, e.ChildType))];
            }

            if (item is Video video)
            {
                // A video owns only the versions merged onto it by hand; any other link on it was
                // written by the container that holds it and must not be rewritten as the video's.
                video.LinkedAlternateVersions =
                [
                    .. links
                        .Where(e => e.ChildType == Database.Implementations.Entities.LinkedChildType.LinkedAlternateVersion)
                        .Select(e => ToLinkedChild(e.ChildId, e.ChildType))
                ];
            }
        }
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
