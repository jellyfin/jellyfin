using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.TV;

/// <summary>
/// External URLs for TMDb.
/// </summary>
public class Zap2ItExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "Zap2It";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        var externalId = item.GetProviderId(MetadataProvider.Zap2It);
        if (!string.IsNullOrEmpty(externalId))
        {
            yield return $"http://tvlistings.zap2it.com/overview.html?programSeriesId={externalId}";
         }
    }
}
