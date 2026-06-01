using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Library.Recommendations;

/// <summary>
/// Resolves whether a requested type/media-type list maps to a single recommendable kind.
/// </summary>
public static class RecommendableKindResolver
{
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
    {
        if (requestedTypes is { Count: 1 })
        {
            var only = requestedTypes[0];
            if (only is BaseItemKind.Movie or BaseItemKind.Series)
            {
                kind = only;
                return true;
            }
        }

        kind = default;
        return false;
    }
}
