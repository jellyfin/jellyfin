namespace MediaBrowser.Providers.Plugins.ListenBrainz.Configuration;

/// <summary>
/// Available similarity algorithms for ListenBrainz Labs API.
/// </summary>
public enum SimilarityAlgorithm
{
    /// <summary>
    /// Session-based algorithm analyzing ~5 years of listening data.
    /// </summary>
    SessionBased1825Days = 0,

    /// <summary>
    /// Session-based algorithm analyzing ~5 years of listening data (alternate).
    /// </summary>
    SessionBased1800Days = 1,

    /// <summary>
    /// Session-based algorithm analyzing ~20 years of listening data.
    /// </summary>
    SessionBased7500Days = 2,

    /// <summary>
    /// Session-based algorithm analyzing ~20 years with higher contribution threshold.
    /// </summary>
    SessionBased7500DaysHighContribution = 3,

    /// <summary>
    /// Session-based algorithm analyzing ~25 years of listening data.
    /// </summary>
    SessionBased9000Days = 4,

    /// <summary>
    /// Session-based algorithm analyzing ~75 days of recent listening data.
    /// </summary>
    SessionBased75Days = 5
}
