namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// What a compatibility check tested.
/// </summary>
public enum TranscodeTestCheckKind
{
    /// <summary>
    /// Hardware decoding of a format.
    /// </summary>
    Decode,

    /// <summary>
    /// Hardware encoding to a format.
    /// </summary>
    Encode,

    /// <summary>
    /// HDR to SDR tone mapping.
    /// </summary>
    Tonemap
}
