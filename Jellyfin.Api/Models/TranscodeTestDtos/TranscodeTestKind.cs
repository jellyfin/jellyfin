namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The kind of transcoding test.
/// </summary>
public enum TranscodeTestKind
{
    /// <summary>
    /// Checks hardware decoding, encoding and tone mapping with generated clips.
    /// </summary>
    Compatibility,

    /// <summary>
    /// Adds streams of a library item until one can no longer keep up with real time.
    /// </summary>
    StressTest
}
