using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.GoogleBooks;

/// <inheritdoc/>
public class GoogleBooksExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc />
    public string Name => "Google Books";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId("GoogleBooks", out var externalId))
        {
            if (item is Book)
            {
                yield return $"https://books.google.com/books?id={externalId}";
            }
        }
    }
}
