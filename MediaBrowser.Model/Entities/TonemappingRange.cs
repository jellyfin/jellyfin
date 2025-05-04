#pragma warning disable SA1300 // Lowercase required for backwards compat.

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Enum containing tonemapping ranges.
/// </summary>
public enum TonemappingRange
{
    /// <summary>
    /// Auto.
    /// </summary>
    auto = 0,

    /// <summary>
    /// TV.
    /// </summary>
    tv = 1,

    /// <summary>
    /// PC.
    /// </summary>
    pc = 2
}
