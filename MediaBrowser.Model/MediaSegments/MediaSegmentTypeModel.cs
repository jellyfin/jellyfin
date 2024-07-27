namespace MediaBrowser.Model.MediaSegments;

/// <summary>
///     Defines the types of content a individial <see cref="MediaSegmentModel"/> represents.
/// </summary>
public enum MediaSegmentTypeModel
{
    /// <summary>
    ///     Default media type or custom one.
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     Commercial.
    /// </summary>
    Commercial = 1,

    /// <summary>
    ///     Preview.
    /// </summary>
    Preview = 2,

    /// <summary>
    ///     Recap.
    /// </summary>
    Recap = 3,

    /// <summary>
    ///     Outro.
    /// </summary>
    Outro = 4,

    /// <summary>
    ///     Intro.
    /// </summary>
    Intro = 5
}
