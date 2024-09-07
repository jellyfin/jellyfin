namespace Jellyfin.MediaEncoding.Hls.Playlist;

/// <summary>
/// Request class for the <see cref="IDynamicHlsPlaylistGenerator.CreateMainPlaylist(CreateMainPlaylistRequest)"/> method.
/// </summary>
public class CreateMainPlaylistRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateMainPlaylistRequest"/> class.
    /// </summary>
    /// <param name="filePath">The absolute file path to the file.</param>
    /// <param name="desiredSegmentLengthMs">The desired segment length in milliseconds.</param>
    /// <param name="totalRuntimeTicks">The total duration of the file in ticks.</param>
    /// <param name="segmentContainer">The desired segment container eg. "ts".</param>
    /// <param name="endpointPrefix">The URI prefix for the relative URL in the playlist.</param>
    /// <param name="queryString">The desired query string to append (must start with ?).</param>
    /// <param name="isRemuxingVideo">Whether the video is being remuxed.</param>
    public CreateMainPlaylistRequest(string filePath, int desiredSegmentLengthMs, long totalRuntimeTicks, string segmentContainer, string endpointPrefix, string queryString, bool isRemuxingVideo)
    {
        FilePath = filePath;
        DesiredSegmentLengthMs = desiredSegmentLengthMs;
        TotalRuntimeTicks = totalRuntimeTicks;
        SegmentContainer = segmentContainer;
        EndpointPrefix = endpointPrefix;
        QueryString = queryString;
        IsRemuxingVideo = isRemuxingVideo;
    }

    /// <summary>
    /// Gets the file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the desired segment length in milliseconds.
    /// </summary>
    public int DesiredSegmentLengthMs { get; }

    /// <summary>
    /// Gets the total runtime in ticks.
    /// </summary>
    public long TotalRuntimeTicks { get; }

    /// <summary>
    /// Gets the segment container.
    /// </summary>
    public string SegmentContainer { get; }

    /// <summary>
    /// Gets the endpoint prefix for the URL.
    /// </summary>
    public string EndpointPrefix { get; }

    /// <summary>
    /// Gets the query string.
    /// </summary>
    public string QueryString { get; }

    /// <summary>
    /// Gets a value indicating whether the video is being remuxed.
    /// </summary>
    public bool IsRemuxingVideo { get; }
}
