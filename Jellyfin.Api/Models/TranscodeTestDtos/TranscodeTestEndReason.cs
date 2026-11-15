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
    /// It was stopped.
    /// </summary>
    Cancelled,

    /// <summary>
    /// It could not run.
    /// </summary>
    Error,

    /// <summary>
    /// The GPU ran out of memory after at least one stream kept up; the kept-up count is the result. Memory, not
    /// encoding speed or a driver session limit, stopped one more stream.
    /// </summary>
    GpuMemoryFull,

    /// <summary>
    /// NVIDIA's encoder refused another session. The diagnostic may reflect resource exhaustion, session
    /// restrictions or licensing; it does not prove a numerical driver session cap. The kept-up count is the result.
    /// </summary>
    NvencSessionRefused,

    /// <summary>
    /// The test stopped without establishing a capacity limit. Any kept-up count is a measured lower bound,
    /// not the server's maximum; for example, a short file exhausted the restart budget.
    /// </summary>
    Inconclusive
}
