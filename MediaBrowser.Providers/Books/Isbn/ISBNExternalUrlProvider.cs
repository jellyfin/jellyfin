using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Books.Isbn;

/// <inheritdoc/>
public class IsbnExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "ISBN";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId("ISBN", out var externalId))
        {
            if (item is Book)
            {
                yield return $"https://search.worldcat.org/search?q=bn:{externalId}";
            }
        }
    }
}
