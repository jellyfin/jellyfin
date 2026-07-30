#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixRecommendationService : ICustomNetflixRecommendationService
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly CustomNetflixCardDtoCache _cardDtoCache;

    public CustomNetflixRecommendationService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixRepository repository,
        IUserManager userManager,
        ILibraryManager libraryManager,
        CustomNetflixCardDtoCache cardDtoCache)
    {
        _profileService = profileService;
        _repository = repository;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _cardDtoCache = cardDtoCache;
    }

    public async Task<CustomNetflixRecommendationsResponseDto?> GetRecommendationsAsync(
        Guid jellyfinUserId,
        Guid profileId,
        int limit,
        CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return null;
        }

        limit = CustomNetflixRecommendationPolicy.NormalizeLimit(limit);
        var utcNow = DateTime.UtcNow;
        var history = await _repository.GetWatchHistoryAsync(
            profileId,
            CustomNetflixRecommendationPolicy.HistoryLimit,
            cancellationToken).ConfigureAwait(false);
        var likedFeedback = await _repository.GetLikedItemFeedbacksAsync(
            profileId,
            CustomNetflixFeedbackPolicy.RecommendationFeedbackLimit,
            cancellationToken).ConfigureAwait(false);
        var signals = BuildSignals(history, likedFeedback, user);
        var topGenres = CustomNetflixRecommendationPolicy.GetTopGenres(signals, utcNow);
        var candidates = GetCandidates(user, topGenres, CustomNetflixRecommendationPolicy.GetCandidatePoolSize(limit));
        var candidateModels = candidates
            .Select(item => new CustomNetflixRecommendationCandidate(
                item.Id,
                GetRecommendationType(item),
                NormalizeGenres(item.Genres),
                item.CommunityRating,
                item.PremiereDate,
                item.DateCreated))
            .ToArray();
        var candidateFeedback = await _repository.GetItemFeedbacksForItemsAsync(
            profileId,
            candidateModels.Select(candidate => candidate.ItemId).ToArray(),
            cancellationToken).ConfigureAwait(false);
        var excludedItemIds = candidateFeedback
            .Where(row => CustomNetflixFeedbackPolicy.IsNegative(row.Feedback))
            .Select(row => row.ItemId)
            .ToHashSet();
        var rankedIds = CustomNetflixRecommendationPolicy.RankCandidates(
            signals,
            candidateModels,
            utcNow,
            limit,
            excludedItemIds);
        var candidateById = candidates.ToDictionary(item => item.Id);
        var progressRows = await _repository.GetProgressForItemsAsync(profileId, rankedIds, cancellationToken).ConfigureAwait(false);
        var progressById = progressRows.ToDictionary(progress => progress.ItemId);
        var rankedItems = rankedIds
            .Where(candidateById.ContainsKey)
            .Select(itemId => candidateById[itemId])
            .ToArray();
        var itemDtos = _cardDtoCache.GetBaseItemDtos(rankedItems, user);
        var personalized = CustomNetflixRecommendationPolicy.HasPersonalizationSignals(signals);
        var reason = CustomNetflixRecommendationPolicy.GetStableReason(likedFeedback.Count > 0, personalized);
        var items = rankedItems
            .Select((item, index) => new CustomNetflixHomeItemDto
            {
                Item = itemDtos[index],
                Progress = progressById.TryGetValue(item.Id, out var progress)
                    ? CustomNetflixDtoMapper.MapProgress(progress)
                    : null,
                RecommendationReason = reason
            })
            .ToArray();

        return new CustomNetflixRecommendationsResponseDto
        {
            ProfileId = profileId,
            GeneratedAt = utcNow,
            Personalized = personalized,
            Title = personalized ? $"Pour {profile.Name}" : "\u00c0 d\u00e9couvrir",
            TitleKey = personalized
                ? "customnetflix.recommendations.for_profile"
                : "customnetflix.recommendations.discover",
            Items = items
        };
    }

    private IReadOnlyList<CustomNetflixRecommendationSignal> BuildSignals(
        IReadOnlyList<WatchHistoryRow> history,
        IReadOnlyList<ItemFeedbackRow> likedFeedback,
        User user)
    {
        var signals = new Dictionary<Guid, CustomNetflixRecommendationSignal>();
        foreach (var historyRow in history)
        {
            var watchedItem = _libraryManager.GetItemById<BaseItem>(historyRow.ItemId, user);
            if (watchedItem is null)
            {
                continue;
            }

            var sourceItem = GetRecommendationSource(watchedItem, user);
            var sourceId = sourceItem.Id;
            var next = new CustomNetflixRecommendationSignal(
                sourceId,
                GetRecommendationType(sourceItem),
                NormalizeGenres(sourceItem.Genres),
                historyRow.LastPlayedAt,
                historyRow.CompletedAt.HasValue,
                Math.Max(1, historyRow.PlayCount));
            MergeSignal(signals, next);
        }

        foreach (var feedback in likedFeedback)
        {
            var likedItem = _libraryManager.GetItemById<BaseItem>(feedback.ItemId, user);
            if (likedItem is null)
            {
                continue;
            }

            var sourceItem = GetRecommendationSource(likedItem, user);
            MergeSignal(
                signals,
                new CustomNetflixRecommendationSignal(
                    sourceItem.Id,
                    GetRecommendationType(sourceItem),
                    NormalizeGenres(sourceItem.Genres),
                    feedback.UpdatedAt,
                    true,
                    5));
        }

        return signals.Values.ToArray();
    }

    private static void MergeSignal(
        IDictionary<Guid, CustomNetflixRecommendationSignal> signals,
        CustomNetflixRecommendationSignal next)
    {
        if (!signals.TryGetValue(next.ItemId, out var current))
        {
            signals[next.ItemId] = next;
            return;
        }

        signals[next.ItemId] = current with
        {
            Genres = current.Genres
                .Concat(next.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            LastPlayedAt = current.LastPlayedAt > next.LastPlayedAt ? current.LastPlayedAt : next.LastPlayedAt,
            Completed = current.Completed || next.Completed,
            PlayCount = current.PlayCount + next.PlayCount
        };
    }

    private BaseItem GetRecommendationSource(BaseItem item, User user)
    {
        if (item is not Episode episode || episode.SeriesId.Equals(Guid.Empty))
        {
            return item;
        }

        return _libraryManager.GetItemById<Series>(episode.SeriesId, user) ?? item;
    }

    private IReadOnlyList<BaseItem> GetCandidates(User user, IReadOnlyList<string> topGenres, int poolSize)
    {
        var candidates = new List<BaseItem>(poolSize * 2);
        if (topGenres.Count > 0)
        {
            candidates.AddRange(_libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Recursive = true,
                IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
                Genres = topGenres,
                Limit = poolSize,
                OrderBy =
                [
                    (ItemSortBy.CommunityRating, SortOrder.Descending),
                    (ItemSortBy.PremiereDate, SortOrder.Descending)
                ]
            }));
        }

        if (candidates.Count < poolSize)
        {
            candidates.AddRange(_libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Recursive = true,
                IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
                Limit = poolSize,
                OrderBy =
                [
                    (ItemSortBy.CommunityRating, SortOrder.Descending),
                    (ItemSortBy.DateCreated, SortOrder.Descending)
                ]
            }));
        }

        return candidates
            .DistinctBy(item => item.Id)
            .ToArray();
    }

    private static string GetRecommendationType(BaseItem item)
        => item is Movie ? "Movie" : "Series";

    private static IReadOnlyList<string> NormalizeGenres(IReadOnlyList<string>? genres)
        => genres?
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();
}
