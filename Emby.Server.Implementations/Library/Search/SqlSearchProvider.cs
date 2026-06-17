#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Emby.Server.Implementations.Library.Search;

/// <summary>
/// Built-in SQL-based search provider that queries the library database directly.
/// </summary>
public class SqlSearchProvider : IInternalSearchProvider
{
    private const int DefaultSearchLimit = 100;
    private const float ExactMatchScore = 100f;
    private const float PrefixMatchScore = 80f;
    private const float WordPrefixMatchScore = 75f;
    private const float ContainsMatchScore = 50f;

    private static readonly Guid _placeholderId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IItemTypeLookup _itemTypeLookup;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IItemQueryHelpers _queryHelpers;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSearchProvider"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="itemTypeLookup">The item type lookup.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="queryHelpers">The shared item query helpers.</param>
    public SqlSearchProvider(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IItemTypeLookup itemTypeLookup,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IItemQueryHelpers queryHelpers)
    {
        _dbProvider = dbProvider;
        _itemTypeLookup = itemTypeLookup;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _queryHelpers = queryHelpers;
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
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(SearchProviderQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.SearchTerm);

        var rawSearchTerm = query.SearchTerm.Trim().RemoveDiacritics();
        if (string.IsNullOrEmpty(rawSearchTerm))
        {
            return [];
        }

        var cleanSearchTerm = rawSearchTerm.GetCleanValue();
        if (string.IsNullOrEmpty(cleanSearchTerm))
        {
            return [];
        }

        var cleanPrefix = cleanSearchTerm + " ";
        // OriginalTitle is stored mixed-case and isn't pre-normalized like CleanName,
        // so match it via a case-insensitive LIKE rather than a per-row case conversion
        // that may not translate to SQL on every provider.
        var likeOriginal = $"%{rawSearchTerm}%";
        var limit = query.Limit ?? DefaultSearchLimit;

        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            // Lightweight projection: select only what's needed to score and identify items.
            var dbQuery = dbContext.BaseItems
                .AsNoTracking()
                .Where(e => e.Id != _placeholderId)
                .Where(e => !e.IsVirtualItem)
                .Where(e => e.CleanName!.Contains(cleanSearchTerm)
                    || (e.OriginalTitle != null && EF.Functions.Like(e.OriginalTitle, likeOriginal)));

            dbQuery = ApplyTypeFilter(dbQuery, query.IncludeItemTypes, query.ExcludeItemTypes);
            dbQuery = ApplyMediaTypeFilter(dbQuery, query.MediaTypes);
            dbQuery = ApplyParentFilter(dbQuery, query.ParentId);
            dbQuery = ApplyUserAccessFilter(dbContext, dbQuery, query.UserId);

            // Compute the score in SQL: the ternary translates to a CASE WHEN. CleanName is
            // the pre-normalized (lowercase, diacritic-stripped) form, so we score against it
            // directly without any per-row case conversion. Items that match only via
            // OriginalTitle fall through to the Contains tier.
            // Tie-break by Id for deterministic ordering so the explicit OrderBy + Take
            // satisfies EF Core's row-limiting-with-OrderBy requirement.
            var scored = dbQuery.Select(e => new
            {
                e.Id,
                Score =
                    (e.CleanName == cleanSearchTerm) ? ExactMatchScore
                    : e.CleanName!.StartsWith(cleanSearchTerm) ? PrefixMatchScore
                    : e.CleanName!.Contains(cleanPrefix) ? WordPrefixMatchScore
                    : ContainsMatchScore
            });

            return await scored
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Id)
                .Take(limit)
                .Select(x => new SearchResult(x.Id, x.Score))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private IQueryable<BaseItemEntity> ApplyTypeFilter(
        IQueryable<BaseItemEntity> query,
        BaseItemKind[] includeItemTypes,
        BaseItemKind[] excludeItemTypes)
    {
        if (includeItemTypes.Length > 0)
        {
            var includeTypeNames = MapKindsToTypeNames(includeItemTypes);
            if (includeTypeNames.Count > 0)
            {
                query = query.Where(e => includeTypeNames.Contains(e.Type));
            }
        }
        else if (excludeItemTypes.Length > 0)
        {
            var excludeTypeNames = MapKindsToTypeNames(excludeItemTypes);
            if (excludeTypeNames.Count > 0)
            {
                query = query.Where(e => !excludeTypeNames.Contains(e.Type));
            }
        }

        return query;
    }

    private static IQueryable<BaseItemEntity> ApplyMediaTypeFilter(
        IQueryable<BaseItemEntity> query,
        MediaType[] mediaTypes)
    {
        if (mediaTypes.Length == 0)
        {
            return query;
        }

        var mediaTypeNames = mediaTypes.Select(m => m.ToString()).ToArray();
        return query.Where(e => e.MediaType != null && mediaTypeNames.Contains(e.MediaType));
    }

    private static IQueryable<BaseItemEntity> ApplyParentFilter(
        IQueryable<BaseItemEntity> query,
        Guid? parentId)
    {
        if (!parentId.HasValue || parentId.Value.IsEmpty())
        {
            return query;
        }

        var pid = parentId.Value;
        return query.Where(e => e.ParentId == pid || e.Parents!.Any(p => p.ParentItemId == pid));
    }

    private IQueryable<BaseItemEntity> ApplyUserAccessFilter(
        JellyfinDbContext dbContext,
        IQueryable<BaseItemEntity> query,
        Guid? userId)
    {
        if (!userId.HasValue || userId.Value.IsEmpty())
        {
            return query;
        }

        var user = _userManager.GetUserById(userId.Value);
        if (user is null)
        {
            return query;
        }

        var accessFilter = new InternalItemsQuery(user);
        _libraryManager.ConfigureUserAccess(accessFilter, user);
        return _queryHelpers.ApplyAccessFiltering(dbContext, query, accessFilter);
    }

    private List<string> MapKindsToTypeNames(BaseItemKind[] kinds)
    {
        var list = new List<string>(kinds.Length);
        foreach (var kind in kinds)
        {
            if (_itemTypeLookup.BaseItemKindNames.TryGetValue(kind, out var name) && name is not null)
            {
                list.Add(name);
            }
        }

        return list;
    }
}
