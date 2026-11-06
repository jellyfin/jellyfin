namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// How a stream tone-maps HDR.
/// </summary>
public enum TranscodeTestTonemapMode
{
    /// <summary>
    /// No tone mapping.
    /// </summary>
    None,

    /// <summary>
    /// Tone mapping on the GPU.
    /// </summary>
    Hardware,

    /// <summary>
    /// Tone mapping on the CPU.
    /// </summary>
    Software
}
