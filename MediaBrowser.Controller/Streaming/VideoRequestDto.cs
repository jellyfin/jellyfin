namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// The video request dto.
/// </summary>
public class VideoRequestDto : StreamingRequestDto
{
    /// <summary>
    /// Gets a value indicating whether this instance has fixed resolution.
    /// </summary>
    /// <value><c>true</c> if this instance has fixed resolution; otherwise, <c>false</c>.</value>
    public bool HasFixedResolution => Width.HasValue || Height.HasValue;

    /// <summary>
    /// Gets or sets a value indicating whether to enable subtitles in the manifest.
    /// </summary>
    public bool EnableSubtitlesInManifest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable trickplay images.
    /// </summary>
    public bool EnableTrickplay { get; set; }
}
