using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;

/// <summary>
/// MusicBrainz plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// The default server URL.
    /// </summary>
    public const string DefaultServer = "https://musicbrainz.org";

    /// <summary>
    /// The default rate limit.
    /// </summary>
    public const double DefaultRateLimit = 1.0;

    private string _server = DefaultServer;

    private double _rateLimit = DefaultRateLimit;

    /// <summary>
    /// Gets or sets the server URL.
    /// </summary>
    public string Server
    {
        get => _server;

        set => _server = value.TrimEnd('/');
    }

    /// <summary>
    /// Gets or sets the rate limit.
    /// </summary>
    public double RateLimit
    {
        get => _rateLimit;
        set
        {
            if (value < DefaultRateLimit && _server == DefaultServer)
            {
                _rateLimit = DefaultRateLimit;
            }
            else
            {
                _rateLimit = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to replace the artist name.
    /// </summary>
    public bool ReplaceArtistName { get; set; }
}
