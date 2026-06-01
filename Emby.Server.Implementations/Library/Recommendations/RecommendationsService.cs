using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Recommendations;

/// <summary>
/// Orchestrates the taste-profile-based recommendation engine.
/// </summary>
public sealed class RecommendationsService : IRecommendationsService, IDisposable
{
    private const int HistoryWatchedCap = 500;
    private const int HistoryFavoriteCap = 250;

    private const int MaxCategoryLimit = 20;
    private const int MaxItemLimit = 100;
    private const int DefaultRankedLimit = 50;
    private const int MaxRankedLimit = 200;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;
    private readonly IPeopleRepository _peopleRepository;
    private readonly IDtoService _dtoService;
    private readonly IUserManager _userManager;
    private readonly ILogger<RecommendationsService> _logger;

    private readonly ConcurrentDictionary<ProfileKey, Lazy<Task<TasteProfile>>> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userDataManager">The user data manager.</param>
    /// <param name="peopleRepository">The people repository.</param>
    /// <param name="dtoService">The DTO service.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="logger">The logger.</param>
    public RecommendationsService(
        ILibraryManager libraryManager,
        IUserDataManager userDataManager,
        IPeopleRepository peopleRepository,
        IDtoService dtoService,
        IUserManager userManager,
        ILogger<RecommendationsService> logger)
    {
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
        _peopleRepository = peopleRepository;
        _dtoService = dtoService;
        _userManager = userManager;
        _logger = logger;

        _userDataManager.UserDataSaved += OnUserDataSaved;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _userDataManager.UserDataSaved -= OnUserDataSaved;
    }

    /// <summary>
    /// Returns true when the requested types resolve to exactly one recommendable kind (Movie or Series).
    /// </summary>
    /// <param name="requestedTypes">The requested item kinds.</param>
    /// <param name="requestedMediaTypes">The requested media types.</param>
    /// <param name="kind">When successful, the resolved recommendable kind.</param>
    /// <returns>True if the requested types resolve to exactly one recommendable kind; otherwise, false.</returns>
    public static bool TryGetRecommendableKind(
        IReadOnlyList<BaseItemKind> requestedTypes,
        IReadOnlyList<MediaType> requestedMediaTypes,
        out BaseItemKind kind)
        => RecommendableKindResolver.TryGetRecommendableKind(requestedTypes, requestedMediaTypes, out kind);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetOrBuildProfileAsync(request.UserId, request.Kind, request.ParentId).ConfigureAwait(false);
        if (profile.TotalSignalMass <= 0)
        {
            return Array.Empty<RecommendationDto>();
        }

        var user = _userManager.GetUserById(request.UserId);
        if (user is null)
        {
            return Array.Empty<RecommendationDto>();
        }

        var parentIdGuid = request.ParentId ?? Guid.Empty;
        var dtoOptions = request.DtoOptions;
        var categoryLimit = Math.Clamp(request.CategoryLimit, 0, MaxCategoryLimit);
        var itemLimit = Math.Clamp(request.ItemLimit, 1, MaxItemLimit);

