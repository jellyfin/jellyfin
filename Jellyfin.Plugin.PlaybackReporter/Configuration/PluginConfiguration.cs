using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PlaybackReporter.Configuration;

/// <summary>
/// Plugin configuration for the Playback Reporter.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the GitHub personal access token used to create issues.
    /// Requires 'repo' scope.
    /// </summary>
    public string GitHubToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub repository owner (user or org) to file issues against.
    /// </summary>
    public string GitHubOwner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub repository name to file issues against.
    /// </summary>
    public string GitHubRepo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically create GitHub issues when an event is recorded.
    /// When false, events are only stored in the plugin's data folder.
    /// </summary>
    public bool AutoReportToGitHub { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum number of seconds a session must have played before a stop
    /// is considered a normal stop rather than a playback failure.
    /// </summary>
    public int MinPlaybackSecondsForFailure { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to track playback failures (file fails to play or stops very early).
    /// </summary>
    public bool TrackPlaybackFailures { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to track format/codec mismatches
    /// (client requested a format that couldn't be direct-played and had to be transcoded).
    /// </summary>
    public bool TrackFormatMismatches { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to track item mismatches
    /// (client requested a specific media source but a different one was played).
    /// </summary>
    public bool TrackItemMismatches { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to track HDR/Dolby Vision playback issues
    /// (high dynamic range content that had to be tone-mapped or failed to play).
    /// </summary>
    public bool TrackHdrIssues { get; set; } = true;

    /// <summary>
    /// Gets or sets labels to apply to GitHub issues created by this plugin.
    /// Comma-separated list of label names.
    /// </summary>
    public string GitHubIssueLabels { get; set; } = "playback,bug";
}
