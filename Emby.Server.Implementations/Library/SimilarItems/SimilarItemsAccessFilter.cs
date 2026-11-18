using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.Library.SimilarItems;

/// <summary>
/// Builds the access filter that decides which items a similar-items lookup may return for a user.
/// </summary>
internal static class SimilarItemsAccessFilter
{
    private static readonly BaseItemKind[] _itemByNameKinds =
    [
        BaseItemKind.Person,
        BaseItemKind.Genre,
        BaseItemKind.MusicGenre,
        BaseItemKind.MusicArtist,
        BaseItemKind.Studio
    ];

    /// <summary>
    /// Builds an access filter carrying the user's library access and parental restrictions.
    /// </summary>
    /// <param name="user">The user the lookup runs for.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <returns>The access filter.</returns>
    public static InternalItemsQuery Build(User user, ILibraryManager libraryManager)
    {
        // IncludeItemTypes is read only for the by-name exemption here; the caller applies this
        // filter through ApplyAccessFiltering, which does not translate it into a type restriction.
        var accessFilter = new InternalItemsQuery(user)
        {
            IncludeItemTypes = _itemByNameKinds
        };

        // ConfigureUserAccess populates TopParentIds for the libraries the user may open.
        libraryManager.ConfigureUserAccess(accessFilter, user);

        return accessFilter;
    }
}
