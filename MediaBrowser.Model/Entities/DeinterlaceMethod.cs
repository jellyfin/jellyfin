#pragma warning disable SA1300 // Lowercase required for backwards compat.

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Enum containing deinterlace methods.
/// </summary>
public enum DeinterlaceMethod
{
    /// <summary>
    /// YADIF.
    /// </summary>
    yadif = 0,

    /// <summary>
    /// BWDIF.
    /// </summary>
    bwdif = 1
}
