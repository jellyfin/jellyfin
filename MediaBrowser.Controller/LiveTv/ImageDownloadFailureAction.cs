namespace MediaBrowser.Controller.LiveTv;

/// <summary>
/// What the caller must do with an image after a failed download.
/// </summary>
public enum ImageDownloadFailureAction
{
    /// <summary>
    /// Keep the image; the download may succeed on a later attempt.
    /// </summary>
    None = 0,

    /// <summary>
    /// Drop the image. The url is permanently invalid and re-requesting it gets the account blocked.
    /// </summary>
    RemoveImage = 1
}
