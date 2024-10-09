namespace Jellyfin.Data.Entities;

/// <summary>
/// Lists types of Audio.
/// </summary>
public enum ProgramAudioEntity
{
    /// <summary>
    /// Mono.
    /// </summary>
    Mono,

    /// <summary>
    /// Sterio.
    /// </summary>
    Stereo,

    /// <summary>
    /// Dolby.
    /// </summary>
    Dolby,

    /// <summary>
    /// DolbyDigital.
    /// </summary>
    DolbyDigital,

    /// <summary>
    /// Thx.
    /// </summary>
    Thx,

    /// <summary>
    /// Atmos.
    /// </summary>
    Atmos
}
