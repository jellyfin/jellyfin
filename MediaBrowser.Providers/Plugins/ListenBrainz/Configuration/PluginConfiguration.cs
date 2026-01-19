using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.ListenBrainz.Configuration;

/// <summary>
/// ListenBrainz plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// The default Labs API server URL.
    /// </summary>
    public const string DefaultLabsServer = "https://labs.api.listenbrainz.org";

    /// <summary>
    /// The default rate limit in seconds.
    /// </summary>
    public const double DefaultRateLimit = 1.0;

    private string _labsServer = DefaultLabsServer;
    private double _rateLimit = DefaultRateLimit;

    /// <summary>
    /// Gets or sets the Labs API server URL.
    /// </summary>
    public string LabsServer
    {
        get => _labsServer;
        set => _labsServer = string.IsNullOrWhiteSpace(value) ? DefaultLabsServer : value.TrimEnd('/');
    }

    /// <summary>
    /// Gets or sets the similarity algorithm.
    /// </summary>
    public SimilarityAlgorithm Algorithm { get; set; } = SimilarityAlgorithm.SessionBased1825Days;

    /// <summary>
    /// Gets or sets the rate limit in seconds.
    /// </summary>
    public double RateLimit
    {
        get => _rateLimit;
        set
        {
            if (value < DefaultRateLimit && _labsServer == DefaultLabsServer)
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
    /// Gets the algorithm string for the API call.
    /// </summary>
    public string AlgorithmString => Algorithm.ToApiString();
}
