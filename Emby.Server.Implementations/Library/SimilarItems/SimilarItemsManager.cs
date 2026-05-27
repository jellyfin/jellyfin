using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.SimilarItems;

/// <summary>
/// Manages similar items providers and orchestrates similar items operations.
/// </summary>
public class SimilarItemsManager : ISimilarItemsManager
{
    private readonly ILogger<SimilarItemsManager> _logger;
    private readonly IServerApplicationPaths _appPaths;
    private readonly ILibraryManager _libraryManager;
    private readonly IFileSystem _fileSystem;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private ISimilarItemsProvider[] _similarItemsProviders = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SimilarItemsManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">The server application paths.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    public SimilarItemsManager(
        ILogger<SimilarItemsManager> logger,
        IServerApplicationPaths appPaths,
        ILibraryManager libraryManager,
        IFileSystem fileSystem,
        IServerConfigurationManager serverConfigurationManager)
    {
        _logger = logger;
        _appPaths = appPaths;
        _libraryManager = libraryManager;
        _fileSystem = fileSystem;
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <inheritdoc/>
    public void AddParts(IEnumerable<ISimilarItemsProvider> providers)
    {
        _similarItemsProviders = providers.ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ISimilarItemsProvider> GetSimilarItemsProviders<T>()
        where T : BaseItem
    {
        var itemType = typeof(T);
        return _similarItemsProviders
            .Where(p => (p is ILocalSimilarItemsProvider local && local.Supports(itemType))
                || (p is IRemoteSimilarItemsProvider remote && remote.Supports(itemType)))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BaseItem>> GetSimilarItemsAsync(
        BaseItem item,
        IReadOnlyList<Guid> excludeArtistIds,
        User? user,
        DtoOptions dtoOptions,
        int? limit,
        LibraryOptions? libraryOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(excludeArtistIds);

        var itemType = item.GetType();
        var requestedLimit = limit ?? 50;
        var itemKind = item.GetBaseItemKind();

        // Ensure ProviderIds is included in DtoOptions for matching remote provider responses
        if (!dtoOptions.Fields.Contains(ItemFields.ProviderIds))
        {
            dtoOptions.Fields = dtoOptions.Fields.Concat([ItemFields.ProviderIds]).ToArray();
        }

        // Local providers are always enabled. Remote providers must be explicitly enabled.
        var localProviders = _similarItemsProviders
            .OfType<ILocalSimilarItemsProvider>()
            .Where(p => p.Supports(itemType))
            .ToList();
        var remoteProviders = _similarItemsProviders
            .OfType<IRemoteSimilarItemsProvider>()
            .Where(p => p.Supports(itemType));
        var matchingProviders = new List<ISimilarItemsProvider>(localProviders);

        var typeOptions = libraryOptions?.GetTypeOptions(itemType.Name);
        if (typeOptions?.SimilarItemProviders?.Length > 0)
        {
            matchingProviders.AddRange(remoteProviders
                .Where(p => typeOptions.SimilarItemProviders.Contains(p.Name, StringComparer.OrdinalIgnoreCase)));
        }

        var orderConfig = typeOptions?.SimilarItemProviderOrder is { Length: > 0 } order
            ? order
            : typeOptions?.SimilarItemProviders;
        var orderedProviders = matchingProviders
            .OrderBy(p => GetConfiguredSimilarProviderOrder(orderConfig, p.Name))
            .ToList();

        var allResults = new List<(BaseItem Item, float Score)>();
        var excludeIds = new HashSet<Guid> { item.Id };
        foreach (var (providerOrder, provider) in orderedProviders.Index())
        {
            if (allResults.Count >= requestedLimit || cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (provider is ILocalSimilarItemsProvider localProvider)
                {
                    var query = new SimilarItemsQuery
                    {
                        User = user,
                        Limit = requestedLimit - allResults.Count,
                        DtoOptions = dtoOptions,
                        ExcludeItemIds = [.. excludeIds],
                        ExcludeArtistIds = excludeArtistIds
                    };

                    var items = await localProvider.GetSimilarItemsAsync(item, query, cancellationToken).ConfigureAwait(false);

                    foreach (var (position, resultItem) in items.Index())
                    {
                        if (excludeIds.Add(resultItem.Id))
                        {
                            var score = CalculateScore(null, providerOrder, position);
                            allResults.Add((resultItem, score));
                        }
                    }
                }
                else if (provider is IRemoteSimilarItemsProvider remoteProvider)
                {
                    var cachePath = GetSimilarItemsCachePath(provider.Name, itemType.Name, item.Id);

                    var cachedReferences = await TryReadSimilarItemsCacheAsync(cachePath, cancellationToken).ConfigureAwait(false);
                    if (cachedReferences is not null)
                    {
                        var resolvedItems = ResolveRemoteReferences(cachedReferences, providerOrder, user, dtoOptions, itemKind, excludeIds);
                        allResults.AddRange(resolvedItems);
                        continue;
                    }

                    var query = new SimilarItemsQuery
                    {
                        User = user,
                        Limit = requestedLimit - allResults.Count,
                        DtoOptions = dtoOptions,
                        ExcludeItemIds = [.. excludeIds],
                        ExcludeArtistIds = excludeArtistIds
                    };

                    // Collect references in batches and resolve against local library.
                    // Stop fetching once we have enough resolved local items.
                    const int BatchSize = 20;
                    var remaining = requestedLimit - allResults.Count;
                    var collectedReferences = new List<SimilarItemReference>();
                    var pendingBatch = new List<SimilarItemReference>();

                    await foreach (var reference in remoteProvider.GetSimilarItemsAsync(item, query, cancellationToken).ConfigureAwait(false))
                    {
                        collectedReferences.Add(reference);
                        pendingBatch.Add(reference);

                        if (pendingBatch.Count >= BatchSize)
                        {
                            var resolvedItems = ResolveRemoteReferences(pendingBatch, providerOrder, user, dtoOptions, itemKind, excludeIds);
                            allResults.AddRange(resolvedItems);
                            remaining -= resolvedItems.Count;
                            pendingBatch.Clear();

                            if (remaining <= 0)
                            {
                                break;
                            }
                        }
                    }

                    // Resolve any remaining references in the last partial batch
                    if (pendingBatch.Count > 0)
                    {
                        var resolvedItems = ResolveRemoteReferences(pendingBatch, providerOrder, user, dtoOptions, itemKind, excludeIds);
                        allResults.AddRange(resolvedItems);
                    }

                    if (collectedReferences.Count > 0 && provider.CacheDuration is not null)
                    {
                        await SaveSimilarItemsCacheAsync(cachePath, collectedReferences, provider.CacheDuration.Value, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Similar items provider {ProviderName} failed for item {ItemId}", provider.Name, item.Id);
            }
        }

        return allResults
            .OrderByDescending(x => x.Score)
            .Select(x => x.Item)
            .Take(requestedLimit)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SimilarItemsRecommendation>> GetMovieRecommendationsAsync(
        User? user,
        Guid parentId,
        int categoryLimit,
        int itemLimit,
        DtoOptions dtoOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dtoOptions);

        var recentlyPlayedMovies = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            OrderBy = [(ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.Random, SortOrder.Descending)],
            Limit = 7,
            ParentId = parentId,
            Recursive = true,
            IsPlayed = true,
            EnableGroupByMetadataKey = true,
            DtoOptions = dtoOptions
        });

        var itemTypes = new List<BaseItemKind> { BaseItemKind.Movie };
        if (_serverConfigurationManager.Configuration.EnableExternalContentInSuggestions)
        {
            itemTypes.Add(BaseItemKind.Trailer);
            itemTypes.Add(BaseItemKind.LiveTvProgram);
        }

        var likedMovies = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = itemTypes.ToArray(),
            IsMovie = true,
            OrderBy = [(ItemSortBy.Random, SortOrder.Descending)],
            Limit = 10,
            IsFavoriteOrLiked = true,
            ExcludeItemIds = recentlyPlayedMovies.Select(i => i.Id).ToArray(),
            EnableGroupByMetadataKey = true,
            ParentId = parentId,
            Recursive = true,
            DtoOptions = dtoOptions
        });

        var mostRecentMovies = recentlyPlayedMovies.Take(Math.Min(recentlyPlayedMovies.Count, 6)).ToList();
        var recentDirectors = GetPeopleNames(mostRecentMovies, [PersonType.Director]);
        var recentActors = GetPeopleNames(mostRecentMovies, [PersonType.Actor, PersonType.GuestStar]);

        // Cap baseline items to categoryLimit - the round-robin can't use more categories than that.
        var recentlyPlayedBaseline = recentlyPlayedMovies.Count > categoryLimit
            ? recentlyPlayedMovies.Take(categoryLimit).ToList()
            : recentlyPlayedMovies;
        var likedBaseline = likedMovies.Count > categoryLimit
            ? likedMovies.Take(categoryLimit).ToList()
            : likedMovies;

        var batchQuery = new SimilarItemsQuery
        {
            User = user,
            Limit = itemLimit,
            DtoOptions = dtoOptions
        };

        var similarToRecentlyPlayed = await GetSimilarItemsRecommendationsAsync(
            recentlyPlayedBaseline,
            RecommendationType.SimilarToRecentlyPlayed,
            batchQuery,
            cancellationToken).ConfigureAwait(false);

        var similarToLiked = await GetSimilarItemsRecommendationsAsync(
            likedBaseline,
            RecommendationType.SimilarToLikedItem,
            batchQuery,
            cancellationToken).ConfigureAwait(false);

        var hasDirectorFromRecentlyPlayed = GetPersonRecommendations(user, recentDirectors, itemLimit, dtoOptions, RecommendationType.HasDirectorFromRecentlyPlayed, itemTypes);
        var hasActorFromRecentlyPlayed = GetPersonRecommendations(user, recentActors, itemLimit, dtoOptions, RecommendationType.HasActorFromRecentlyPlayed, itemTypes);

        // Use a single enumerator per list, listed twice so MoveNext advances it
        // twice per round-robin pass (giving these categories double weight).
        // IMPORTANT: Declare as IEnumerator<T> to box the List<T>.Enumerator struct once;
        // using var would box separately per list insertion, creating independent copies.
        IEnumerator<SimilarItemsRecommendation> similarToRecentlyPlayedEnum = similarToRecentlyPlayed.GetEnumerator();
        IEnumerator<SimilarItemsRecommendation> similarToLikedEnum = similarToLiked.GetEnumerator();

        var categoryTypes = new List<IEnumerator<SimilarItemsRecommendation>>
        {
            similarToRecentlyPlayedEnum,
            similarToRecentlyPlayedEnum,
            similarToLikedEnum,
            similarToLikedEnum,
            hasDirectorFromRecentlyPlayed.GetEnumerator(),
            hasActorFromRecentlyPlayed.GetEnumerator()
        };

        var categories = new List<SimilarItemsRecommendation>();
        while (categories.Count < categoryLimit)
        {
            var allEmpty = true;
            foreach (var category in categoryTypes)
            {
                if (category.MoveNext())
                {
                    categories.Add(category.Current);
                    allEmpty = false;

                    if (categories.Count >= categoryLimit)
                    {
                        break;
                    }
                }
            }

            if (allEmpty)
            {
                break;
            }
        }

        return [.. categories.OrderBy(i => i.RecommendationType)];
    }

    private async Task<IReadOnlyList<SimilarItemsRecommendation>> GetSimilarItemsRecommendationsAsync(
        IReadOnlyList<BaseItem> baselineItems,
        RecommendationType recommendationType,
        SimilarItemsQuery query,
        CancellationToken cancellationToken)
    {
        var batchProvider = _similarItemsProviders
            .OfType<IBatchLocalSimilarItemsProvider>()
            .FirstOrDefault();

        if (batchProvider is null || baselineItems.Count == 0)
        {
            return [];
        }

        var batchResults = await batchProvider.GetBatchSimilarItemsAsync(baselineItems, query, cancellationToken).ConfigureAwait(false);

        var recommendations = new List<SimilarItemsRecommendation>(baselineItems.Count);
        foreach (var baseline in baselineItems)
        {
            if (batchResults.TryGetValue(baseline.Id, out var similar) && similar.Count > 0)
            {
                recommendations.Add(new SimilarItemsRecommendation
                {
                    BaselineItemName = baseline.Name,
                    CategoryId = baseline.Id,
                    RecommendationType = recommendationType,
                    Items = similar
                });
            }
        }

        return recommendations;
    }

    private IEnumerable<SimilarItemsRecommendation> GetPersonRecommendations(
        User? user,
        IReadOnlyList<string> names,
        int itemLimit,
        DtoOptions dtoOptions,
        RecommendationType type,
        IReadOnlyList<BaseItemKind> itemTypes)
    {
        var personTypes = type == RecommendationType.HasDirectorFromRecentlyPlayed
            ? [PersonType.Director]
            : Array.Empty<string>();

        foreach (var name in names)
        {
            var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Person = name,
                Limit = itemLimit + 2,
                PersonTypes = personTypes,
                IncludeItemTypes = itemTypes.ToArray(),
                IsMovie = true,
                IsPlayed = false,
                EnableGroupByMetadataKey = true,
                DtoOptions = dtoOptions
            })
                .DistinctBy(i => i.GetProviderId(MetadataProvider.Imdb) ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
                .Take(itemLimit)
                .ToList();

            if (items.Count > 0)
            {
                yield return new SimilarItemsRecommendation
                {
                    BaselineItemName = name,
                    CategoryId = name.GetMD5(),
                    RecommendationType = type,
                    Items = items
                };
            }
        }
    }

    private IReadOnlyList<string> GetPeopleNames(IReadOnlyList<BaseItem> items, IReadOnlyList<string> personTypes)
    {
        var itemIds = items.Select(i => i.Id).ToArray();
        return _libraryManager.GetPeopleNamesByItems(itemIds, personTypes, limit: 0);
    }

    private List<(BaseItem Item, float Score)> ResolveRemoteReferences(
        IReadOnlyList<SimilarItemReference> references,
        int providerOrder,
        User? user,
        DtoOptions dtoOptions,
        BaseItemKind itemKind,
        HashSet<Guid> excludeIds)
    {
        if (references.Count == 0)
        {
            return [];
        }

        var resolvedById = new Dictionary<Guid, (BaseItem Item, float Score)>();
        var providerLookup = new Dictionary<(string ProviderName, string ProviderId), (float? Score, int Position)>(StringTupleComparer.Instance);

        foreach (var (position, match) in references.Index())
        {
            var lookupKey = (match.ProviderName, match.ProviderId);
            if (!providerLookup.TryGetValue(lookupKey, out var existing))
            {
                providerLookup[lookupKey] = (match.Score, position);
            }
            else if (match.Score > existing.Score || (match.Score == existing.Score && position < existing.Position))
            {
                providerLookup[lookupKey] = (match.Score, position);
            }
        }

        var allProviderIds = providerLookup
            .GroupBy(kvp => kvp.Key.ProviderName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key.ProviderId).ToArray());

        var query = new InternalItemsQuery(user)
        {
            HasAnyProviderIds = allProviderIds,
            IncludeItemTypes = [itemKind],
            DtoOptions = dtoOptions
        };

        var items = _libraryManager.GetItemList(query);

        foreach (var item in items)
        {
            if (excludeIds.Contains(item.Id) || resolvedById.ContainsKey(item.Id))
            {
                continue;
            }

            foreach (var providerName in allProviderIds.Keys)
            {
                if (item.TryGetProviderId(providerName, out var itemProviderId) && providerLookup.TryGetValue((providerName, itemProviderId), out var matchInfo))
                {
                    var score = CalculateScore(matchInfo.Score, providerOrder, matchInfo.Position);
                    if (!resolvedById.TryGetValue(item.Id, out var existing) || existing.Score < score)
                    {
                        excludeIds.Add(item.Id);
                        resolvedById[item.Id] = (item, score);
                    }

                    break;
                }
            }
        }

        return [.. resolvedById.Values];
    }

    private static float CalculateScore(float? matchScore, int providerOrder, int position)
    {
        // Use provider-supplied score if available, otherwise derive from position
        var baseScore = matchScore ?? (1.0f - (position * 0.02f));

        // Apply small boost based on provider order (higher priority providers get small bonus)
        var priorityBoost = Math.Max(0, 10 - providerOrder) * 0.005f;

        return Math.Clamp(baseScore + priorityBoost, 0f, 1f);
    }

    private static int GetConfiguredSimilarProviderOrder(string[]? orderConfig, string providerName)
    {
        if (orderConfig is null || orderConfig.Length == 0)
        {
            return int.MaxValue;
        }

        var index = Array.FindIndex(orderConfig, name => string.Equals(name, providerName, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : int.MaxValue;
    }

    private string GetSimilarItemsCachePath(string providerName, string baseItemType, Guid itemId)
    {
        var dataPath = Path.Combine(
            _appPaths.CachePath,
            $"{providerName.ToLowerInvariant()}-similar-{baseItemType.ToLowerInvariant()}");
        return Path.Combine(dataPath, $"{itemId.ToString("N", CultureInfo.InvariantCulture)}.json");
    }

    private async Task<List<SimilarItemReference>?> TryReadSimilarItemsCacheAsync(string cachePath, CancellationToken cancellationToken)
    {
        var fileInfo = _fileSystem.GetFileSystemInfo(cachePath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            return null;
        }

        try
        {
            var stream = File.OpenRead(cachePath);
            await using (stream.ConfigureAwait(false))
            {
                var cache = await JsonSerializer.DeserializeAsync<SimilarItemsCache>(stream, JsonDefaults.Options, cancellationToken).ConfigureAwait(false);
                if (cache?.References is not null && DateTime.UtcNow < cache.ExpiresAt)
                {
                    return cache.References;
                }
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read similar items cache from {CachePath}", cachePath);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse similar items cache from {CachePath}", cachePath);
        }

        return null;
    }

    private async Task SaveSimilarItemsCacheAsync(string cachePath, List<SimilarItemReference> references, TimeSpan cacheDuration, CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var cache = new SimilarItemsCache
            {
                References = references,
                ExpiresAt = DateTime.UtcNow.Add(cacheDuration)
            };

            var stream = File.Create(cachePath);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, cache, JsonDefaults.Options, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to save similar items cache to {CachePath}", cachePath);
        }
    }

    private sealed class SimilarItemsCache
    {
        public List<SimilarItemReference>? References { get; set; }

        public DateTime ExpiresAt { get; set; }
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string Key, string Value)>
    {
        public static readonly StringTupleComparer Instance = new();

        public bool Equals((string Key, string Value) x, (string Key, string Value) y)
            => string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Key, string Value) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Value));
    }
}
