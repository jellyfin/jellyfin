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
    /// NVIDIA's driver refused another encoding session. Consumer GPUs allow a limited number at once; this is a
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
    Error,

    /// <summary>
    /// The GPU ran out of memory after at least one stream kept up; the kept-up count is the result. Memory, not
    /// encoding speed or a driver session limit, stopped one more stream.
    /// </summary>
    GpuMemoryFull,

    /// <summary>
    /// NVIDIA's encoder refused another session with "out of memory", at a stream count that is not a known driver
    /// limit. That is either the driver's session limit or the GPU's memory; the kept-up count is the result.
    /// </summary>
    NvencSessionRefused
}
