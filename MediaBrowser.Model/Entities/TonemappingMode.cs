#pragma warning disable SA1300 // Lowercase required for backwards compat.

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Enum containing tonemapping modes.
/// </summary>
public enum TonemappingMode
{
    /// <summary>
    /// Auto.
    /// </summary>
    auto = 0,

    /// <summary>
    /// Max.
    /// </summary>
    max = 1,

    /// <summary>
    /// RGB.
    /// </summary>
    rgb = 2,

    /// <summary>
    /// Lum.
    /// </summary>
    lum = 3,

    /// <summary>
    /// ITP.
    /// </summary>
    itp = 4
}
