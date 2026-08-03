using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Search;

/// <summary>
/// Manages search providers and orchestrates search operations.
/// </summary>
public class SearchManager : ISearchManager
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IItemQueryHelpers _queryHelpers;
    private readonly ILogger<SearchManager> _logger;
    private IExternalSearchProvider[] _externalProviders = [];
    private IInternalSearchProvider[] _internalProviders = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchManager"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="queryHelpers">The shared item query helpers.</param>
    /// <param name="logger">The logger.</param>
    public SearchManager(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IItemQueryHelpers queryHelpers,
        ILogger<SearchManager> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dbProvider = dbProvider;
        _queryHelpers = queryHelpers;
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
    public async Task<IReadOnlyList<SearchResult>> GetSearchResultsAsync(
        SearchProviderQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.SearchTerm);

        var searchTerm = query.SearchTerm.Trim().RemoveDiacritics();

        var externalTask = CollectFromProvidersAsync(_externalProviders, query, searchTerm, cancellationToken);
        var internalTask = _internalProviders.Length > 0
            ? CollectFromProvidersAsync(_internalProviders, query, searchTerm, cancellationToken)
            : Task.FromResult<IReadOnlyList<SearchResult>>([]);

        await Task.WhenAll(externalTask, internalTask).ConfigureAwait(false);

        var externalResults = await externalTask.ConfigureAwait(false);
        var fromExternal = externalResults.Count > 0;
        IReadOnlyList<SearchResult> results;
        if (fromExternal)
        {
            results = externalResults;
        }
        else
        {
            results = await internalTask.ConfigureAwait(false);
            if (_internalProviders.Length > 0)
            {
                _logger.LogDebug("No results from external providers, using internal provider results");
            }
        }

        // Internal providers apply user-access filtering inline in their queries. External
        // providers don't know about user permissions, so they may return IDs from hidden
        // libraries or items the user is otherwise blocked from. Run the post-filter only
        // when results came from externals to close that gap. The Items controller's second
        // roundtrip via folder.GetItems applies most of these again, but it does not restrict
        // by TopParentIds when ItemIds is set.
        if (fromExternal && results.Count > 0 && query.UserId.HasValue && !query.UserId.Value.IsEmpty())
        {
            var user = _userManager.GetUserById(query.UserId.Value);
            if (user is not null)
            {
                results = await FilterByUserAccessAsync(results, user, cancellationToken).ConfigureAwait(false);
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<SearchResult>> FilterByUserAccessAsync(
        IReadOnlyList<SearchResult> candidates,
        User user,
        CancellationToken cancellationToken)
    {
        // SetUser populates parental rating + blocked/allowed tags. ConfigureUserAccess populates
        // TopParentIds for the user's accessible libraries — we call it before assigning ItemIds
        // because LibraryManager.AddUserToQuery skips TopParentIds when ItemIds is non-empty.
        var accessFilter = new InternalItemsQuery(user);
        _libraryManager.ConfigureUserAccess(accessFilter, user);

        Guid[] candidateIds = [.. candidates.Select(c => c.ItemId)];

        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var baseQuery = dbContext.BaseItems
                .AsNoTracking()
                .WhereOneOrMany(candidateIds, e => e.Id);

            baseQuery = _queryHelpers.ApplyAccessFiltering(dbContext, baseQuery, accessFilter);

            var allowedCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
            if (allowedCount == candidates.Count)
            {
                return candidates;
            }

            var allowedIds = await baseQuery
                .Select(e => e.Id)
                .ToHashSetAsync(cancellationToken)
                .ConfigureAwait(false);

            var filtered = candidates.Where(c => allowedIds.Contains(c.ItemId)).ToList();
            if (filtered.Count < candidates.Count)
            {
                _logger.LogDebug(
                    "Dropped {Dropped} of {Total} search candidates due to user access filtering",
                    candidates.Count - filtered.Count,
                    candidates.Count);
            }

            return filtered;
        }
    }

    /// <inheritdoc/>
    public async Task<QueryResult<SearchHintInfo>> GetSearchHintsAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.SearchTerm);

        var providerQuery = BuildProviderQuery(query);
        var candidates = await GetSearchResultsAsync(providerQuery, cancellationToken).ConfigureAwait(false);
        if (candidates.Count == 0)
        {
            return new QueryResult<SearchHintInfo>();
        }

        var candidateScores = BuildScoreLookup(candidates);
        var user = query.UserId.IsEmpty() ? null : _userManager.GetUserById(query.UserId);

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

    private async Task<IReadOnlyList<SearchResult>> CollectFromProvidersAsync(
        IEnumerable<ISearchProvider> providers,
        SearchProviderQuery providerQuery,
        string searchTerm,
        CancellationToken cancellationToken)
    {
        var requestedLimit = providerQuery.Limit ?? 100;
        var applicable = providers.Where(p => p.CanSearch(providerQuery)).ToArray();
        if (applicable.Length == 0)
        {
            return [];
        }

        var perProvider = await Task.WhenAll(
            applicable.Select(p => CollectFromProviderAsync(p, providerQuery, searchTerm, requestedLimit, cancellationToken)))
            .ConfigureAwait(false);

        var bestScores = new Dictionary<Guid, float>();
        foreach (var providerResults in perProvider)
        {
            foreach (var result in providerResults)
            {
                UpdateBestScore(bestScores, result);
            }
        }

        return bestScores
            .Select(kvp => new SearchResult(kvp.Key, kvp.Value))
            .OrderByDescending(r => r.Score)
            .Take(requestedLimit)
            .ToList();
    }

    private async Task<IReadOnlyList<SearchResult>> CollectFromProviderAsync(
        ISearchProvider provider,
        SearchProviderQuery providerQuery,
        string searchTerm,
        int requestedLimit,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = provider is IExternalSearchProvider externalProvider
                ? await CollectFromExternalProviderAsync(externalProvider, providerQuery, requestedLimit, cancellationToken).ConfigureAwait(false)
                : await provider.SearchAsync(providerQuery, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Provider {Provider} returned {Count} candidates for search term '{SearchTerm}'",
                provider.Name,
                results.Count,
                searchTerm);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search provider {Provider} failed for term '{SearchTerm}'", provider.Name, searchTerm);
            return [];
        }
    }

    private static async Task<IReadOnlyList<SearchResult>> CollectFromExternalProviderAsync(
        IExternalSearchProvider provider,
        SearchProviderQuery providerQuery,
        int requestedLimit,
        CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();
        await foreach (var result in provider.SearchAsync(providerQuery, cancellationToken).ConfigureAwait(false))
        {
            results.Add(result);
            if (results.Count >= requestedLimit)
            {
                break;
            }
        }

        return results;
    }

    private static void UpdateBestScore(Dictionary<Guid, float> bestScores, SearchResult result)
    {
        if (!bestScores.TryGetValue(result.ItemId, out var existingScore) || result.Score > existingScore)
        {
            bestScores[result.ItemId] = result.Score;
        }
    }

    private static Dictionary<Guid, float> BuildScoreLookup(IReadOnlyList<SearchResult> results)
    {
        var lookup = new Dictionary<Guid, float>(results.Count);
        foreach (var result in results)
        {
            lookup[result.ItemId] = result.Score;
        }

        return lookup;
    }

    private static SearchProviderQuery BuildProviderQuery(SearchQuery query)
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
            SearchTerm = query.SearchTerm,
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
        if (query.IncludeMedia)
        {
            return includeItemTypes;
        }

        if (query.IncludeGenres && IsEmptyOrContains(includeItemTypes, BaseItemKind.Genre))
        {
            AddIfMissing(includeItemTypes, BaseItemKind.Genre);
            AddIfMissing(includeItemTypes, BaseItemKind.MusicGenre);
        }

        if (query.IncludePeople && IsEmptyOrContains(includeItemTypes, BaseItemKind.Person))
        {
            AddIfMissing(includeItemTypes, BaseItemKind.Person);
        }

        if (query.IncludeStudios && IsEmptyOrContains(includeItemTypes, BaseItemKind.Studio))
        {
            AddIfMissing(includeItemTypes, BaseItemKind.Studio);
        }

        if (query.IncludeArtists && IsEmptyOrContains(includeItemTypes, BaseItemKind.MusicArtist))
        {
            AddIfMissing(includeItemTypes, BaseItemKind.MusicArtist);
        }

        return includeItemTypes;
    }

    private static bool IsEmptyOrContains(List<BaseItemKind> list, BaseItemKind value)
        => list.Count == 0 || list.Contains(value);

    private static void AddIfMissing(List<BaseItemKind> list, BaseItemKind value)
    {
        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }
}
