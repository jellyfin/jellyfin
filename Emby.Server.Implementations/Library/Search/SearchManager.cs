using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Search;

/// <summary>
/// Manages search providers and orchestrates search operations.
/// </summary>
public class SearchManager : ISearchManager
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<SearchManager> _logger;
    private IExternalSearchProvider[] _externalProviders = [];
    private IInternalSearchProvider[] _internalProviders = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchManager"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="logger">The logger.</param>
    public SearchManager(
        ILibraryManager libraryManager,
        IUserManager userManager,
        ILogger<SearchManager> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void AddParts(IEnumerable<ISearchProvider> providers)
    {
        var allProviders = providers.OrderBy(p => p.Priority).ToArray();

        _externalProviders = allProviders.OfType<IExternalSearchProvider>().ToArray();
        _internalProviders = allProviders.OfType<IInternalSearchProvider>().ToArray();

        _logger.LogInformation(
            "Registered {ExternalCount} external search providers: {ExternalProviders}. Fallback providers: {FallbackProviders}",
            _externalProviders.Length,
            string.Join(", ", _externalProviders.Select(p => $"{p.Name} (priority {p.Priority})")),
            string.Join(", ", _internalProviders.Select(p => $"{p.Name} (priority {p.Priority})")));
    }

    /// <inheritdoc/>
    public IReadOnlyList<ISearchProvider> GetProviders()
    {
        return [.. _externalProviders, .. _internalProviders];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SearchResult>> GetSearchResultsWithItemsAsync(
        SearchProviderQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.SearchTerm);

        var searchTerm = query.SearchTerm.Trim().RemoveDiacritics();

        var queryWithItems = new SearchProviderQuery
        {
            SearchTerm = query.SearchTerm,
            UserId = query.UserId,
            IncludeItemTypes = query.IncludeItemTypes,
            ExcludeItemTypes = query.ExcludeItemTypes,
            MediaTypes = query.MediaTypes,
            Limit = query.Limit,
            ParentId = query.ParentId,
            IncludeItemData = true
        };

        var candidates = await GetResultsWithDataFromProvidersAsync(_externalProviders, queryWithItems, searchTerm, cancellationToken).ConfigureAwait(false);
        if (candidates.Count == 0 && _internalProviders.Length > 0)
        {
            _logger.LogDebug("No results from external providers, falling back to internal providers");
            candidates = await GetResultsWithDataFromProvidersAsync(_internalProviders, queryWithItems, searchTerm, cancellationToken).ConfigureAwait(false);
        }

        return candidates;
    }

    /// <inheritdoc/>
    public async Task<QueryResult<SearchHintInfo>> GetSearchHintsAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.SearchTerm);

        var searchTerm = query.SearchTerm.Trim().RemoveDiacritics();

        var providerQuery = BuildProviderQuery(query, searchTerm);
        var candidateScores = await GetSearchResultsAsync(providerQuery, searchTerm, cancellationToken).ConfigureAwait(false);
        if (candidateScores.Count == 0)
        {
            return new QueryResult<SearchHintInfo>();
        }

        var user = !query.UserId.IsEmpty() ? _userManager.GetUserById(query.UserId) : null;

        var excludeItemTypes = BuildExcludeItemTypes(query);
        var includeItemTypes = BuildIncludeItemTypes(query);

        var internalQuery = new InternalItemsQuery(user)
        {
            ItemIds = candidateScores.Keys.ToArray(),
            ExcludeItemTypes = excludeItemTypes.ToArray(),
            IncludeItemTypes = includeItemTypes.Count > 0 ? includeItemTypes.ToArray() : [],
            MediaTypes = query.MediaTypes.ToArray(),
            IncludeItemsByName = !query.ParentId.HasValue,
            ParentId = query.ParentId ?? Guid.Empty,
            Recursive = true,
            IsKids = query.IsKids,
            IsMovie = query.IsMovie,
            IsNews = query.IsNews,
            IsSeries = query.IsSeries,
            IsSports = query.IsSports,
            DtoOptions = new DtoOptions
            {
                Fields =
                [
                    ItemFields.AirTime,
                    ItemFields.DateCreated,
                    ItemFields.ChannelInfo,
                    ItemFields.ParentId
                ]
            }
        };

        // MusicArtist items are "ItemsByName" entities - virtual items that aggregate content by artist name
        // rather than being stored as regular library items. They require special handling:
        // 1. Convert ParentId to AncestorIds (to filter by library folder)
        // 2. Set IncludeItemsByName = true (to include these virtual items in results)
        // 3. Clear IncludeItemTypes (GetAllArtists handles type filtering internally)
        // 4. Use GetAllArtists() instead of GetItemList() to query the artist index
        IReadOnlyList<BaseItem> items;
        if (internalQuery.IncludeItemTypes.Length == 1 && internalQuery.IncludeItemTypes[0] == BaseItemKind.MusicArtist)
        {
            if (!internalQuery.ParentId.IsEmpty())
            {
                internalQuery.AncestorIds = [internalQuery.ParentId];
                internalQuery.ParentId = Guid.Empty;
            }

            internalQuery.IncludeItemsByName = true;
            internalQuery.IncludeItemTypes = [];
            items = _libraryManager.GetAllArtists(internalQuery).Items.Select(i => i.Item).ToList();
        }
        else
        {
            items = _libraryManager.GetItemList(internalQuery);
        }

        var orderedResults = items
            .Select(item => new SearchHintInfo { Item = item })
            .OrderByDescending(hint => candidateScores.GetValueOrDefault(hint.Item.Id, 0f))
            .ToList();

        var totalCount = orderedResults.Count;

        if (query.StartIndex.HasValue)
        {
            orderedResults = orderedResults.Skip(query.StartIndex.Value).ToList();
        }

        if (query.Limit.HasValue)
        {
            orderedResults = orderedResults.Take(query.Limit.Value).ToList();
        }

        return new QueryResult<SearchHintInfo>(query.StartIndex, totalCount, orderedResults);
    }

    private async Task<IReadOnlyDictionary<Guid, float>> GetSearchResultsAsync(
        SearchProviderQuery query,
        string searchTerm,
        CancellationToken cancellationToken)
    {
        var scores = await GetSearchResultsFromProvidersAsync(_externalProviders, query, searchTerm, cancellationToken).ConfigureAwait(false);
        if (scores.Count == 0 && _internalProviders.Length > 0)
        {
            _logger.LogDebug("No results from external providers, falling back to internal providers");
            scores = await GetSearchResultsFromProvidersAsync(_internalProviders, query, searchTerm, cancellationToken).ConfigureAwait(false);
        }

        return scores;
    }

    private async Task<List<SearchResult>> GetResultsWithDataFromProvidersAsync(
        IEnumerable<ISearchProvider> providers,
        SearchProviderQuery providerQuery,
        string searchTerm,
        CancellationToken cancellationToken)
    {
        var allResults = new List<SearchResult>();
        var seenIds = new HashSet<Guid>();
        var requestedLimit = providerQuery.Limit ?? 100;

        foreach (var provider in providers.Where(p => p.CanSearch(providerQuery)))
        {
            if (allResults.Count >= requestedLimit || cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (provider is IExternalSearchProvider externalProvider)
                {
                    var batchSize = 20;
                    var pendingBatch = new List<SearchResult>();

                    await foreach (var result in externalProvider.SearchAsync(providerQuery, cancellationToken).ConfigureAwait(false))
                    {
                        if (seenIds.Add(result.ItemId))
                        {
                            pendingBatch.Add(result);
                        }

                        if (pendingBatch.Count >= batchSize)
                        {
                            allResults.AddRange(pendingBatch);
                            pendingBatch.Clear();

                            if (allResults.Count >= requestedLimit)
                            {
                                break;
                            }
                        }
                    }

                    if (pendingBatch.Count > 0)
                    {
                        allResults.AddRange(pendingBatch);
                    }

                    _logger.LogDebug(
                        "External provider {Provider} returned {Count} results for search term '{SearchTerm}'",
                        provider.Name,
                        allResults.Count,
                        searchTerm);
                }
                else
                {
                    var results = await provider.SearchAsync(providerQuery, cancellationToken).ConfigureAwait(false);
                    foreach (var result in results)
                    {
                        if (seenIds.Add(result.ItemId))
                        {
                            allResults.Add(result);
                        }
                    }

                    _logger.LogDebug(
                        "Provider {Provider} returned {Count} results for search term '{SearchTerm}'",
                        provider.Name,
                        results.Count,
                        searchTerm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search provider {Provider} failed for term '{SearchTerm}'", provider.Name, searchTerm);
            }
        }

        return allResults
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.Item is not null)
            .Take(requestedLimit)
            .ToList();
    }

    private async Task<Dictionary<Guid, float>> GetSearchResultsFromProvidersAsync(
        IEnumerable<ISearchProvider> providers,
        SearchProviderQuery providerQuery,
        string searchTerm,
        CancellationToken cancellationToken)
    {
        var scores = new Dictionary<Guid, float>();
        var requestedLimit = providerQuery.Limit ?? 100;

        foreach (var provider in providers.Where(p => p.CanSearch(providerQuery)))
        {
            if (scores.Count >= requestedLimit || cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (provider is IExternalSearchProvider externalProvider)
                {
                    var count = 0;
                    await foreach (var result in externalProvider.SearchAsync(providerQuery, cancellationToken).ConfigureAwait(false))
                    {
                        if (!scores.TryGetValue(result.ItemId, out var existingScore) || result.Score > existingScore)
                        {
                            scores[result.ItemId] = result.Score;
                        }

                        count++;
                        if (scores.Count >= requestedLimit)
                        {
                            break;
                        }
                    }

                    _logger.LogDebug(
                        "External provider {Provider} returned {Count} candidates for search term '{SearchTerm}'",
                        provider.Name,
                        count,
                        searchTerm);
                }
                else
                {
                    var candidates = await provider.SearchAsync(providerQuery, cancellationToken).ConfigureAwait(false);
                    foreach (var result in candidates)
                    {
                        if (!scores.TryGetValue(result.ItemId, out var existingScore) || result.Score > existingScore)
                        {
                            scores[result.ItemId] = result.Score;
                        }
                    }

                    _logger.LogDebug(
                        "Provider {Provider} returned {Count} candidates for search term '{SearchTerm}'",
                        provider.Name,
                        candidates.Count,
                        searchTerm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search provider {Provider} failed for term '{SearchTerm}'", provider.Name, searchTerm);
            }
        }

        return scores;
    }

    private static SearchProviderQuery BuildProviderQuery(SearchQuery query, string searchTerm)
    {
        var excludeItemTypes = BuildExcludeItemTypes(query);
        var includeItemTypes = BuildIncludeItemTypes(query);

        // Remove any excluded types from includes
        if (includeItemTypes.Count > 0 && excludeItemTypes.Count > 0)
        {
            includeItemTypes.RemoveAll(excludeItemTypes.Contains);
        }

        return new SearchProviderQuery
        {
            SearchTerm = searchTerm,
            UserId = query.UserId.IsEmpty() ? null : query.UserId,
            IncludeItemTypes = includeItemTypes.ToArray(),
            ExcludeItemTypes = excludeItemTypes.ToArray(),
            MediaTypes = query.MediaTypes.ToArray(),
            Limit = query.Limit,
            ParentId = query.ParentId
        };
    }

    private static List<BaseItemKind> BuildExcludeItemTypes(SearchQuery query)
    {
        var excludeItemTypes = query.ExcludeItemTypes.ToList();

        excludeItemTypes.Add(BaseItemKind.Year);
        excludeItemTypes.Add(BaseItemKind.Folder);
        excludeItemTypes.Add(BaseItemKind.CollectionFolder);

        if (!query.IncludeGenres)
        {
            AddIfMissing(excludeItemTypes, BaseItemKind.Genre);
            AddIfMissing(excludeItemTypes, BaseItemKind.MusicGenre);
        }

        if (!query.IncludePeople)
        {
            AddIfMissing(excludeItemTypes, BaseItemKind.Person);
        }

        if (!query.IncludeStudios)
        {
            AddIfMissing(excludeItemTypes, BaseItemKind.Studio);
        }

        if (!query.IncludeArtists)
        {
            AddIfMissing(excludeItemTypes, BaseItemKind.MusicArtist);
        }

        return excludeItemTypes;
    }

    private static List<BaseItemKind> BuildIncludeItemTypes(SearchQuery query)
    {
        var includeItemTypes = query.IncludeItemTypes.ToList();
        if (query.IncludeGenres && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.Genre)))
        {
            if (!query.IncludeMedia)
            {
                AddIfMissing(includeItemTypes, BaseItemKind.Genre);
                AddIfMissing(includeItemTypes, BaseItemKind.MusicGenre);
            }
        }

        if (query.IncludePeople && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.Person)))
        {
            if (!query.IncludeMedia)
            {
                AddIfMissing(includeItemTypes, BaseItemKind.Person);
            }
        }

        if (query.IncludeStudios && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.Studio)))
        {
            if (!query.IncludeMedia)
            {
                AddIfMissing(includeItemTypes, BaseItemKind.Studio);
            }
        }

        if (query.IncludeArtists && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.MusicArtist)))
        {
            if (!query.IncludeMedia)
            {
                AddIfMissing(includeItemTypes, BaseItemKind.MusicArtist);
            }
        }

        return includeItemTypes;
    }

    private static void AddIfMissing(List<BaseItemKind> list, BaseItemKind value)
    {
        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }
}
