using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.Library.Search;

/// <summary>
/// Builds the access filter that decides which items a search may return for a user.
/// </summary>
internal static class SearchQueryAccessFilter
{
    /// <summary>
    /// Builds an access filter carrying the search's library access and type filters.
    /// </summary>
    /// <param name="user">The user the search runs for.</param>
    /// <param name="query">The search query.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <returns>The access filter.</returns>
    public static InternalItemsQuery Build(User user, SearchProviderQuery query, ILibraryManager libraryManager)
    {
        // The type filters have to travel with the access filter: a by-name item belongs to no
        // library, so it carries no TopParentId to match, and the library filter only knows to
        // exempt it when the query says those types are wanted. A search scoped to a parent gets
        // no exemption because a by-name item has no parent to descend from either.
        var accessFilter = new InternalItemsQuery(user)
        {
            IncludeItemTypes = query.IncludeItemTypes,
            ExcludeItemTypes = query.ExcludeItemTypes,
            IncludeItemsByName = !query.ParentId.HasValue || query.ParentId.Value.IsEmpty()
        };

        // ConfigureUserAccess populates TopParentIds for the libraries the user may open.
        libraryManager.ConfigureUserAccess(accessFilter, user);

        return accessFilter;
    }
}
