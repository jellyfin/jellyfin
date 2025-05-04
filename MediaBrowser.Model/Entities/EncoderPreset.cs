#pragma warning disable SA1300 // Lowercase required for backwards compat.

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Enum containing encoder presets.
/// </summary>
public enum EncoderPreset
{
    /// <summary>
    /// Auto preset.
    /// </summary>
    auto = 0,

    /// <summary>
    /// Placebo preset.
    /// </summary>
    placebo = 1,

    /// <summary>
    /// Veryslow preset.
    /// </summary>
    veryslow = 2,

    /// <summary>
    /// Slower preset.
    /// </summary>
    slower = 3,

    /// <summary>
    /// Slow preset.
    /// </summary>
    slow = 4,

    /// <summary>
    /// Medium preset.
    /// </summary>
    medium = 5,

    /// <summary>
    /// Fast preset.
    /// </summary>
    fast = 6,

    /// <summary>
    /// Faster preset.
    /// </summary>
    faster = 7,

    /// <summary>
    /// Veryfast preset.
    /// </summary>
    veryfast = 8,

    /// <summary>
    /// Superfast preset.
    /// </summary>
    superfast = 9,

    /// <summary>
    /// Ultrafast preset.
    /// </summary>
    ultrafast = 10
}
