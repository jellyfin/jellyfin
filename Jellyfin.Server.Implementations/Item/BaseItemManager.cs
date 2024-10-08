using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;
using BaseItemEntity = Jellyfin.Data.Entities.BaseItem;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Handles all storage logic for BaseItems.
/// </summary>
public class BaseItemManager : IItemRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _appHost;

    private readonly ItemFields[] _allItemFields = Enum.GetValues<ItemFields>();

    private static readonly BaseItemKind[] _programTypes = new[]
    {
            BaseItemKind.Program,
            BaseItemKind.TvChannel,
            BaseItemKind.LiveTvProgram,
            BaseItemKind.LiveTvChannel
    };

    private static readonly BaseItemKind[] _programExcludeParentTypes = new[]
    {
            BaseItemKind.Series,
            BaseItemKind.Season,
            BaseItemKind.MusicAlbum,
            BaseItemKind.MusicArtist,
            BaseItemKind.PhotoAlbum
    };

    private static readonly BaseItemKind[] _serviceTypes = new[]
    {
            BaseItemKind.TvChannel,
            BaseItemKind.LiveTvChannel
    };

    private static readonly BaseItemKind[] _startDateTypes = new[]
    {
            BaseItemKind.Program,
            BaseItemKind.LiveTvProgram
    };

    private static readonly BaseItemKind[] _seriesTypes = new[]
    {
            BaseItemKind.Book,
            BaseItemKind.AudioBook,
            BaseItemKind.Episode,
            BaseItemKind.Season
    };

    private static readonly BaseItemKind[] _artistExcludeParentTypes = new[]
    {
            BaseItemKind.Series,
            BaseItemKind.Season,
            BaseItemKind.PhotoAlbum
    };

    private static readonly BaseItemKind[] _artistsTypes = new[]
    {
            BaseItemKind.Audio,
            BaseItemKind.MusicAlbum,
            BaseItemKind.MusicVideo,
            BaseItemKind.AudioBook
    };

    private static readonly Dictionary<BaseItemKind, string?> _baseItemKindNames = new()
        {
            { BaseItemKind.AggregateFolder, typeof(AggregateFolder).FullName },
            { BaseItemKind.Audio, typeof(Audio).FullName },
            { BaseItemKind.AudioBook, typeof(AudioBook).FullName },
            { BaseItemKind.BasePluginFolder, typeof(BasePluginFolder).FullName },
            { BaseItemKind.Book, typeof(Book).FullName },
            { BaseItemKind.BoxSet, typeof(BoxSet).FullName },
            { BaseItemKind.Channel, typeof(Channel).FullName },
            { BaseItemKind.CollectionFolder, typeof(CollectionFolder).FullName },
            { BaseItemKind.Episode, typeof(Episode).FullName },
            { BaseItemKind.Folder, typeof(Folder).FullName },
            { BaseItemKind.Genre, typeof(Genre).FullName },
            { BaseItemKind.Movie, typeof(Movie).FullName },
            { BaseItemKind.LiveTvChannel, typeof(LiveTvChannel).FullName },
            { BaseItemKind.LiveTvProgram, typeof(LiveTvProgram).FullName },
            { BaseItemKind.MusicAlbum, typeof(MusicAlbum).FullName },
            { BaseItemKind.MusicArtist, typeof(MusicArtist).FullName },
            { BaseItemKind.MusicGenre, typeof(MusicGenre).FullName },
            { BaseItemKind.MusicVideo, typeof(MusicVideo).FullName },
            { BaseItemKind.Person, typeof(Person).FullName },
            { BaseItemKind.Photo, typeof(Photo).FullName },
            { BaseItemKind.PhotoAlbum, typeof(PhotoAlbum).FullName },
            { BaseItemKind.Playlist, typeof(Playlist).FullName },
            { BaseItemKind.PlaylistsFolder, typeof(PlaylistsFolder).FullName },
            { BaseItemKind.Season, typeof(Season).FullName },
            { BaseItemKind.Series, typeof(Series).FullName },
            { BaseItemKind.Studio, typeof(Studio).FullName },
            { BaseItemKind.Trailer, typeof(Trailer).FullName },
            { BaseItemKind.TvChannel, typeof(LiveTvChannel).FullName },
            { BaseItemKind.TvProgram, typeof(LiveTvProgram).FullName },
            { BaseItemKind.UserRootFolder, typeof(UserRootFolder).FullName },
            { BaseItemKind.UserView, typeof(UserView).FullName },
            { BaseItemKind.Video, typeof(Video).FullName },
            { BaseItemKind.Year, typeof(Year).FullName }
        };

    /// <summary>
    /// This holds all the types in the running assemblies
    /// so that we can de-serialize properly when we don't have strong types.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type?> _typeMap = new ConcurrentDictionary<string, Type?>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItemManager"/> class.
    /// </summary>
    /// <param name="dbProvider">The db factory.</param>
    /// <param name="appHost">The Application host.</param>
    public BaseItemManager(IDbContextFactory<JellyfinDbContext> dbProvider, IServerApplicationHost appHost)
    {
        _dbProvider = dbProvider;
        _appHost = appHost;
    }

    private QueryResult<(BaseItemDto Item, ItemCounts ItemCounts)> GetItemValues(InternalItemsQuery filter, int[] itemValueTypes, string returnType)
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
        var query = TranslateQuery(context.BaseItems, context, innerQuery);

        query = query.Where(e => e.Type == returnType && e.ItemValues!.Any(f => e.CleanName == f.CleanValue && itemValueTypes.Contains(f.Type)));

        var outerQuery = new InternalItemsQuery(filter.User)
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
            SimilarTo = filter.SimilarTo,
            ExcludeItemIds = filter.ExcludeItemIds
        };
        query = TranslateQuery(query, context, outerQuery)
            .OrderBy(e => e.PresentationUniqueKey);

        if (filter.OrderBy.Count != 0
            || filter.SimilarTo is not null
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
                query.Take(filter.Limit.Value);
            }
        }

        var result = new QueryResult<(BaseItem, ItemCounts)>();
        string countText = string.Empty;
        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = query.DistinctBy(e => e.PresentationUniqueKey).Count();
        }

        var resultQuery = query.Select(e => new
        {
            item = e,
            itemCount = new ItemCounts()
            {
                SeriesCount = e.ItemValues!.Count(e => e.Type == (int)BaseItemKind.Series),
                EpisodeCount = e.ItemValues!.Count(e => e.Type == (int)BaseItemKind.Episode),
                MovieCount = e.ItemValues!.Count(e => e.Type == (int)BaseItemKind.Movie),
                AlbumCount = e.ItemValues!.Count(e => e.Type == (int)BaseItemKind.MusicAlbum),
                ArtistCount = e.ItemValues!.Count(e => e.Type == 0 || e.Type == 1),
                SongCount = e.ItemValues!.Count(e => e.Type == (int)BaseItemKind.MusicAlbum),
                TrailerCount = e.ItemValues!.Count(e => e.Type == (int)BaseItemKind.Trailer),
            }
        });

        result.StartIndex = filter.StartIndex ?? 0;
        result.Items = resultQuery.ToImmutableArray().Select(e =>
        {
            return (DeserialiseBaseItem(e.item), e.itemCount);
        }).ToImmutableArray();

        return result;
    }

    /// <inheritdoc />
    public void DeleteItem(Guid id)
    {
        ArgumentNullException.ThrowIfNull(id.IsEmpty() ? null : id);

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        context.Peoples.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.Chapters.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.MediaStreamInfos.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.AncestorIds.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.ItemValues.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.BaseItems.Where(e => e.Id.Equals(id)).ExecuteDelete();
        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc />
    public void UpdateInheritedValues()
    {
        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        context.ItemValues.Where(e => e.Type == 6).ExecuteDelete();
        context.ItemValues.AddRange(context.ItemValues.Where(e => e.Type == 4).Select(e => new Data.Entities.ItemValue()
        {
            CleanValue = e.CleanValue,
            ItemId = e.ItemId,
            Type = 6,
            Value = e.Value,
            Item = null!
        }));

        context.ItemValues.AddRange(
            context.AncestorIds.Where(e => e.AncestorIdText != null).Join(context.ItemValues.Where(e => e.Value != null && e.Type == 4), e => e.Id, e => e.ItemId, (e, f) => new Data.Entities.ItemValue()
            {
                CleanValue = f.CleanValue,
                ItemId = e.ItemId,
                Item = null!,
                Type = 6,
                Value = f.Value
            }));
        context.SaveChanges();

        transaction.Commit();
    }

    /// <inheritdoc cref="IItemRepository"/>
    public IReadOnlyList<Guid> GetItemIdsList(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems.AsNoTracking(), context, filter)
            .DistinctBy(e => e.Id);

        var enableGroupByPresentationUniqueKey = EnableGroupByPresentationUniqueKey(filter);
        if (enableGroupByPresentationUniqueKey && filter.GroupBySeriesPresentationUniqueKey)
        {
            dbQuery = dbQuery.GroupBy(e => new { e.PresentationUniqueKey, e.SeriesPresentationUniqueKey }).SelectMany(e => e);
        }

        if (enableGroupByPresentationUniqueKey)
        {
            dbQuery = dbQuery.GroupBy(e => e.PresentationUniqueKey).SelectMany(e => e);
        }

        if (filter.GroupBySeriesPresentationUniqueKey)
        {
            dbQuery = dbQuery.GroupBy(e => e.SeriesPresentationUniqueKey).SelectMany(e => e);
        }

        dbQuery = ApplyOrder(dbQuery, filter);

        return Pageinate(dbQuery, filter).Select(e => e.Id).ToImmutableArray();
    }

    /// <inheritdoc />
    public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery query)
    {
        return GetItemValues(query, new[] { 0, 1 }, typeof(MusicArtist).FullName);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery query)
    {
        return GetItemValues(query, new[] { 0 }, typeof(MusicArtist).FullName);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery query)
    {
        return GetItemValues(query, new[] { 1 }, typeof(MusicArtist).FullName);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery query)
    {
        return GetItemValues(query, new[] { 3 }, typeof(Studio).FullName);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery query)
    {
        return GetItemValues(query, new[] { 2 }, typeof(Genre).FullName);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery query)
    {
        return GetItemValues(query, new[] { 2 }, typeof(MusicGenre).FullName);
    }

    /// <inheritdoc cref="IItemRepository"/>
    public QueryResult<BaseItemDto> GetItems(InternalItemsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.EnableTotalRecordCount || (!query.Limit.HasValue && (query.StartIndex ?? 0) == 0))
        {
            var returnList = GetItemList(query);
            return new QueryResult<BaseItemDto>(
                query.StartIndex,
                returnList.Count,
                returnList);
        }

        PrepareFilterQuery(query);
        var result = new QueryResult<BaseItemDto>();

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems, context, query)
            .DistinctBy(e => e.Id);
        if (query.EnableTotalRecordCount)
        {
            result.TotalRecordCount = dbQuery.Count();
        }

        if (query.Limit.HasValue || query.StartIndex.HasValue)
        {
            var offset = query.StartIndex ?? 0;

            if (offset > 0)
            {
                dbQuery = dbQuery.Skip(offset);
            }

            if (query.Limit.HasValue)
            {
                dbQuery = dbQuery.Take(query.Limit.Value);
            }
        }

        result.Items = dbQuery.ToList().Select(DeserialiseBaseItem).ToImmutableArray();
        result.StartIndex = query.StartIndex ?? 0;
        return result;
    }

    /// <inheritdoc cref="IItemRepository"/>
    public IReadOnlyList<BaseItemDto> GetItemList(InternalItemsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        PrepareFilterQuery(query);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems, context, query)
            .DistinctBy(e => e.Id);
        if (query.Limit.HasValue || query.StartIndex.HasValue)
        {
            var offset = query.StartIndex ?? 0;

            if (offset > 0)
            {
                dbQuery = dbQuery.Skip(offset);
            }

            if (query.Limit.HasValue)
            {
                dbQuery = dbQuery.Take(query.Limit.Value);
            }
        }

        return dbQuery.ToList().Select(DeserialiseBaseItem).ToImmutableArray();
    }

    /// <inheritdoc/>
    public int GetCount(InternalItemsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        // Hack for right now since we currently don't support filtering out these duplicates within a query
        PrepareFilterQuery(query);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems, context, query);

        return dbQuery.Count();
    }

    private IQueryable<BaseItemEntity> TranslateQuery(
        IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        InternalItemsQuery query)
    {
        var minWidth = query.MinWidth;
        var maxWidth = query.MaxWidth;
        var now = DateTime.UtcNow;

        if (query.IsHD.HasValue)
        {
            const int Threshold = 1200;
            if (query.IsHD.Value)
            {
                minWidth = Threshold;
            }
            else
            {
                maxWidth = Threshold - 1;
            }
        }

        if (query.Is4K.HasValue)
        {
            const int Threshold = 3800;
            if (query.Is4K.Value)
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

        if (query.MinHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height >= query.MinHeight);
        }

        if (maxWidth.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Width >= maxWidth);
        }

        if (query.MaxHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height <= query.MaxHeight);
        }

        if (query.IsLocked.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsLocked == query.IsLocked);
        }

        var tags = query.Tags.ToList();
        var excludeTags = query.ExcludeTags.ToList();

        if (query.IsMovie == true)
        {
            if (query.IncludeItemTypes.Length == 0
                || query.IncludeItemTypes.Contains(BaseItemKind.Movie)
                || query.IncludeItemTypes.Contains(BaseItemKind.Trailer))
            {
                baseQuery = baseQuery.Where(e => e.IsMovie);
            }
        }
        else if (query.IsMovie.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsMovie == query.IsMovie);
        }

        if (query.IsSeries.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsSeries == query.IsSeries);
        }

        if (query.IsSports.HasValue)
        {
            if (query.IsSports.Value)
            {
                tags.Add("Sports");
            }
            else
            {
                excludeTags.Add("Sports");
            }
        }

        if (query.IsNews.HasValue)
        {
            if (query.IsNews.Value)
            {
                tags.Add("News");
            }
            else
            {
                excludeTags.Add("News");
            }
        }

        if (query.IsKids.HasValue)
        {
            if (query.IsKids.Value)
            {
                tags.Add("Kids");
            }
            else
            {
                excludeTags.Add("Kids");
            }
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            baseQuery = baseQuery.Where(e => e.CleanName!.Contains(query.SearchTerm, StringComparison.InvariantCultureIgnoreCase) || (e.OriginalTitle != null && e.OriginalTitle.Contains(query.SearchTerm, StringComparison.InvariantCultureIgnoreCase)));
        }

        if (query.IsFolder.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsFolder == query.IsFolder);
        }

        var includeTypes = query.IncludeItemTypes;
        // Only specify excluded types if no included types are specified
        if (query.IncludeItemTypes.Length == 0)
        {
            var excludeTypes = query.ExcludeItemTypes;
            if (excludeTypes.Length == 1)
            {
                if (_baseItemKindNames.TryGetValue(excludeTypes[0], out var excludeTypeName))
                {
                    baseQuery = baseQuery.Where(e => e.Type != excludeTypeName);
                }
            }
            else if (excludeTypes.Length > 1)
            {
                var excludeTypeName = new List<string>();
                foreach (var excludeType in excludeTypes)
                {
                    if (_baseItemKindNames.TryGetValue(excludeType, out var baseItemKindName))
                    {
                        excludeTypeName.Add(baseItemKindName!);
                    }
                }

                baseQuery = baseQuery.Where(e => !excludeTypeName.Contains(e.Type));
            }
        }
        else if (includeTypes.Length == 1)
        {
            if (_baseItemKindNames.TryGetValue(includeTypes[0], out var includeTypeName))
            {
                baseQuery = baseQuery.Where(e => e.Type == includeTypeName);
            }
        }
        else if (includeTypes.Length > 1)
        {
            var includeTypeName = new List<string>();
            foreach (var includeType in includeTypes)
            {
                if (_baseItemKindNames.TryGetValue(includeType, out var baseItemKindName))
                {
                    includeTypeName.Add(baseItemKindName!);
                }
            }

            baseQuery = baseQuery.Where(e => includeTypeName.Contains(e.Type));
        }

        if (query.ChannelIds.Count == 1)
        {
            baseQuery = baseQuery.Where(e => e.ChannelId == query.ChannelIds[0].ToString("N", CultureInfo.InvariantCulture));
        }
        else if (query.ChannelIds.Count > 1)
        {
            baseQuery = baseQuery.Where(e => query.ChannelIds.Select(f => f.ToString("N", CultureInfo.InvariantCulture)).Contains(e.ChannelId));
        }

        if (!query.ParentId.IsEmpty())
        {
            baseQuery = baseQuery.Where(e => e.ParentId.Equals(query.ParentId));
        }

        if (!string.IsNullOrWhiteSpace(query.Path))
        {
            baseQuery = baseQuery.Where(e => e.Path == query.Path);
        }

        if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
        {
            baseQuery = baseQuery.Where(e => e.PresentationUniqueKey == query.PresentationUniqueKey);
        }

        if (query.MinCommunityRating.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.CommunityRating >= query.MinCommunityRating);
        }

        if (query.MinIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber >= query.MinIndexNumber);
        }

        if (query.MinParentAndIndexNumber.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => (e.ParentIndexNumber == query.MinParentAndIndexNumber.Value.ParentIndexNumber && e.IndexNumber >= query.MinParentAndIndexNumber.Value.IndexNumber) || e.ParentIndexNumber > query.MinParentAndIndexNumber.Value.ParentIndexNumber);
        }

        if (query.MinDateCreated.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateCreated >= query.MinDateCreated);
        }

        if (query.MinDateLastSaved.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= query.MinDateLastSaved.Value);
        }

        if (query.MinDateLastSavedForUser.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= query.MinDateLastSavedForUser.Value);
        }

        if (query.IndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber == query.IndexNumber.Value);
        }

        if (query.ParentIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber == query.ParentIndexNumber.Value);
        }

        if (query.ParentIndexNumberNotEquals.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber != query.ParentIndexNumberNotEquals.Value || e.ParentIndexNumber == null);
        }

        var minEndDate = query.MinEndDate;
        var maxEndDate = query.MaxEndDate;

        if (query.HasAired.HasValue)
        {
            if (query.HasAired.Value)
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

        if (query.MinStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate >= query.MinStartDate.Value);
        }

        if (query.MaxStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate <= query.MaxStartDate.Value);
        }

        if (query.MinPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate <= query.MinPremiereDate.Value);
        }

        if (query.MaxPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate <= query.MaxPremiereDate.Value);
        }

        if (query.TrailerTypes.Length > 0)
        {
            baseQuery = baseQuery.Where(e => query.TrailerTypes.Any(f => e.TrailerTypes!.Contains(f.ToString(), StringComparison.OrdinalIgnoreCase)));
        }

        if (query.IsAiring.HasValue)
        {
            if (query.IsAiring.Value)
            {
                baseQuery = baseQuery.Where(e => e.StartDate <= now && e.EndDate >= now);
            }
            else
            {
                baseQuery = baseQuery.Where(e => e.StartDate > now && e.EndDate < now);
            }
        }

        if (query.PersonIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e =>
                    context.Peoples.Where(w => context.BaseItems.Where(w => query.PersonIds.Contains(w.Id)).Any(f => f.Name == w.Name))
                        .Any(f => f.ItemId.Equals(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(query.Person))
        {
            baseQuery = baseQuery.Where(e => e.Peoples!.Any(f => f.Name == query.Person));
        }

        if (!string.IsNullOrWhiteSpace(query.MinSortName))
        {
            // this does not makes sense.
            // baseQuery = baseQuery.Where(e => e.SortName >= query.MinSortName);
            // whereClauses.Add("SortName>=@MinSortName");
            // statement?.TryBind("@MinSortName", query.MinSortName);
        }

        if (!string.IsNullOrWhiteSpace(query.ExternalSeriesId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalSeriesId == query.ExternalSeriesId);
        }

        if (!string.IsNullOrWhiteSpace(query.ExternalId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalId == query.ExternalId);
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var cleanName = GetCleanValue(query.Name);
            baseQuery = baseQuery.Where(e => e.CleanName == cleanName);
        }

        // These are the same, for now
        var nameContains = query.NameContains;
        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            baseQuery = baseQuery.Where(e =>
                e.CleanName == query.NameContains
                || e.OriginalTitle!.Contains(query.NameContains!, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(query.NameStartsWith))
        {
            baseQuery = baseQuery.Where(e => e.SortName!.Contains(query.NameStartsWith, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.NameStartsWithOrGreater))
        {
            // i hate this
            baseQuery = baseQuery.Where(e => e.SortName![0] > query.NameStartsWithOrGreater[0]);
        }

        if (!string.IsNullOrWhiteSpace(query.NameLessThan))
        {
            // i hate this
            baseQuery = baseQuery.Where(e => e.SortName![0] < query.NameLessThan[0]);
        }

        if (query.ImageTypes.Length > 0)
        {
            baseQuery = baseQuery.Where(e => query.ImageTypes.Any(f => e.Images!.Contains(f.ToString(), StringComparison.InvariantCulture)));
        }

        if (query.IsLiked.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.Rating >= UserItemData.MinLikeValue);
        }

        if (query.IsFavoriteOrLiked.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.IsFavorite == query.IsFavoriteOrLiked);
        }

        if (query.IsFavorite.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.IsFavorite == query.IsFavorite);
        }

        if (query.IsPlayed.HasValue)
        {
            baseQuery = baseQuery
                   .Where(e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.Played == query.IsPlayed.Value);
        }

        if (query.IsResumable.HasValue)
        {
            if (query.IsResumable.Value)
            {
                baseQuery = baseQuery
                       .Where(e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.PlaybackPositionTicks > 0);
            }
            else
            {
                baseQuery = baseQuery
                       .Where(e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.PlaybackPositionTicks == 0);
            }
        }

        var artistQuery = context.BaseItems.Where(w => query.ArtistIds.Contains(w.Id));

        if (query.ArtistIds.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.Type <= 1 && artistQuery.Any(w => w.CleanName == f.CleanValue)));
        }

        if (query.AlbumArtistIds.Length > 0)
        {
            baseQuery = baseQuery
               .Where(e => e.ItemValues!.Any(f => f.Type == 1 && artistQuery.Any(w => w.CleanName == f.CleanValue)));
        }

        if (query.ContributingArtistIds.Length > 0)
        {
            var contributingArtists = context.BaseItems.Where(e => query.ContributingArtistIds.Contains(e.Id));
            baseQuery = baseQuery.Where(e => e.ItemValues!.Any(f => f.Type == 0 && contributingArtists.Any(w => w.CleanName == f.CleanValue)));
        }

        if (query.AlbumIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => context.BaseItems.Where(e => query.AlbumIds.Contains(e.Id)).Any(f => f.Name == e.Album));
        }

        if (query.ExcludeArtistIds.Length > 0)
        {
            var excludeArtistQuery = context.BaseItems.Where(w => query.ExcludeArtistIds.Contains(w.Id));
            baseQuery = baseQuery
                   .Where(e => !e.ItemValues!.Any(f => f.Type <= 1 && artistQuery.Any(w => w.CleanName == f.CleanValue)));
        }

        if (query.GenreIds.Count > 0)
        {
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.Type == 2 && context.BaseItems.Where(w => query.GenreIds.Contains(w.Id)).Any(w => w.CleanName == f.CleanValue)));
        }

        if (query.Genres.Count > 0)
        {
            var cleanGenres = query.Genres.Select(e => GetCleanValue(e)).ToArray();
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.Type == 2 && cleanGenres.Contains(f.CleanValue)));
        }

        if (tags.Count > 0)
        {
            var cleanValues = tags.Select(e => GetCleanValue(e)).ToArray();
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.Type == 4 && cleanValues.Contains(f.CleanValue)));
        }

        if (excludeTags.Count > 0)
        {
            var cleanValues = excludeTags.Select(e => GetCleanValue(e)).ToArray();
            baseQuery = baseQuery
                   .Where(e => !e.ItemValues!.Any(f => f.Type == 4 && cleanValues.Contains(f.CleanValue)));
        }

        if (query.StudioIds.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => e.ItemValues!.Any(f => f.Type == 3 && context.BaseItems.Where(w => query.StudioIds.Contains(w.Id)).Any(w => w.CleanName == f.CleanValue)));
        }

        if (query.OfficialRatings.Length > 0)
        {
            baseQuery = baseQuery
                   .Where(e => query.OfficialRatings.Contains(e.OfficialRating));
        }

        if (query.HasParentalRating ?? false)
        {
            if (query.MinParentalRating.HasValue)
            {
                baseQuery = baseQuery
                   .Where(e => e.InheritedParentalRatingValue >= query.MinParentalRating.Value);
            }

            if (query.MaxParentalRating.HasValue)
            {
                baseQuery = baseQuery
                   .Where(e => e.InheritedParentalRatingValue < query.MaxParentalRating.Value);
            }
        }
        else if (query.BlockUnratedItems.Length > 0)
        {
            if (query.MinParentalRating.HasValue)
            {
                if (query.MaxParentalRating.HasValue)
                {
                    baseQuery = baseQuery
                        .Where(e => (e.InheritedParentalRatingValue == null && !query.BlockUnratedItems.Select(e => e.ToString()).Contains(e.UnratedType))
                        || (e.InheritedParentalRatingValue >= query.MinParentalRating && e.InheritedParentalRatingValue <= query.MaxParentalRating));
                }
                else
                {
                    baseQuery = baseQuery
                        .Where(e => (e.InheritedParentalRatingValue == null && !query.BlockUnratedItems.Select(e => e.ToString()).Contains(e.UnratedType))
                        || e.InheritedParentalRatingValue >= query.MinParentalRating);
                }
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.InheritedParentalRatingValue != null && !query.BlockUnratedItems.Select(e => e.ToString()).Contains(e.UnratedType));
            }
        }
        else if (query.MinParentalRating.HasValue)
        {
            if (query.MaxParentalRating.HasValue)
            {
                baseQuery = baseQuery
                    .Where(e => e.InheritedParentalRatingValue != null && e.InheritedParentalRatingValue >= query.MinParentalRating.Value && e.InheritedParentalRatingValue <= query.MaxParentalRating.Value);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.InheritedParentalRatingValue != null && e.InheritedParentalRatingValue >= query.MinParentalRating.Value);
            }
        }
        else if (query.MaxParentalRating.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.InheritedParentalRatingValue != null && e.InheritedParentalRatingValue >= query.MaxParentalRating.Value);
        }
        else if (!query.HasParentalRating ?? false)
        {
            baseQuery = baseQuery
                .Where(e => e.InheritedParentalRatingValue == null);
        }

        if (query.HasOfficialRating.HasValue)
        {
            if (query.HasOfficialRating.Value)
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

        if (query.HasOverview.HasValue)
        {
            if (query.HasOverview.Value)
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

        if (query.HasOwnerId.HasValue)
        {
            if (query.HasOwnerId.Value)
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

        if (!string.IsNullOrWhiteSpace(query.HasNoAudioTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(e => e.StreamType == "Audio" && e.Language == query.HasNoAudioTrackWithLanguage));
        }

        if (!string.IsNullOrWhiteSpace(query.HasNoInternalSubtitleTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(e => e.StreamType == "Subtitle" && !e.IsExternal && e.Language == query.HasNoInternalSubtitleTrackWithLanguage));
        }

        if (!string.IsNullOrWhiteSpace(query.HasNoExternalSubtitleTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(e => e.StreamType == "Subtitle" && e.IsExternal && e.Language == query.HasNoExternalSubtitleTrackWithLanguage));
        }

        if (!string.IsNullOrWhiteSpace(query.HasNoSubtitleTrackWithLanguage))
        {
            baseQuery = baseQuery
                .Where(e => !e.MediaStreams!.Any(e => e.StreamType == "Subtitle" && e.Language == query.HasNoSubtitleTrackWithLanguage));
        }

        if (query.HasSubtitles.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.MediaStreams!.Any(e => e.StreamType == "Subtitle") == query.HasSubtitles.Value);
        }

        if (query.HasChapterImages.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.Chapters!.Any(e => e.ImagePath != null) == query.HasChapterImages.Value);
        }

        if (query.HasDeadParentId.HasValue && query.HasDeadParentId.Value)
        {
            baseQuery = baseQuery
                .Where(e => e.ParentId.HasValue && context.BaseItems.Any(f => f.Id.Equals(e.ParentId.Value)));
        }

        if (query.IsDeadArtist.HasValue && query.IsDeadArtist.Value)
        {
            baseQuery = baseQuery
                .Where(e => e.ItemValues!.Any(f => (f.Type == 0 || f.Type == 1) && f.CleanValue == e.CleanName));
        }

        if (query.IsDeadStudio.HasValue && query.IsDeadStudio.Value)
        {
            baseQuery = baseQuery
                .Where(e => e.ItemValues!.Any(f => f.Type == 3 && f.CleanValue == e.CleanName));
        }

        if (query.IsDeadPerson.HasValue && query.IsDeadPerson.Value)
        {
            baseQuery = baseQuery
                .Where(e => !e.Peoples!.Any(f => f.Name == e.Name));
        }

        if (query.Years.Length == 1)
        {
            baseQuery = baseQuery
                .Where(e => e.ProductionYear == query.Years[0]);
        }
        else if (query.Years.Length > 1)
        {
            baseQuery = baseQuery
                .Where(e => query.Years.Any(f => f == e.ProductionYear));
        }

        var isVirtualItem = query.IsVirtualItem ?? query.IsMissing;
        if (isVirtualItem.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.IsVirtualItem == isVirtualItem.Value);
        }

        if (query.IsSpecialSeason.HasValue)
        {
            if (query.IsSpecialSeason.Value)
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

        if (query.IsUnaired.HasValue)
        {
            if (query.IsUnaired.Value)
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

        if (query.MediaTypes.Length == 1)
        {
            baseQuery = baseQuery
                .Where(e => e.MediaType == query.MediaTypes[0].ToString());
        }
        else if (query.MediaTypes.Length > 1)
        {
            baseQuery = baseQuery
                .Where(e => query.MediaTypes.Select(f => f.ToString()).Contains(e.MediaType));
        }

        if (query.ItemIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => query.ItemIds.Contains(e.Id));
        }

        if (query.ExcludeItemIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => !query.ItemIds.Contains(e.Id));
        }

        if (query.ExcludeProviderIds is not null && query.ExcludeProviderIds.Count > 0)
        {
            baseQuery = baseQuery.Where(e => !e.Provider!.All(f => !query.ExcludeProviderIds.All(w => f.ProviderId == w.Key && f.ProviderValue == w.Value)));
        }

        if (query.HasAnyProviderId is not null && query.HasAnyProviderId.Count > 0)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => !query.HasAnyProviderId.Any(w => f.ProviderId == w.Key && f.ProviderValue == w.Value)));
        }

        if (query.HasImdbId.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId == "imdb"));
        }

        if (query.HasTmdbId.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId == "tmdb"));
        }

        if (query.HasTvdbId.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId == "tvdb"));
        }

        var queryTopParentIds = query.TopParentIds;

        if (queryTopParentIds.Length > 0)
        {
            var includedItemByNameTypes = GetItemByNameTypesInQuery(query);
            var enableItemsByName = (query.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0;
            if (enableItemsByName && includedItemByNameTypes.Count > 0)
            {
                baseQuery = baseQuery.Where(e => includedItemByNameTypes.Contains(e.Type) || queryTopParentIds.Any(w => w.Equals(e.TopParentId!.Value)));
            }
            else
            {
                baseQuery = baseQuery.Where(e => queryTopParentIds.Any(w => w.Equals(e.TopParentId!.Value)));
            }
        }

        if (query.AncestorIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => e.AncestorIds!.Any(f => query.AncestorIds.Contains(f.Id)));
        }

        if (!string.IsNullOrWhiteSpace(query.AncestorWithPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => context.BaseItems.Where(f => f.PresentationUniqueKey == query.AncestorWithPresentationUniqueKey).Any(f => f.AncestorIds!.Any(w => w.ItemId.Equals(f.Id))));
        }

        if (!string.IsNullOrWhiteSpace(query.SeriesPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => e.SeriesPresentationUniqueKey == query.SeriesPresentationUniqueKey);
        }

        if (query.ExcludeInheritedTags.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => !e.ItemValues!.Where(e => e.Type == 6)
                    .Any(f => query.ExcludeInheritedTags.Contains(f.CleanValue)));
        }

        if (query.IncludeInheritedTags.Length > 0)
        {
            // Episodes do not store inherit tags from their parents in the database, and the tag may be still required by the client.
            // In addtion to the tags for the episodes themselves, we need to manually query its parent (the season)'s tags as well.
            if (includeTypes.Length == 1 && includeTypes.FirstOrDefault() is BaseItemKind.Episode)
            {
                baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Where(e => e.Type == 6)
                        .Any(f => query.IncludeInheritedTags.Contains(f.CleanValue))
                        ||
                        (e.ParentId.HasValue && context.ItemValues.Where(w => w.ItemId.Equals(e.ParentId.Value))!.Where(e => e.Type == 6)
                        .Any(f => query.IncludeInheritedTags.Contains(f.CleanValue))));
            }

            // A playlist should be accessible to its owner regardless of allowed tags.
            else if (includeTypes.Length == 1 && includeTypes.FirstOrDefault() is BaseItemKind.Playlist)
            {
                baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Where(e => e.Type == 6)
                        .Any(f => query.IncludeInheritedTags.Contains(f.CleanValue)) || e.Data!.Contains($"OwnerUserId\":\"{query.User!.Id:N}\""));
                // d                                                                      ^^ this is stupid it hate this.
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.ItemValues!.Where(e => e.Type == 6)
                        .Any(f => query.IncludeInheritedTags.Contains(f.CleanValue)));
            }
        }

        if (query.SeriesStatuses.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => query.SeriesStatuses.Any(f => e.Data!.Contains(f.ToString(), StringComparison.InvariantCultureIgnoreCase)));
        }

        if (query.BoxSetLibraryFolders.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => query.BoxSetLibraryFolders.Any(f => e.Data!.Contains(f.ToString("N", CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)));
        }

        if (query.VideoTypes.Length > 0)
        {
            var videoTypeBs = query.VideoTypes.Select(e => $"\"VideoType\":\"" + e + "\"");
            baseQuery = baseQuery
                .Where(e => videoTypeBs.Any(f => e.Data!.Contains(f, StringComparison.InvariantCultureIgnoreCase)));
        }

        if (query.Is3D.HasValue)
        {
            if (query.Is3D.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.Data!.Contains("Video3DFormat", StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => !e.Data!.Contains("Video3DFormat", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        if (query.IsPlaceHolder.HasValue)
        {
            if (query.IsPlaceHolder.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.Data!.Contains("IsPlaceHolder\":true", StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => !e.Data!.Contains("IsPlaceHolder\":true", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        if (query.HasSpecialFeature.HasValue)
        {
            if (query.HasSpecialFeature.Value)
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

        if (query.HasTrailer.HasValue || query.HasThemeSong.HasValue || query.HasThemeVideo.HasValue)
        {
            if (query.HasTrailer.GetValueOrDefault() || query.HasThemeSong.GetValueOrDefault() || query.HasThemeVideo.GetValueOrDefault())
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

    /// <summary>
    /// Gets the type.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <returns>Type.</returns>
    /// <exception cref="ArgumentNullException"><c>typeName</c> is null.</exception>
    private static Type? GetType(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return _typeMap.GetOrAdd(typeName, k => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(k))
            .FirstOrDefault(t => t is not null));
    }

    /// <inheritdoc cref="IItemRepository" />
    public void SaveImages(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var images = SerializeImages(item.ImageInfos);
        using var db = _dbProvider.CreateDbContext();

        db.BaseItems
            .Where(e => e.Id.Equals(item.Id))
            .ExecuteUpdate(e => e.SetProperty(f => f.Images, images));
    }

    /// <inheritdoc cref="IItemRepository" />
    public void SaveItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        UpdateOrInsertItems(items, cancellationToken);
    }

    /// <inheritdoc cref="IItemRepository" />
    public void UpdateOrInsertItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var itemsLen = items.Count;
        var tuples = new (BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, string? UserDataKey, List<string> InheritedTags)[itemsLen];
        for (int i = 0; i < itemsLen; i++)
        {
            var item = items[i];
            var ancestorIds = item.SupportsAncestors ?
                item.GetAncestorIds().Distinct().ToList() :
                null;

            var topParent = item.GetTopParent();

            var userdataKey = item.GetUserDataKeys().FirstOrDefault();
            var inheritedTags = item.GetInheritedTags();

            tuples[i] = (item, ancestorIds, topParent, userdataKey, inheritedTags);
        }

        using var context = _dbProvider.CreateDbContext();
        foreach (var item in tuples)
        {
            var entity = Map(item.Item);
            context.BaseItems.Add(entity);

            if (item.Item.SupportsAncestors && item.AncestorIds != null)
            {
                foreach (var ancestorId in item.AncestorIds)
                {
                    context.AncestorIds.Add(new Data.Entities.AncestorId()
                    {
                        Item = entity,
                        AncestorIdText = ancestorId.ToString(),
                        Id = ancestorId
                    });
                }
            }

            var itemValues = GetItemValuesToSave(item.Item, item.InheritedTags);
            context.ItemValues.Where(e => e.ItemId.Equals(entity.Id)).ExecuteDelete();
            foreach (var itemValue in itemValues)
            {
                context.ItemValues.Add(new()
                {
                    Item = entity,
                    Type = itemValue.MagicNumber,
                    Value = itemValue.Value,
                    CleanValue = GetCleanValue(itemValue.Value)
                });
            }
        }

        context.SaveChanges(true);
    }

    /// <inheritdoc cref="IItemRepository" />
    public BaseItemDto? RetrieveItem(Guid id)
    {
        if (id.IsEmpty())
        {
            throw new ArgumentException("Guid can't be empty", nameof(id));
        }

        using var context = _dbProvider.CreateDbContext();
        var item = context.BaseItems.FirstOrDefault(e => e.Id.Equals(id));
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
    /// <returns>The dto to map.</returns>
    public BaseItemDto Map(BaseItemEntity entity, BaseItemDto dto)
    {
        dto.Id = entity.Id;
        dto.ParentId = entity.ParentId.GetValueOrDefault();
        dto.Path = entity.Path;
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
        dto.Genres = entity.Genres?.Split('|');
        dto.DateCreated = entity.DateCreated.GetValueOrDefault();
        dto.DateModified = entity.DateModified.GetValueOrDefault();
        dto.ChannelId = string.IsNullOrWhiteSpace(entity.ChannelId) ? Guid.Empty : Guid.Parse(entity.ChannelId);
        dto.DateLastRefreshed = entity.DateLastRefreshed.GetValueOrDefault();
        dto.DateLastSaved = entity.DateLastSaved.GetValueOrDefault();
        dto.OwnerId = string.IsNullOrWhiteSpace(entity.OwnerId) ? Guid.Empty : Guid.Parse(entity.OwnerId);
        dto.Width = entity.Width.GetValueOrDefault();
        dto.Height = entity.Height.GetValueOrDefault();
        if (entity.Provider is not null)
        {
            dto.ProviderIds = entity.Provider.ToDictionary(e => e.ProviderId, e => e.ProviderValue);
        }

        if (entity.ExtraType is not null)
        {
            dto.ExtraType = Enum.Parse<ExtraType>(entity.ExtraType);
        }

        if (entity.LockedFields is not null)
        {
            List<MetadataField>? fields = null;
            foreach (var i in entity.LockedFields.AsSpan().Split('|'))
            {
                if (Enum.TryParse(i, true, out MetadataField parsedValue))
                {
                    (fields ??= new List<MetadataField>()).Add(parsedValue);
                }
            }

            dto.LockedFields = fields?.ToArray() ?? Array.Empty<MetadataField>();
        }

        if (entity.Audio is not null)
        {
            dto.Audio = Enum.Parse<ProgramAudio>(entity.Audio);
        }

        dto.ExtraIds = entity.ExtraIds?.Split('|').Select(e => Guid.Parse(e)).ToArray();
        dto.ProductionLocations = entity.ProductionLocations?.Split('|');
        dto.Studios = entity.Studios?.Split('|');
        dto.Tags = entity.Tags?.Split('|');

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
            List<TrailerType>? types = null;
            foreach (var i in entity.TrailerTypes.AsSpan().Split('|'))
            {
                if (Enum.TryParse(i, true, out TrailerType parsedValue))
                {
                    (types ??= new List<TrailerType>()).Add(parsedValue);
                }
            }

            trailer.TrailerTypes = types?.ToArray() ?? Array.Empty<TrailerType>();
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
            hasArtists.Artists = entity.Artists?.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            hasAlbumArtists.AlbumArtists = entity.AlbumArtists?.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }

        if (dto is LiveTvProgram program)
        {
            program.ShowId = entity.ShowId;
        }

        if (entity.Images is not null)
        {
            dto.ImageInfos = DeserializeImages(entity.Images);
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        // dto.MediaType = entity.MediaType;
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
        var entity = new BaseItemEntity()
        {
            Type = dto.GetType().ToString(),
        };
        entity.Id = dto.Id;
        entity.ParentId = dto.ParentId;
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
        entity.Provider = dto.ProviderIds.Select(e => new Data.Entities.BaseItemProvider()
        {
            Item = entity,
            ProviderId = e.Key,
            ProviderValue = e.Value
        }).ToList();

        entity.Audio = dto.Audio?.ToString();
        entity.ExtraType = dto.ExtraType?.ToString();

        entity.ExtraIds = string.Join('|', dto.ExtraIds);
        entity.ProductionLocations = string.Join('|', dto.ProductionLocations);
        entity.Studios = dto.Studios is not null ? string.Join('|', dto.Studios) : null;
        entity.Tags = dto.Tags is not null ? string.Join('|', dto.Tags) : null;
        entity.LockedFields = dto.LockedFields is not null ? string.Join('|', dto.LockedFields) : null;

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

        if (dto is Trailer trailer)
        {
            entity.LockedFields = trailer.LockedFields is not null ? string.Join('|', trailer.LockedFields) : null;
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
            entity.Images = SerializeImages(dto.ImageInfos);
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        // dto.MediaType = entity.MediaType;
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

    private BaseItemDto DeserialiseBaseItem(BaseItemEntity baseItemEntity)
    {
        var type = GetType(baseItemEntity.Type) ?? throw new InvalidOperationException("Cannot deserialise unkown type.");
        var dto = Activator.CreateInstance(type) as BaseItemDto ?? throw new InvalidOperationException("Cannot deserialise unkown type.");
        return Map(baseItemEntity, dto);
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

    private List<(int MagicNumber, string Value)> GetItemValuesToSave(BaseItem item, List<string> inheritedTags)
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

    internal static string? SerializeProviderIds(Dictionary<string, string> providerIds)
    {
        StringBuilder str = new StringBuilder();
        foreach (var i in providerIds)
        {
            // Ideally we shouldn't need this IsNullOrWhiteSpace check,
            // but we're seeing some cases of bad data slip through
            if (string.IsNullOrWhiteSpace(i.Value))
            {
                continue;
            }

            str.Append(i.Key)
                .Append('=')
                .Append(i.Value)
                .Append('|');
        }

        if (str.Length == 0)
        {
            return null;
        }

        str.Length -= 1; // Remove last |
        return str.ToString();
    }

    internal static void DeserializeProviderIds(string value, IHasProviderIds item)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var part in value.SpanSplit('|'))
        {
            var providerDelimiterIndex = part.IndexOf('=');
            // Don't let empty values through
            if (providerDelimiterIndex != -1 && part.Length != providerDelimiterIndex + 1)
            {
                item.SetProviderId(part[..providerDelimiterIndex].ToString(), part[(providerDelimiterIndex + 1)..].ToString());
            }
        }
    }

    internal string? SerializeImages(ItemImageInfo[] images)
    {
        if (images.Length == 0)
        {
            return null;
        }

        StringBuilder str = new StringBuilder();
        foreach (var i in images)
        {
            if (string.IsNullOrWhiteSpace(i.Path))
            {
                continue;
            }

            AppendItemImageInfo(str, i);
            str.Append('|');
        }

        str.Length -= 1; // Remove last |
        return str.ToString();
    }

    internal ItemImageInfo[] DeserializeImages(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<ItemImageInfo>();
        }

        // TODO The following is an ugly performance optimization, but it's extremely unlikely that the data in the database would be malformed
        var valueSpan = value.AsSpan();
        var count = valueSpan.Count('|') + 1;

        var position = 0;
        var result = new ItemImageInfo[count];
        foreach (var part in valueSpan.Split('|'))
        {
            var image = ItemImageInfoFromValueString(part);

            if (image is not null)
            {
                result[position++] = image;
            }
        }

        if (position == count)
        {
            return result;
        }

        if (position == 0)
        {
            return Array.Empty<ItemImageInfo>();
        }

        // Extremely unlikely, but somehow one or more of the image strings were malformed. Cut the array.
        return result[..position];
    }

    private void AppendItemImageInfo(StringBuilder bldr, ItemImageInfo image)
    {
        const char Delimiter = '*';

        var path = image.Path ?? string.Empty;

        bldr.Append(GetPathToSave(path))
            .Append(Delimiter)
            .Append(image.DateModified.Ticks)
            .Append(Delimiter)
            .Append(image.Type)
            .Append(Delimiter)
            .Append(image.Width)
            .Append(Delimiter)
            .Append(image.Height);

        var hash = image.BlurHash;
        if (!string.IsNullOrEmpty(hash))
        {
            bldr.Append(Delimiter)
                // Replace delimiters with other characters.
                // This can be removed when we migrate to a proper DB.
                .Append(hash.Replace(Delimiter, '/').Replace('|', '\\'));
        }
    }

    private string? GetPathToSave(string path)
    {
        if (path is null)
        {
            return null;
        }

        return _appHost.ReverseVirtualPath(path);
    }

    private string RestorePath(string path)
    {
        return _appHost.ExpandVirtualPath(path);
    }

    internal ItemImageInfo? ItemImageInfoFromValueString(ReadOnlySpan<char> value)
    {
        const char Delimiter = '*';

        var nextSegment = value.IndexOf(Delimiter);
        if (nextSegment == -1)
        {
            return null;
        }

        ReadOnlySpan<char> path = value[..nextSegment];
        value = value[(nextSegment + 1)..];
        nextSegment = value.IndexOf(Delimiter);
        if (nextSegment == -1)
        {
            return null;
        }

        ReadOnlySpan<char> dateModified = value[..nextSegment];
        value = value[(nextSegment + 1)..];
        nextSegment = value.IndexOf(Delimiter);
        if (nextSegment == -1)
        {
            nextSegment = value.Length;
        }

        ReadOnlySpan<char> imageType = value[..nextSegment];

        var image = new ItemImageInfo
        {
            Path = RestorePath(path.ToString())
        };

        if (long.TryParse(dateModified, CultureInfo.InvariantCulture, out var ticks)
            && ticks >= DateTime.MinValue.Ticks
            && ticks <= DateTime.MaxValue.Ticks)
        {
            image.DateModified = new DateTime(ticks, DateTimeKind.Utc);
        }
        else
        {
            return null;
        }

        if (Enum.TryParse(imageType, true, out ImageType type))
        {
            image.Type = type;
        }
        else
        {
            return null;
        }

        // Optional parameters: width*height*blurhash
        if (nextSegment + 1 < value.Length - 1)
        {
            value = value[(nextSegment + 1)..];
            nextSegment = value.IndexOf(Delimiter);
            if (nextSegment == -1 || nextSegment == value.Length)
            {
                return image;
            }

            ReadOnlySpan<char> widthSpan = value[..nextSegment];

            value = value[(nextSegment + 1)..];
            nextSegment = value.IndexOf(Delimiter);
            if (nextSegment == -1)
            {
                nextSegment = value.Length;
            }

            ReadOnlySpan<char> heightSpan = value[..nextSegment];

            if (int.TryParse(widthSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
                && int.TryParse(heightSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            {
                image.Width = width;
                image.Height = height;
            }

            if (nextSegment < value.Length - 1)
            {
                value = value[(nextSegment + 1)..];
                var length = value.Length;

                Span<char> blurHashSpan = stackalloc char[length];
                for (int i = 0; i < length; i++)
                {
                    var c = value[i];
                    blurHashSpan[i] = c switch
                    {
                        '/' => Delimiter,
                        '\\' => '|',
                        _ => c
                    };
                }

                image.BlurHash = new string(blurHashSpan);
            }
        }

        return image;
    }

    private List<string> GetItemByNameTypesInQuery(InternalItemsQuery query)
    {
        var list = new List<string>();

        if (IsTypeInQuery(BaseItemKind.Person, query))
        {
            list.Add(typeof(Person).FullName!);
        }

        if (IsTypeInQuery(BaseItemKind.Genre, query))
        {
            list.Add(typeof(Genre).FullName!);
        }

        if (IsTypeInQuery(BaseItemKind.MusicGenre, query))
        {
            list.Add(typeof(MusicGenre).FullName!);
        }

        if (IsTypeInQuery(BaseItemKind.MusicArtist, query))
        {
            list.Add(typeof(MusicArtist).FullName!);
        }

        if (IsTypeInQuery(BaseItemKind.Studio, query))
        {
            list.Add(typeof(Studio).FullName!);
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

    private IQueryable<T> Pageinate<T>(IQueryable<T> query, InternalItemsQuery filter)
    {
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

        return query;
    }

    private Expression<Func<BaseItemEntity, object>> MapOrderByField(ItemSortBy sortBy, InternalItemsQuery query)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return sortBy switch
        {
            ItemSortBy.AirTime => e => e.SortName, // TODO
            ItemSortBy.Runtime => e => e.RunTimeTicks,
            ItemSortBy.Random => e => EF.Functions.Random(),
            ItemSortBy.DatePlayed => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.LastPlayedDate,
            ItemSortBy.PlayCount => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.PlayCount,
            ItemSortBy.IsFavoriteOrLiked => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.IsFavorite,
            ItemSortBy.IsFolder => e => e.IsFolder,
            ItemSortBy.IsPlayed => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.Played,
            ItemSortBy.IsUnplayed => e => !e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id) && f.Key == e.UserDataKey)!.Played,
            ItemSortBy.DateLastContentAdded => e => e.DateLastMediaAdded,
            ItemSortBy.Artist => e => e.ItemValues!.Where(f => f.Type == 0).Select(f => f.CleanValue),
            ItemSortBy.AlbumArtist => e => e.ItemValues!.Where(f => f.Type == 1).Select(f => f.CleanValue),
            ItemSortBy.Studio => e => e.ItemValues!.Where(f => f.Type == 3).Select(f => f.CleanValue),
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
        bool hasSearch = !string.IsNullOrEmpty(filter.SearchTerm);

        if (hasSearch)
        {
            List<(ItemSortBy, SortOrder)> prepend = new List<(ItemSortBy, SortOrder)>(4);
            if (hasSearch)
            {
                prepend.Add((ItemSortBy.SortName, SortOrder.Ascending));
            }

            orderBy = filter.OrderBy = [.. prepend, .. orderBy];
        }
        else if (orderBy.Count == 0)
        {
            return query;
        }

        foreach (var item in orderBy)
        {
            var expression = MapOrderByField(item.OrderBy, filter);
            if (item.SortOrder == SortOrder.Ascending)
            {
                query = query.OrderBy(expression);
            }
            else
            {
                query = query.OrderByDescending(expression);
            }
        }

        return query;
    }
}
