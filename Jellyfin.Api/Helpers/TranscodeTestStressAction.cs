namespace Jellyfin.Api.Helpers;

/// <summary>
/// The actions the stress test controller can ask for.
/// </summary>
internal enum TranscodeTestStressAction
{
    /// <summary>
    /// Keep the current load.
    /// </summary>
    Wait,

    /// <summary>
    /// Start one more stream.
    /// </summary>
    AddStream,

    /// <summary>
    /// End: a stream fell behind.
    /// </summary>
    LimitReached,

    /// <summary>
    /// End: the stream limit was reached while every stream kept up.
    /// </summary>
    MaxStreams,

    /// <summary>
    /// End: a stream produced no frames.
    /// </summary>
    Stalled,

    /// <summary>
    /// End: the test ran for its time limit.
    /// </summary>
    TimeLimit
}
