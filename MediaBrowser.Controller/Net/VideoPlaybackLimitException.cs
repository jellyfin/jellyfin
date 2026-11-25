namespace MediaBrowser.Controller.Net;

/// <summary>
/// The exception thrown when a new video playback exceeds the user's simultaneous playback limit.
/// </summary>
public sealed class VideoPlaybackLimitException : SecurityException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackLimitException"/> class.
    /// </summary>
    public VideoPlaybackLimitException()
        : base("The maximum number of simultaneous video playbacks has been reached.")
    {
    }
}
