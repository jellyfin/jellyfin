#pragma warning disable SA1300 // Lowercase required for backwards compat.

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Enum containing tonemapping algorithms.
/// </summary>
public enum TonemappingAlgorithm
{
    /// <summary>
    /// None.
    /// </summary>
    none = 0,

    /// <summary>
    /// Clip.
    /// </summary>
    clip = 1,

    /// <summary>
    /// Linear.
    /// </summary>
    linear = 2,

    /// <summary>
    /// Gamma.
    /// </summary>
    gamma = 3,

    /// <summary>
    /// Reinhard.
    /// </summary>
    reinhard = 4,

    /// <summary>
    /// Hable.
    /// </summary>
    hable = 5,

    /// <summary>
    /// Mobius.
    /// </summary>
    mobius = 6,

    /// <summary>
    /// BT2390.
    /// </summary>
    bt2390 = 7
}
