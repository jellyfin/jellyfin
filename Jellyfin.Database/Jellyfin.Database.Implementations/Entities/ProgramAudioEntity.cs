namespace Jellyfin.Data.Entities;

/// <summary>
/// Lists types of Audio.
/// </summary>
public enum ProgramAudioEntity
{
    /// <summary>
    /// Mono.
    /// </summary>
    Mono = 0,

    /// <summary>
    /// Stereo.
    /// </summary>
    Stereo = 1,

    /// <summary>
    /// Dolby.
    /// </summary>
    Dolby = 2,

    /// <summary>
    /// DolbyDigital.
    /// </summary>
    DolbyDigital = 3,

    /// <summary>
    /// Thx.
    /// </summary>
    Thx = 4,

    /// <summary>
    /// Atmos.
    /// </summary>
    Atmos = 5
}
