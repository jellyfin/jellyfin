namespace MediaBrowser.XbmcMetadata.Configuration;

/// <summary>
/// Extension methods for accessing XBMC metadata configuration.
/// </summary>
public static class NfoConfigurationExtension
{
    /// <summary>
    /// Retrieves the XBMC metadata configuration from the provided configuration manager.
    /// </summary>
    /// <param name="manager">The configuration manager instance.</param>
    /// <returns>The XBMC metadata configuration options.</returns>
    public static XbmcMetadataOptions GetNfoConfiguration(this IConfigurationManager manager)
        => manager.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata");
}
