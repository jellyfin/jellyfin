using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;

namespace Emby.Server.Implementations.Library.Search;

/// <summary>
/// Built-in SQL-based search provider that uses the library's database.
/// </summary>
public class SqlSearchProvider : IInternalSearchProvider
{
    private const int DefaultSearchLimit = 100;
    private const float ExactMatchScore = 100;
    private const float PrefixMatchScore = 80;
    private const float ContainsMatchScore = 50;

    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSearchProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    public SqlSearchProvider(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <inheritdoc/>
    public string Name => "Database";

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.SearchProvider;

    /// <inheritdoc/>
    public int Priority => 100; // Low priority - runs as fallback

    /// <inheritdoc/>
    public bool CanSearch(SearchProviderQuery query)
    {
        // SQL search can always handle any query
        return true;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchProviderQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.SearchTerm);

        var searchTerm = query.SearchTerm.Trim().RemoveDiacritics();
        var cleanSearchTerm = searchTerm.ToLowerInvariant();

        var internalQuery = new InternalItemsQuery
        {
            SearchTerm = searchTerm,
            IncludeItemTypes = query.IncludeItemTypes,
            ExcludeItemTypes = query.ExcludeItemTypes,
            MediaTypes = query.MediaTypes,
            Limit = query.Limit ?? DefaultSearchLimit,
            Recursive = true,
            IncludeItemsByName = !query.ParentId.HasValue,
            ParentId = query.ParentId ?? Guid.Empty,
            DtoOptions = new DtoOptions(false)
        };

        var items = _libraryManager.GetItemList(internalQuery);
        var results = new List<SearchResult>(items.Count);

        foreach (var item in items)
        {
            var score = CalculateRelevanceScore(item, cleanSearchTerm);
            // Include item data when requested
            results.Add(new SearchResult(item.Id, score, query.IncludeItemData ? item : null));
        }

        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    private static float CalculateRelevanceScore(BaseItem item, string searchTerm)
    {
        var name = item.Name?.ToLowerInvariant() ?? string.Empty;
        var cleanName = name.RemoveDiacritics();

        // Also check OriginalTitle for foreign language content
        var originalTitle = item.OriginalTitle?.ToLowerInvariant() ?? string.Empty;
        var cleanOriginalTitle = originalTitle.RemoveDiacritics();

        // Exact match on Name or OriginalTitle
        if (cleanName.Equals(searchTerm, StringComparison.Ordinal) ||
            cleanOriginalTitle.Equals(searchTerm, StringComparison.Ordinal))
        {
            return ExactMatchScore;
        }

        // Prefix match (starts with search term)
        if (cleanName.StartsWith(searchTerm, StringComparison.Ordinal) ||
            cleanOriginalTitle.StartsWith(searchTerm, StringComparison.Ordinal))
        {
            return PrefixMatchScore;
        }

        // Word prefix match (e.g., "The Matrix" when searching "matrix")
        if (cleanName.Contains(' ' + searchTerm, StringComparison.Ordinal) ||
            cleanOriginalTitle.Contains(' ' + searchTerm, StringComparison.Ordinal))
        {
            return PrefixMatchScore - 5f;
        }

        // Contains match
        return ContainsMatchScore;
    }
}
