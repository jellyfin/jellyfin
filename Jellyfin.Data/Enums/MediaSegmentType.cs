using Jellyfin.Data.Entities;

namespace Jellyfin.Data.Enums;

/// <summary>
/// Defines the types of content an individual <see cref="MediaSegment"/> represents.
/// </summary>
public enum MediaSegmentType
{
    /// <summary>
    /// Default media type or custom one.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Commercial.
    /// </summary>
    Commercial = 1,

    /// <summary>
    /// Preview.
    /// </summary>
    Preview = 2,

    /// <summary>
    /// Recap.
    /// </summary>
    Recap = 3,

    /// <summary>
    /// Outro.
    /// </summary>
    Outro = 4,

    /// <summary>
    /// Intro.
    /// </summary>
    Intro = 5
}
