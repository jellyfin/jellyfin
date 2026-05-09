using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.ComicVine;

/// <inheritdoc/>
public class ComicVineExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "Comic Vine";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId("ComicVine", out var externalId))
        {
            switch (item)
            {
                case Person:
                case Book:
                    yield return $"https://comicvine.gamespot.com/{externalId}";
                    break;
            }
        }
    }
}
