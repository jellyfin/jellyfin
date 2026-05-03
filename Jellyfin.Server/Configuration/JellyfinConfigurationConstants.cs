namespace Jellyfin.Server.Configuration;

/// <summary>
/// Well-known file names and section keys used by the JSON-backed configuration system.
/// </summary>
public static class JellyfinConfigurationConstants
{
    /// <summary>JSON file name for <see cref="MediaBrowser.Model.Configuration.ServerConfiguration"/>.</summary>
    public const string SystemJsonFile = "system.json";

    /// <summary>JSON file name for <see cref="MediaBrowser.Model.Configuration.EncodingOptions"/>.</summary>
    public const string EncodingJsonFile = "encoding.json";

    /// <summary>JSON file name for <see cref="MediaBrowser.Common.Net.NetworkConfiguration"/>.</summary>
    public const string NetworkJsonFile = "network.json";

    /// <summary>JSON file name for <see cref="MediaBrowser.Model.Branding.BrandingOptions"/>.</summary>
    public const string BrandingJsonFile = "branding.json";

    /// <summary>JSON file name for <see cref="Jellyfin.Database.Implementations.DbConfiguration.DatabaseConfigurationOptions"/>.</summary>
    public const string DatabaseJsonFile = "database.json";

    /// <summary>JSON file name for <see cref="MediaBrowser.Model.LiveTv.LiveTvOptions"/>.</summary>
    public const string LiveTvJsonFile = "livetv.json";

    /// <summary>JSON file name for <see cref="MediaBrowser.Model.Configuration.XbmcMetadataOptions"/>.</summary>
    public const string XbmcMetadataJsonFile = "xbmcmetadata.json";

    /// <summary>Top-level JSON section key for <see cref="MediaBrowser.Model.Configuration.ServerConfiguration"/>.</summary>
    public const string ServerConfigurationKey = "ServerConfiguration";

    /// <summary>Top-level JSON section key for <see cref="MediaBrowser.Model.Configuration.EncodingOptions"/>.</summary>
    public const string EncodingOptionsKey = "EncodingOptions";

    /// <summary>Top-level JSON section key for <see cref="MediaBrowser.Common.Net.NetworkConfiguration"/>.</summary>
    public const string NetworkConfigurationKey = "NetworkConfiguration";

    /// <summary>Top-level JSON section key for <see cref="MediaBrowser.Model.Branding.BrandingOptions"/>.</summary>
    public const string BrandingOptionsKey = "BrandingOptions";

    /// <summary>Top-level JSON section key for <see cref="Jellyfin.Database.Implementations.DbConfiguration.DatabaseConfigurationOptions"/>.</summary>
    public const string DatabaseConfigurationKey = "DatabaseConfiguration";

    /// <summary>Top-level JSON section key for <see cref="MediaBrowser.Model.LiveTv.LiveTvOptions"/>.</summary>
    public const string LiveTvOptionsKey = "LiveTvOptions";

    /// <summary>Top-level JSON section key for <see cref="MediaBrowser.Model.Configuration.XbmcMetadataOptions"/>.</summary>
    public const string XbmcMetadataOptionsKey = "XbmcMetadataOptions";
}
