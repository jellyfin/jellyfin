namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// Why a stress test ended.
/// </summary>
public enum TranscodeTestEndReason
{
    /// <summary>
    /// It has not ended.
    /// </summary>
    None,

    /// <summary>
    /// A stream fell behind real time; the previous count is the result.
    /// </summary>
    LimitReached,

    /// <summary>
    /// The stream limit was reached while every stream kept up.
    /// </summary>
    MaxStreams,

    /// <summary>
    /// ffmpeg failed.
    /// </summary>
    StreamFailed,

    /// <summary>
    /// A stream produced no frames.
    /// </summary>
    StreamStalled,

    /// <summary>
    /// NVIDIA's driver refused another encoding session. Consumer GPUs allow only a few at once; this is a
    /// driver limit, not the GPU's capacity.
    /// </summary>
    NvencSessionLimit,

    /// <summary>
    /// It was stopped.
    /// </summary>
    Cancelled,

    /// <summary>
    /// It could not run.
    /// </summary>
    Error
}
