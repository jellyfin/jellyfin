using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;

namespace Jellyfin.LiveTv.Configuration;

/// <summary>
/// <see cref="IConfigurationManager"/> extensions for Live TV.
/// </summary>
public static class LiveTvConfigurationExtensions
{
    /// <summary>
    /// Gets the <see cref="LiveTvOptions"/>.
    /// </summary>
    /// <param name="configurationManager">The <see cref="IConfigurationManager"/>.</param>
    /// <returns>The <see cref="LiveTvOptions"/>.</returns>
    public static LiveTvOptions GetLiveTvConfiguration(this IConfigurationManager configurationManager)
        => configurationManager.GetConfiguration<LiveTvOptions>("livetv");

    /// <summary>
    /// Gets the <see cref="XbmcMetadataOptions"/>.
    /// </summary>
    /// <param name="configurationManager">The <see cref="IConfigurationManager"/>.</param>
    /// <returns>The <see cref="XbmcMetadataOptions"/>.</returns>
    public static XbmcMetadataOptions GetNfoConfiguration(this IConfigurationManager configurationManager)
        => configurationManager.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata");
}
