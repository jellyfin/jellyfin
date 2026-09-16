#nullable enable

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Context for building a session media edit graph.
/// </summary>
public sealed class SessionMediaEditGraphContext
{
    /// <summary>
    /// Gets the media-source duration in seconds, or 0 if unknown.
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Gets the requested stream start offset in seconds on the delivered timeline.
    /// </summary>
    public double StartTimeSeconds { get; init; }

    /// <summary>
    /// Gets the source audio channel layout string (e.g. <c>5.1</c>, <c>stereo</c>), or empty if unknown.
    /// </summary>
    public string InputChannelLayout { get; init; } = string.Empty;

    /// <summary>
    /// Gets the source audio channel count, or 0 if unknown.
    /// </summary>
    public int InputChannelCount { get; init; }

    /// <summary>
    /// Gets the requested output audio channel count, or 0 if unknown / unchanged.
    /// </summary>
    public int OutputAudioChannels { get; init; }

    /// <summary>
    /// Gets an optional FFmpeg stereo-downmix filter to apply after channel-level audio edits.
    /// </summary>
    public string? StereoDownmixFilter { get; init; }

    /// <summary>
    /// Gets the FFmpeg input file index for a graphical subtitle burn-in
    /// (0 = internal in the main file, 1 = external second <c>-i</c>), or null.
    /// </summary>
    public int? BurnInGraphicalSubtitleInputIndex { get; init; }

    /// <summary>
    /// Gets the FFmpeg stream index within that input of a graphical subtitle to burn in
    /// on the original timeline before length-changing filters, or null when not burning.
    /// </summary>
    public int? BurnInGraphicalSubtitleStreamIndex { get; init; }

    /// <summary>
    /// Gets scale/format filters for the graphical subtitle stream (no pad labels), or null.
    /// </summary>
    public string? BurnInGraphicalSubtitleFilters { get; init; }

    /// <summary>
    /// Gets an FFmpeg <c>subtitles=</c> filter for text/ASS burn-in on the original
    /// timeline, or null when not burning text.
    /// </summary>
    public string? BurnInTextSubtitleFilter { get; init; }
}
