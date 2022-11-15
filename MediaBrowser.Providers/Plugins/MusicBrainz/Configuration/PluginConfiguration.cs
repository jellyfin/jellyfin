using MediaBrowser.Model.Plugins;
using MetaBrainz.MusicBrainz;

namespace MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;

/// <summary>
/// MusicBrainz plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private const string DefaultServer = "musicbrainz.org";

    private const double DefaultRateLimit = 1.0;

    private string _server = DefaultServer;

    private double _rateLimit = DefaultRateLimit;

    /// <summary>
    /// Gets or sets the server url.
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
