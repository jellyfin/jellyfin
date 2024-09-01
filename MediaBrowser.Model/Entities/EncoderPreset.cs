#pragma warning disable SA1300 // Lowercase required for backwards compat.

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Enum containing encoder presets.
/// </summary>
public enum EncoderPreset
{
    /// <summary>
    /// Placebo preset.
    /// </summary>
    placebo = 0,

    /// <summary>
    /// Veryslow preset.
    /// </summary>
    veryslow = 1,

    /// <summary>
    /// Slower preset.
    /// </summary>
    slower = 2,

    /// <summary>
    /// Slow preset.
    /// </summary>
    slow = 3,

    /// <summary>
    /// Medium preset.
    /// </summary>
    medium = 4,

    /// <summary>
    /// Fast preset.
    /// </summary>
    fast = 5,

    /// <summary>
    /// Faster preset.
    /// </summary>
    faster = 6,

    /// <summary>
    /// Veryfast preset.
    /// </summary>
    veryfast = 7,

    /// <summary>
    /// Superfast preset.
    /// </summary>
    superfast = 8,

    /// <summary>
    /// Ultrafast preset.
    /// </summary>
    ultrafast = 9
}
