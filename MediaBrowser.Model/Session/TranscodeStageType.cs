namespace MediaBrowser.Model.Session;

/// <summary>
/// Enum representing the kind of operation a transcoding pipeline stage performs.
/// </summary>
public enum TranscodeStageType
{
    /// <summary>
    /// Unknown or unclassified stage.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Decoding of the source video stream.
    /// </summary>
    Decode = 1,

    /// <summary>
    /// Scaling/resizing of the video frames.
    /// </summary>
    Scale = 2,

    /// <summary>
    /// Deinterlacing of the video frames.
    /// </summary>
    Deinterlace = 3,

    /// <summary>
    /// HDR to SDR (or HDR to HDR) tone mapping.
    /// </summary>
    ToneMap = 4,

    /// <summary>
    /// Subtitle burn-in / overlay.
    /// </summary>
    Subtitle = 5,

    /// <summary>
    /// Pixel format conversion.
    /// </summary>
    Format = 6,

    /// <summary>
    /// Encoding of the output video stream.
    /// </summary>
    Encode = 7,

    /// <summary>
    /// Upload of frames from system (software) memory into hardware memory (<c>hwupload</c>).
    /// This crosses the software/hardware boundary and is a real memory transfer that can affect
    /// performance.
    /// </summary>
    HardwareUpload = 8,

    /// <summary>
    /// Download of frames from hardware memory back to system (software) memory
    /// (<c>hwdownload</c>). This crosses the hardware/software boundary and is a real memory
    /// transfer that can affect performance.
    /// </summary>
    HardwareDownload = 9,

    /// <summary>
    /// Bitstream passthrough (<c>-c copy</c>). The stream is not decoded or re-encoded, only
    /// rewrapped into the output container.
    /// </summary>
    Copy = 10
}
