#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixHomeRowsService : ICustomNetflixHomeRowsService
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixWatchProgressService _watchProgressService;
    private readonly ICustomNetflixMyListService _myListService;
    private readonly ICustomNetflixRecommendationService _recommendationService;
    private readonly ICustomNetflixRankingService _rankingService;
    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly CustomNetflixCardDtoCache _cardDtoCache;

    public CustomNetflixHomeRowsService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixWatchProgressService watchProgressService,
        ICustomNetflixMyListService myListService,
        ICustomNetflixRecommendationService recommendationService,
        ICustomNetflixRankingService rankingService,
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache,
        IUserManager userManager,
        ILibraryManager libraryManager,
        CustomNetflixCardDtoCache cardDtoCache)
    {
        _profileService = profileService;
        _watchProgressService = watchProgressService;
        _myListService = myListService;
        _recommendationService = recommendationService;
        _rankingService = rankingService;
        _repository = repository;
        _cache = cache;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _cardDtoCache = cardDtoCache;
    }

    public async Task<CustomNetflixHomeResponseDto> GetHomeAsync(Guid jellyfinUserId, Guid profileId, int itemLimit, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = "error";
        try
        {
            var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
            var user = _userManager.GetUserById(jellyfinUserId);
            if (profile is null || user is null)
            {
                result = "not_found";
                return new CustomNetflixHomeResponseDto
                {
                    ProfileId = profileId,
                    GeneratedAt = DateTime.UtcNow,
                    Rows = Array.Empty<CustomNetflixHomeRowDto>()
                };
            }

            itemLimit = CustomNetflixHomeSnapshots.NormalizeLimit(itemLimit);
            var snapshotKey = CustomNetflixHomeSnapshots.SnapshotKey(itemLimit);
            var cachedHome = await GetCachedHomeAsync(profileId, snapshotKey, cancellationToken).ConfigureAwait(false);
            if (cachedHome is not null)
            {
                result = "cached";
                return cachedHome;
            }

            var response = await BuildHomeAsync(jellyfinUserId, profileId, user, itemLimit, cancellationToken).ConfigureAwait(false);
            await SaveHomeSnapshotAsync(profileId, snapshotKey, response, cancellationToken).ConfigureAwait(false);
            result = "built";
            return response;
        }
        finally
        {
            stopwatch.Stop();
            CustomNetflixMetrics.ObserveHomeRequest(result, stopwatch.Elapsed);
        }
    }

    private async Task<CustomNetflixHomeResponseDto> BuildHomeAsync(Guid jellyfinUserId, Guid profileId, User user, int itemLimit, CancellationToken cancellationToken)
    {
        var rows = new List<CustomNetflixHomeRowDto>();
        var continueWatching = await _watchProgressService.GetContinueWatchingAsync(jellyfinUserId, profileId, itemLimit, cancellationToken).ConfigureAwait(false);
        if (continueWatching.Count > 0)
        {
            rows.Add(new CustomNetflixHomeRowDto
            {
                Id = "continue-watching",
                Title = "Continuer \u00e0 regarder",
                TitleKey = "customnetflix.home.continue_watching",
                Items = continueWatching
                    .Select(item => new CustomNetflixHomeItemDto { Item = item.Item, Progress = item.Progress })
                    .ToArray()
            });
        }

        var myList = await _myListService.GetMyListAsync(jellyfinUserId, profileId, itemLimit, cancellationToken).ConfigureAwait(false);
        if (myList is { Items.Count: > 0 })
        {
            rows.Add(new CustomNetflixHomeRowDto
            {
                Id = "my-list",
                Title = "Ma liste",
                TitleKey = "customnetflix.home.my_list",
                Items = myList.Items
                    .Select(item => new CustomNetflixHomeItemDto { Item = item.Item, Progress = item.Progress })
                    .ToArray()
            });
        }

        var topTen = await _rankingService.GetTopTenAsync(jellyfinUserId, 10, cancellationToken).ConfigureAwait(false);
        if (topTen.Items.Count > 0)
        {
            rows.Add(new CustomNetflixHomeRowDto
            {
                Id = topTen.Id,
                Title = topTen.Title,
                TitleKey = topTen.TitleKey,
                Items = topTen.Items
                    .Select(item => new CustomNetflixHomeItemDto { Item = item.Item })
                    .ToArray()
            });
        }

        var trending = await _rankingService.GetTrendingAsync(jellyfinUserId, itemLimit, cancellationToken).ConfigureAwait(false);
        if (trending.Items.Count > 0)
        {
            rows.Add(new CustomNetflixHomeRowDto
            {
                Id = trending.Id,
                Title = trending.Title,
                TitleKey = trending.TitleKey,
                Items = trending.Items
                    .Select(item => new CustomNetflixHomeItemDto { Item = item.Item })
                    .ToArray()
            });
        }

        var latestQuery = new InternalItemsQuery(user)
        {
            Recursive = true,
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            Limit = itemLimit,
            OrderBy = [(ItemSortBy.DateCreated, SortOrder.Descending)]
        };
        var latestItems = _libraryManager.GetItemList(latestQuery);
        if (latestItems.Count > 0)
        {
            rows.Add(new CustomNetflixHomeRowDto
            {
                Id = "new",
                Title = "Nouveaut\u00e9s",
                TitleKey = "customnetflix.home.new_releases",
                Items = _cardDtoCache.GetBaseItemDtos(latestItems, user)
                    .Select(item => new CustomNetflixHomeItemDto { Item = item })
                    .ToArray()
            });
        }

        var recommendations = await _recommendationService.GetRecommendationsAsync(
            jellyfinUserId,
            profileId,
            itemLimit,
            cancellationToken).ConfigureAwait(false);
        if (recommendations is { Items.Count: > 0 })
        {
            rows.Add(new CustomNetflixHomeRowDto
            {
                Id = recommendations.Personalized ? "recommended-for-you" : "discover",
                Title = recommendations.Title,
                TitleKey = recommendations.TitleKey,
                Items = recommendations.Items
            });
        }

        AddNativeRow(
            rows,
            user,
            "popular-movies",
            "Films populaires",
            "customnetflix.home.popular_movies",
            new InternalItemsQuery(user)
            {
                Recursive = true,
                IsFolder = false,
                IncludeItemTypes = [BaseItemKind.Movie],
                Limit = itemLimit,
                MinCommunityRating = 1,
                OrderBy = [(ItemSortBy.CommunityRating, SortOrder.Descending), (ItemSortBy.PremiereDate, SortOrder.Descending)]
            });

        AddNativeRow(
            rows,
            user,
            "popular-series",
            "S\u00e9ries populaires",
            "customnetflix.home.popular_series",
            new InternalItemsQuery(user)
            {
                Recursive = true,
                IncludeItemTypes = [BaseItemKind.Series],
                Limit = itemLimit,
                MinCommunityRating = 1,
                OrderBy = [(ItemSortBy.CommunityRating, SortOrder.Descending), (ItemSortBy.DateLastContentAdded, SortOrder.Descending)]
            });

        await ApplyProfileProgressAsync(jellyfinUserId, profileId, rows, cancellationToken).ConfigureAwait(false);

        return new CustomNetflixHomeResponseDto
        {
            ProfileId = profileId,
            GeneratedAt = DateTime.UtcNow,
            Rows = rows
        };
    }

    private async Task<CustomNetflixHomeResponseDto?> GetCachedHomeAsync(Guid profileId, string snapshotKey, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var redisPayload = await _cache.GetStringAsync(RedisKeyBuilder.Home(profileId, snapshotKey), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(redisPayload))
        {
            var redisResponse = CustomNetflixHomeSnapshots.Deserialize(redisPayload, profileId, snapshotKey, utcNow);
            if (redisResponse is not null)
            {
                CustomNetflixMetrics.ObserveHomeCacheLookup("redis", "hit");
                return redisResponse;
            }

            CustomNetflixMetrics.ObserveHomeCacheLookup("redis", "invalid");
        }
        else
        {
            CustomNetflixMetrics.ObserveHomeCacheLookup("redis", _cache.IsEnabled ? "miss" : "disabled");
        }

        var snapshot = await _repository.GetHomeSnapshotAsync(profileId, snapshotKey, utcNow, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            CustomNetflixMetrics.ObserveHomeCacheLookup("postgresql", "miss");
            return null;
        }

        var response = CustomNetflixHomeSnapshots.Deserialize(snapshot.PayloadJson, profileId, snapshotKey, utcNow);
        if (response is null)
        {
            CustomNetflixMetrics.ObserveHomeCacheLookup("postgresql", "invalid");
            return null;
        }

        CustomNetflixMetrics.ObserveHomeCacheLookup("postgresql", "hit");
        await CacheHomePayloadAsync(profileId, snapshotKey, snapshot.PayloadJson, snapshot.ExpiresAt, cancellationToken).ConfigureAwait(false);
        return response;
    }

    private async Task SaveHomeSnapshotAsync(Guid profileId, string snapshotKey, CustomNetflixHomeResponseDto response, CancellationToken cancellationToken)
    {
        var generatedAt = response.GeneratedAt;
        var expiresAt = generatedAt.Add(CustomNetflixHomeSnapshots.SnapshotTtl);
        var payload = CustomNetflixHomeSnapshots.Serialize(profileId, snapshotKey, response, generatedAt, expiresAt);
        await _repository.SaveHomeSnapshotAsync(profileId, snapshotKey, payload, generatedAt, expiresAt, cancellationToken).ConfigureAwait(false);
        await CacheHomePayloadAsync(profileId, snapshotKey, payload, expiresAt, cancellationToken).ConfigureAwait(false);
    }

    private async Task CacheHomePayloadAsync(Guid profileId, string snapshotKey, string payload, DateTime expiresAt, CancellationToken cancellationToken)
    {
        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        await _cache.SetStringAsync(RedisKeyBuilder.Home(profileId, snapshotKey), payload, ttl, cancellationToken).ConfigureAwait(false);
    }

    private void AddNativeRow(
        List<CustomNetflixHomeRowDto> rows,
        User user,
        string id,
        string title,
        string titleKey,
        InternalItemsQuery query)
    {
        var items = _libraryManager.GetItemList(query);
        if (items.Count == 0)
        {
            return;
        }

        rows.Add(new CustomNetflixHomeRowDto
        {
            Id = id,
            Title = title,
            TitleKey = titleKey,
            Items = _cardDtoCache.GetBaseItemDtos(items, user)
                .Select(item => new CustomNetflixHomeItemDto { Item = item })
                .ToArray()
        });
    }

    private async Task ApplyProfileProgressAsync(
        Guid jellyfinUserId,
        Guid profileId,
        IReadOnlyList<CustomNetflixHomeRowDto> rows,
        CancellationToken cancellationToken)
    {
        var itemIds = rows
            .SelectMany(row => row.Items)
            .Select(item => item.Item.Id)
            .Where(itemId => !itemId.Equals(Guid.Empty))
            .Distinct()
            .ToArray();
        var progress = await _watchProgressService.GetProgressForItemsAsync(
            jellyfinUserId,
            profileId,
            itemIds,
            cancellationToken).ConfigureAwait(false);
        var progressByItemId = progress.ToDictionary(item => item.ItemId);

        foreach (var row in rows)
        {
            row.Items = row.Items
                .Select(item => new CustomNetflixHomeItemDto
                {
                    Item = item.Item,
                    Progress = progressByItemId.GetValueOrDefault(item.Item.Id),
                    RecommendationReason = item.RecommendationReason
                })
                .ToArray();
        }
    }
}
