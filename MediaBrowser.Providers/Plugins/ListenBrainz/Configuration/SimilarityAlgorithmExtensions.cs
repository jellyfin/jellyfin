namespace MediaBrowser.Providers.Plugins.ListenBrainz.Configuration;

/// <summary>
/// Extension methods for <see cref="SimilarityAlgorithm"/>.
/// </summary>
public static class SimilarityAlgorithmExtensions
{
    /// <summary>
    /// Gets the API string value for the algorithm.
    /// </summary>
    /// <param name="algorithm">The algorithm.</param>
    /// <returns>The API string value.</returns>
    public static string ToApiString(this SimilarityAlgorithm algorithm) => algorithm switch
    {
        SimilarityAlgorithm.SessionBased1825Days => "session_based_days_1825_session_300_contribution_3_threshold_10_limit_100_filter_True_skip_30",
        SimilarityAlgorithm.SessionBased1800Days => "session_based_days_1800_session_300_contribution_3_threshold_10_limit_100_filter_True_skip_30",
        SimilarityAlgorithm.SessionBased7500Days => "session_based_days_7500_session_300_contribution_3_threshold_10_limit_100_filter_True_skip_30",
        SimilarityAlgorithm.SessionBased7500DaysHighContribution => "session_based_days_7500_session_300_contribution_5_threshold_10_limit_100_filter_True_skip_30",
        SimilarityAlgorithm.SessionBased9000Days => "session_based_days_9000_session_300_contribution_5_threshold_15_limit_50_skip_30",
        SimilarityAlgorithm.SessionBased75Days => "session_based_days_75_session_300_contribution_5_threshold_10_limit_100_filter_True_skip_30",
        _ => "session_based_days_1825_session_300_contribution_3_threshold_10_limit_100_filter_True_skip_30"
    };
}
