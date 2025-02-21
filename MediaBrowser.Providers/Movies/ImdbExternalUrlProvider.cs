using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Movies;

/// <summary>
/// External URLs for IMDb.
/// </summary>
public class ImdbExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "IMDb";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        var baseUrl = "https://www.imdb.com/";
        if (item.TryGetProviderId(MetadataProvider.Imdb, out var externalId))
        {
            yield return baseUrl + $"title/{externalId}";
        }
    }
}
