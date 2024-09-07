using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers;

/// <summary>
/// Interface to include related urls for an item.
/// </summary>
public interface IExternalUrlProvider
{
    /// <summary>
    /// Gets the external service name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get the list of external urls.
    /// </summary>
    /// <param name="item">The item to get external urls for.</param>
    /// <returns>The list of external urls.</returns>
    IEnumerable<string> GetExternalUrls(BaseItem item);
}
