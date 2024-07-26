namespace Jellyfin.Data.Entities;

/// <summary>
///     Defines the types of content a individial <see cref="MediaSegment"/> represents.
/// </summary>
public enum MediaSegmentType
{
    /// <summary>
    ///     Default media type or custom one.
    /// </summary>
    Unknown,

    /// <summary>
    ///     Commercial.
    /// </summary>
    Commercial,

    /// <summary>
    ///     Preview.
    /// </summary>
    Preview,

    /// <summary>
    ///     Recap.
    /// </summary>
    Recap,

    /// <summary>
    ///     Outro.
    /// </summary>
    Outro,

    /// <summary>
    ///     Intro.
    /// </summary>
    Intro
}