        var recentlyPlayed = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { request.Kind },
            IsPlayed = true,
            OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
            Limit = 6,
            ParentId = parentIdGuid,
            Recursive = true,
            DtoOptions = dtoOptions
        });

        var favoriteSeeds = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { request.Kind },
            IsFavoriteOrLiked = true,
            OrderBy = new[] { (ItemSortBy.Random, SortOrder.Descending) },
            Limit = 10,
            ExcludeItemIds = recentlyPlayed.Select(i => i.Id).ToArray(),
            ParentId = parentIdGuid,
            Recursive = true,
            DtoOptions = dtoOptions
        });

        var emittedIds = new HashSet<Guid>(recentlyPlayed.Select(i => i.Id));
        foreach (var f in favoriteSeeds)
        {
            emittedIds.Add(f.Id);
        }

        var categories = new List<RecommendationDto>();

        foreach (var seed in recentlyPlayed)
        {
            if (categories.Count >= categoryLimit)
            {
                break;
            }

            var cat = BuildSeedCategory(user, seed, profile, itemLimit, emittedIds, dtoOptions, RecommendationType.SimilarToRecentlyPlayed, parentIdGuid);
            if (cat is not null)
            {
                categories.Add(cat);
            }
        }

        foreach (var seed in favoriteSeeds)
        {
            if (categories.Count >= categoryLimit)
            {
                break;
            }

            var cat = BuildSeedCategory(user, seed, profile, itemLimit, emittedIds, dtoOptions, RecommendationType.SimilarToLikedItem, parentIdGuid);
            if (cat is not null)
            {
                categories.Add(cat);
            }
        }

        var directorNames = ExtractPeopleNames(recentlyPlayed, PersonKind.Director);
        foreach (var name in directorNames)
        {
            if (categories.Count >= categoryLimit)
            {
                break;
            }

            var cat = BuildPersonCategory(user, name, PersonKind.Director, profile, itemLimit, emittedIds, dtoOptions, RecommendationType.HasDirectorFromRecentlyPlayed, parentIdGuid);
            if (cat is not null)
            {
                categories.Add(cat);
            }
        }

        var actorNames = ExtractPeopleNames(recentlyPlayed, PersonKind.Actor);
        foreach (var name in actorNames)
        {
            if (categories.Count >= categoryLimit)
            {
                break;
            }

            var cat = BuildPersonCategory(user, name, PersonKind.Actor, profile, itemLimit, emittedIds, dtoOptions, RecommendationType.HasActorFromRecentlyPlayed, parentIdGuid);
            if (cat is not null)
            {
                categories.Add(cat);
            }
        }

        return categories.OrderBy(c => c.RecommendationType).ToList();
    }

    /// <inheritdoc/>
    public async Task<QueryResult<BaseItemDto>?> GetRankedItemsAsync(
        Guid userId,
        BaseItemKind kind,
        Guid? parentId,
        int? startIndex,
        int? limit,
        bool enableTotalRecordCount,
        DtoOptions dtoOptions,
        CancellationToken cancellationToken)
    {
        var profile = await GetOrBuildProfileAsync(userId, kind, parentId).ConfigureAwait(false);
        if (profile.TotalSignalMass <= 0)
        {
            return null;
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return null;
        }

        var safeLimit = Math.Clamp(limit ?? DefaultRankedLimit, 1, MaxRankedLimit);
        var safeStartIndex = Math.Max(0, startIndex ?? 0);

        var pool = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { kind },
            IsPlayed = false,
            ParentId = parentId ?? Guid.Empty,
            Recursive = true,
            IsVirtualItem = false,
            EnableGroupByMetadataKey = true,
            DtoOptions = dtoOptions,
            Limit = safeLimit * 6
        });

        if (pool.Count == 0)
        {
            return new QueryResult<BaseItemDto>(safeStartIndex, 0, Array.Empty<BaseItemDto>());
        }

        var peopleByCandidate = FetchPeopleByItem(pool);

        var ranked = pool
            .Select(c => (Item: c, Score: TasteProfileScorer.Score(
                profile,
                c,
                seedItem: null,
                peopleByCandidate.GetValueOrDefault(c.Id, Array.Empty<PersonInfo>()))))
            .OrderByDescending(t => t.Score)
            .Skip(safeStartIndex)
            .Take(safeLimit)
            .Select(t => t.Item)
            .ToList();

        return new QueryResult<BaseItemDto>(
            safeStartIndex,
            enableTotalRecordCount ? pool.Count : 0,
            _dtoService.GetBaseItemDtos(ranked, dtoOptions, user));
    }

    private async Task<TasteProfile> GetOrBuildProfileAsync(Guid userId, BaseItemKind kind, Guid? parentId)
    {
        var key = new ProfileKey(userId, kind, parentId ?? Guid.Empty);
        var lazy = _cache.GetOrAdd(key, k => new Lazy<Task<TasteProfile>>(
            () => BuildProfileAsync(k),
            LazyThreadSafetyMode.ExecutionAndPublication));
        var profile = await lazy.Value.ConfigureAwait(false);

        if (DateTime.UtcNow - profile.ComputedAt > CacheTtl)
        {
            _cache.TryRemove(new KeyValuePair<ProfileKey, Lazy<Task<TasteProfile>>>(key, lazy));
        }

        return profile;
    }

    private Task<TasteProfile> BuildProfileAsync(ProfileKey key)
    {
        try
        {
            var user = _userManager.GetUserById(key.UserId);
            if (user is null)
            {
                return Task.FromResult(TasteProfile.Empty(key.Kind));
            }

            var parentIdGuid = key.ParentId.Equals(Guid.Empty) ? (Guid?)null : key.ParentId;

            var watched = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { key.Kind },
                IsPlayed = true,
                ParentId = parentIdGuid ?? Guid.Empty,
                Recursive = true,
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                Limit = HistoryWatchedCap
            });

            var favorites = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { key.Kind },
                IsFavoriteOrLiked = true,
                ParentId = parentIdGuid ?? Guid.Empty,
                Recursive = true,
                Limit = HistoryFavoriteCap
            });

            var union = watched.Concat(favorites).GroupBy(i => i.Id).Select(g => g.First()).ToList();

            if (union.Count == 0)
            {
                return Task.FromResult(TasteProfile.Empty(key.Kind));
            }

            var peopleQuery = new InternalPeopleQuery
            {
                ItemIds = union.Select(i => i.Id).ToArray()
            };
            var peopleResult = _peopleRepository.GetPeople(peopleQuery);
            var peopleByItem = peopleResult.Items
                .GroupBy(p => p.ItemId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<PersonInfo>)g.ToList());

            var watchedSet = new HashSet<Guid>(watched.Select(i => i.Id));
            var favoriteSet = new HashSet<Guid>(favorites.Select(i => i.Id));

            var profile = TasteProfileBuilder.Build(
                key.Kind,
                union,
                isPlayed: i => watchedSet.Contains(i.Id),
                isFavorite: i => favoriteSet.Contains(i.Id) && _userDataManager.GetUserData(user, i)?.IsFavorite == true,
                isLiked: i => favoriteSet.Contains(i.Id) && (_userDataManager.GetUserData(user, i)?.Likes ?? false),
                peopleByItem: peopleByItem);

            return Task.FromResult(profile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build taste profile for user {UserId} kind {Kind}", key.UserId, key.Kind);

            return Task.FromResult(TasteProfile.Empty(key.Kind));
        }
    }

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        if (e.SaveReason is not (
            UserDataSaveReason.TogglePlayed
            or UserDataSaveReason.PlaybackFinished
            or UserDataSaveReason.UpdateUserRating
            or UserDataSaveReason.UpdateUserData
            or UserDataSaveReason.Import))
        {
            return;
        }

        if (e.Item is null)
        {
            return;
        }

        // Episodes and seasons roll up to the Series profile; a Movie maps to the Movie profile.
        // Without this mapping, playing an episode would never invalidate the Series profile.
        if (!TryGetProfileKind(e.Item.GetBaseItemKind(), out var affectedKind))
        {
            return;
        }

        foreach (var key in _cache.Keys)
        {
            if (key.UserId.Equals(e.UserId) && key.Kind == affectedKind)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Maps an item's kind to the recommendable profile kind it contributes to, so that
    /// activity on a child item (e.g. an Episode or Season) invalidates the owning Series
    /// profile rather than a non-existent per-episode profile.
    /// </summary>
    /// <param name="itemKind">The kind of the item that changed.</param>
    /// <param name="profileKind">When successful, the recommendable profile kind affected.</param>
    /// <returns>True if the item kind maps to a recommendable profile kind; otherwise, false.</returns>
    private static bool TryGetProfileKind(BaseItemKind itemKind, out BaseItemKind profileKind)
    {
        switch (itemKind)
        {
            case BaseItemKind.Movie:
                profileKind = BaseItemKind.Movie;
                return true;
            case BaseItemKind.Series:
            case BaseItemKind.Season:
            case BaseItemKind.Episode:
                profileKind = BaseItemKind.Series;
                return true;
            default:
                profileKind = default;
                return false;
        }
    }

    private RecommendationDto? BuildSeedCategory(
        User user,
        BaseItem seed,
        TasteProfile profile,
        int itemLimit,
        HashSet<Guid> emittedIds,
        DtoOptions dtoOptions,
        RecommendationType type,
        Guid parentIdGuid)
    {
        var pool = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { profile.Kind },
            Genres = seed.Genres,
            Tags = seed.Tags,
            ExcludeItemIds = emittedIds.ToArray(),
            ParentId = parentIdGuid,
            Recursive = true,
            EnableGroupByMetadataKey = true,
            Limit = itemLimit * 4,
            DtoOptions = dtoOptions
        });

        if (pool.Count == 0)
        {
            return null;
        }

        var peopleByCandidate = FetchPeopleByItem(pool);

        var ranked = pool
            .Select(c => (Item: c, Score: TasteProfileScorer.Score(
                profile,
                c,
                seed,
                peopleByCandidate.GetValueOrDefault(c.Id, Array.Empty<PersonInfo>()))))
            .OrderByDescending(t => t.Score)
            .Take(itemLimit)
            .Select(t => t.Item)
            .ToList();

        if (ranked.Count < Math.Max(1, itemLimit / 2))
        {
            return null;
        }

        foreach (var r in ranked)
        {
            emittedIds.Add(r.Id);
        }

        return new RecommendationDto
        {
            BaselineItemName = seed.Name,
            CategoryId = seed.Id,
            RecommendationType = type,
            Items = _dtoService.GetBaseItemDtos(ranked, dtoOptions, user)
        };
    }

    private RecommendationDto? BuildPersonCategory(
        User user,
        string name,
        PersonKind personKind,
        TasteProfile profile,
        int itemLimit,
        HashSet<Guid> emittedIds,
        DtoOptions dtoOptions,
        RecommendationType type,
        Guid parentIdGuid)
    {
        var pool = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { profile.Kind },
            Person = name,
            PersonTypes = personKind == PersonKind.Director ? new[] { PersonType.Director } : Array.Empty<string>(),
            ExcludeItemIds = emittedIds.ToArray(),
            ParentId = parentIdGuid,
            Recursive = true,
            EnableGroupByMetadataKey = true,
            Limit = itemLimit * 4,
            DtoOptions = dtoOptions
        });

        if (pool.Count == 0)
        {
            return null;
        }

        var peopleByCandidate = FetchPeopleByItem(pool);

        var ranked = pool
            .Select(c => (Item: c, Score: TasteProfileScorer.Score(
                profile,
                c,
                seedItem: null,
                peopleByCandidate.GetValueOrDefault(c.Id, Array.Empty<PersonInfo>()))))
            .OrderByDescending(t => t.Score)
            .Take(itemLimit)
            .Select(t => t.Item)
            .ToList();

        if (ranked.Count < Math.Max(1, itemLimit / 2))
        {
            return null;
        }

        foreach (var r in ranked)
        {
            emittedIds.Add(r.Id);
        }

        return new RecommendationDto
        {
            BaselineItemName = name,
            CategoryId = name.GetMD5(),
            RecommendationType = type,
            Items = _dtoService.GetBaseItemDtos(ranked, dtoOptions, user)
        };
    }

    private Dictionary<Guid, IReadOnlyList<PersonInfo>> FetchPeopleByItem(IReadOnlyList<BaseItem> items)
    {
        if (items.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<PersonInfo>>();
        }

        var ids = items.Select(i => i.Id).ToArray();
        var result = _peopleRepository.GetPeople(new InternalPeopleQuery { ItemIds = ids });

        return result.Items
            .GroupBy(p => p.ItemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PersonInfo>)g.ToList());
    }

    private IReadOnlyList<string> ExtractPeopleNames(IReadOnlyList<BaseItem> seedItems, PersonKind kind)
    {
        if (seedItems.Count == 0)
        {
            return Array.Empty<string>();
        }

        var byItem = FetchPeopleByItem(seedItems);

        return byItem.Values
            .SelectMany(list => list.Where(p => p.Type == kind))
            .Select(p => p.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private readonly record struct ProfileKey(Guid UserId, BaseItemKind Kind, Guid ParentId);
}
