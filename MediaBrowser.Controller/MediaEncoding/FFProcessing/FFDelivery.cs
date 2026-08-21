namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// How a stream reaches the client. Orthogonal to <see cref="StreamMode"/>: either delivery can
/// carry any amount of re-encoding.
/// </summary>
public enum FFDelivery
{
    /// <summary>A single continuous output the client reads as one response.</summary>
    Progressive,

    /// <summary>A playlist of segment files the client fetches individually.</summary>
    Hls
}
